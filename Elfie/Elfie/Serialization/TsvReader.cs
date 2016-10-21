﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using Microsoft.CodeAnalysis.Elfie.Model.Structures;

namespace Microsoft.CodeAnalysis.Elfie.Serialization
{
    /// <summary>
    ///  TsvReader is a high performance reader for the TSV (tab-separated value)
    ///  format. The reader doesn't support escaped \t or \n in values (no standard
    ///  for escaping seems to be defined).
    ///  
    ///  Content is read into a byte[] large enough to read at least two rows.
    ///  Look up the indices of columns you want to read outside any loops.
    ///  Values are read into String8s, which can be copied or converted.
    ///  
    ///  String8s from TsvReader must be copied, since NextRow calls will reuse the
    ///  byte[] and change the values of String8s returned beforehand.
    ///  
    ///  Usage:
    ///  using (TsvReader r = new TsvReader(loadFromPath, true))
    ///  {
    ///     // Look up column indices outside the loop
    ///     int memberShaIndex = r.ColumnIndex("c_ObjectDN_Sha");
    ///     int groupShaIndex = r.ColumnIndex("c_ObjectMemberOfDN_Sha");
    ///     int linkTypeIndex = r.ColumnIndex("ti_LinkType");
    ///
    ///     // Use NextRow() and CurrentRow(index) to read values
    ///     while (r.NextRow())
    ///     {
    ///         String8 groupSha = r.CurrentRow(groupShaIndex);
    ///         String8 memberSha = r.CurrentRow(memberShaIndex);
    ///         int linkType = r.CurrentRow(linkTypeIndex).ToInteger();
    ///         
    ///         // COPY String8s to be kept
    ///     }
    /// }
    /// </summary>
    public class TsvReader : IDisposable
    {
        private BinaryReader _reader;
        private List<string> _columnHeadingsList;
        private Dictionary<string, int> _columnHeadings;

        private char _cellDelimiter;
        private int _currentLine;
        private byte[] _buffer;
        private PartialArray<int> _rowPositionArray;
        private PartialArray<int> _cellPositionArray;
        private int _nextRowIndexInBlock;
        private String8Set _currentBlock;
        private String8Set _currentRow;

        /// <summary>
        ///  Construct a TsvReader to read the given Tsv file.
        /// </summary>
        /// <param name="tsvFilePath">File Path to Tsv file to read</param>
        /// <param name="hasHeaderRow">True to read the first row as column names, False not to pre-read anything</param>
        public TsvReader(string tsvFilePath, bool hasHeaderRow = true, char cellDelimiter = '\t')
        {
            _reader = new BinaryReader(new FileStream(tsvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            _columnHeadingsList = new List<string>();
            _columnHeadings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            _cellDelimiter = cellDelimiter;
            _currentLine = 0;
            _buffer = new byte[64 * 1024];
            _rowPositionArray = new PartialArray<int>(1024, false);
            _cellPositionArray = new PartialArray<int>(64, false);
            _nextRowIndexInBlock = 0;

            // Read the heading row and record heading positions
            if (hasHeaderRow)
            {
                if (!NextRow()) throw new IOException(String.Format("TsvReader for \"{0}\" didn't find any rows when trying to read a header row.", tsvFilePath));

                for (int i = 0; i < _currentRow.Count; ++i)
                {
                    string columnName = CurrentRow(i).ToString();
                    _columnHeadingsList.Add(columnName);
                    _columnHeadings[columnName] = i;
                }
            }
        }

        /// <summary>
        ///  Return the column headings found. The set is empty if there was no heading row.
        /// </summary>
        public IReadOnlyList<string> Columns
        {
            get { return this._columnHeadingsList;  }
        }

        /// <summary>
        ///  Look up the column index of a given column by name.
        ///  Will only work if the TSV had a header row.
        ///  Column names are case insensitive.
        ///  Will throw if the column name wasn't found.
        /// </summary>
        /// <param name="columnName">Column name for which to find column index</param>
        /// <returns>Index of column in TSV. Throws if column isn't found or no header row was read.</returns>
        public int ColumnIndex(string columnName)
        {
            int columnIndex = 0;
            if (_columnHeadings.TryGetValue(columnName, out columnIndex)) return columnIndex;

            throw new ColumnNotFoundException(String.Format("Column Name \"{0}\" not found in TSV.\nKnown Columns: \"{1}\"", columnName, String.Join(", ", _columnHeadings.Keys)));
        }

        /// <summary>
        ///  Get a cell value from the current row by column index.
        ///  Throws if not enough cells exist in the current row.
        /// </summary>
        /// <param name="columnIndex">Column Index for which to get value</param>
        /// <returns>String8 with value for column in current row. Throws if not enough columns in row.</returns>
        public String8 CurrentRow(int columnIndex)
        {
            return _currentRow[columnIndex];
        }

        /// <summary>
        ///  Returns the full text of the current row. Useful for debugging
        ///  incomplete or mal-formed rows.
        /// </summary>
        public String8 CurrentRowText
        {
            get { return _currentRow.Value; }
        }

        /// <summary>
        ///  Returns the number of the current row (the same as the line number
        ///  in the file). Useful to report a specific location if unexpected
        ///  content is read.
        /// </summary>
        public int CurrentRowNumber
        {
            get { return _currentLine; }
        }

        /// <summary>
        ///  Returns the number of columns in the current row. Used to handle
        ///  rows which are shorter than expected, if desired.
        /// </summary>
        public int CurrentRowColumns
        {
            get { return _currentRow.Count; }
        }

        /// <summary>
        ///  Move the reader to the next row. This must be called before
        ///  reading the first row.
        /// </summary>
        /// <returns>True if another row exists, False if the TSV is out of content</returns>
        public bool NextRow()
        {
            // If we're on the last row, ask for more (we don't read the last row in case it was only partially read into the buffer)
            if (_nextRowIndexInBlock >= _currentBlock.Count - 1)
            {
                NextBlock();
            }

            // If there are no more rows, return false
            if (_nextRowIndexInBlock >= _currentBlock.Count) return false;

            // Get the next (complete) row from the current block
            String8 currentLine = _currentBlock[_nextRowIndexInBlock];

            // Trim a trailing '\r', if found [handle \n or \r\n line endings]
            if (currentLine.EndsWith((byte)'\r')) currentLine = currentLine.Substring(0, currentLine.Length - 1);

            // Split the line into cells
            _currentRow = SplitCells(currentLine);

            _currentLine++;
            _nextRowIndexInBlock++;

            return true;
        }

        /// <summary>
        ///  NextBlock is called by NextRow before reading the last row in _currentBlock.
        ///  Since the file is read in blocks, the last row is usually incomplete.
        ///  
        ///  If there's more file content, NextBlock should copy the last row to the start
        ///  of the buffer, read more content, and reset _currentBlock to the new split rows
        ///  and _nextRowIndexInBlock to zero (telling NextRow to read that row next).
        ///  
        ///  If there's no more file, the last row is complete. NextBlock must return
        ///  without changing _currentBlock or _nextRowIndexInBlock to tell NextRow it's safe
        ///  to return to the user.
        ///  
        ///  NextRow will call NextBlock *again* after the last row. NextBlock must again
        ///  not change anything to tell NextRow that there's nothing left.
        ///  
        ///  So, NextBlock must:
        ///   - Copy the last row to the start of the buffer (if not already there)
        ///   - Read more content to fill the buffer
        ///   - Split the buffer into rows
        ///   - Stop at end-of-file or when a full row was read
        ///   - Double the buffer until one of these conditions is met
        ///   
        ///   - Reset nextRowInIndexBlock *only if* a row was shifted or read
        /// </summary>
        private void NextBlock()
        {
            int bufferLengthFilledStart = 0;

            // Copy the last row to the start of the buffer (if not already there)
            if (_currentBlock.Count > 1)
            {
                String8 lastRow = _currentBlock[_currentBlock.Count - 1];
                lastRow.WriteTo(_buffer, 0);
                bufferLengthFilledStart = lastRow.Length;

                // Reset the next row to read (since we shifted a row)
                _nextRowIndexInBlock = 0;
            }

            int bufferLengthFilled = bufferLengthFilledStart;

            while (true)
            {
                // Read more content to fill the buffer
                bufferLengthFilled += _reader.Read(_buffer, bufferLengthFilled, _buffer.Length - bufferLengthFilled);

                // Split the buffer into rows
                _currentBlock = SplitRows(new String8(_buffer, 0, bufferLengthFilled));

                // Stop at end-of-file (read didn't fill buffer)
                if (bufferLengthFilled < _buffer.Length) break;

                // Stop when a full row was read (split found at least two parts)
                if (_currentBlock.Count > 1) break;

                // Otherwise, double the buffer (until a full row or end of file)
                byte[] newBuffer = new byte[_buffer.Length * 2];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }

            // If we read new content, reset the next row to read
            if (bufferLengthFilled > bufferLengthFilledStart) _nextRowIndexInBlock = 0;
        }

        protected virtual String8Set SplitCells(String8 row)
        {
            return row.Split(_cellDelimiter, _cellPositionArray);
        }

        protected virtual String8Set SplitRows(String8 block)
        {
            return block.Split('\n', _rowPositionArray);
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }
    }

    [Serializable]
    public class ColumnNotFoundException : Exception
    {
        public ColumnNotFoundException() { }
        public ColumnNotFoundException(string message) : base(message) { }
        public ColumnNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected ColumnNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
