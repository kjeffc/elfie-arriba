﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Serialization.Json
{
    [TestClass]
    public class DataBlockJsonConverterTests
    {
        [TestMethod]
        public void DataBlockJsonConverter_Basic()
        {
            DataBlock items = new DataBlock(new string[] { "ID", "Priority", "Title" }, 5);
            items.SetColumn(0, new object[] { 11512, 11643, 11943, 11999, Value.Create(12505) });
            items.SetColumn(1, new object[] { 0, 3, 1, 3, 3 });
            items.SetColumn(2, new object[] { "Sample One", "Sample Two", "Sample Three", "Sample Four", "" });

            string serialized = ArribaConvert.ToJson(items);

            DataBlock itemsRoundTripped = ArribaConvert.FromJson<DataBlock>(serialized);
            Assert.AreEqual(items.RowCount, itemsRoundTripped.RowCount);
            Assert.AreEqual(items.ColumnCount, itemsRoundTripped.ColumnCount);

            for (int rowIndex = 0; rowIndex < items.RowCount; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < items.ColumnCount; ++columnIndex)
                {
                    Assert.AreEqual(Value.Create(items[rowIndex, columnIndex]), Value.Create(itemsRoundTripped[rowIndex, columnIndex]));
                }
            }
        }
    }
}
