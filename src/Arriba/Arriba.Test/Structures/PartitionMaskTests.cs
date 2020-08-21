// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Arriba.Structures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Structures
{
    [TestClass]
    public class PartitionMaskTests
    {
        [TestMethod]
        public void PartitionMask_VerifyAllIsZero()
        {
            PartitionMask mask = PartitionMask.All;

            // Verify 'All' is count zero, value zero
            Assert.AreEqual(0, mask.BitCount);
            Assert.AreEqual(0, mask.Value);
        }

        [TestMethod]
        public void PartitionMask_VerifyBitCountOf4Matches()
        {
            // 1101* should match values with the first four bits
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 4;
            mask.Value = (0xD << 28);
            Assert.IsFalse(mask.Matches(0));
            Assert.IsFalse(mask.Matches(~0));
            Assert.IsFalse(mask.Matches(unchecked((int)0xC0000000)));
            Assert.IsTrue(mask.Matches(unchecked((int)0xD0000000)));
            Assert.IsTrue(mask.Matches(unchecked((int)0xD7777777)));
            Assert.IsFalse(mask.Matches(unchecked((int)0xE0000000)));
            Assert.IsFalse(mask.Matches(unchecked((int)0xF0000000)));
            Assert.IsFalse(mask.Matches(unchecked((int)0x7FFFFFFF)));
            Assert.AreEqual("1101", mask.ToString());
        }

        [TestMethod]
        public void PartitionMask_VerifyBitCountOf2Matches()
        {
            // 11* should match values with the first two bits
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 2;
            mask.Value = (0x3 << 30);
            Assert.IsFalse(mask.Matches(0));
            Assert.IsTrue(mask.Matches(~0));
            Assert.IsFalse(mask.Matches(unchecked((int)0x80000000)));
            Assert.IsTrue(mask.Matches(unchecked((int)0xC0000000)));
            Assert.IsTrue(mask.Matches(unchecked((int)0xE0000000)));
            Assert.IsFalse(mask.Matches(0x7FFFFFFF));
            Assert.AreEqual("11", mask.ToString());
        }

        [TestMethod]
        public void PartitionMask_VerifyZeroDoesNotMatchFirstBit()
        {
            // 0* should match values without the first bit set
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 1;
            mask.Value = 0;
            Assert.IsTrue(mask.Matches(0));
            Assert.IsFalse(mask.Matches(~0));
            Assert.IsFalse(mask.Matches(unchecked((int)0x80000000)));
            Assert.IsTrue(mask.Matches(0x7FFFFFFF));
            Assert.AreEqual("0", mask.ToString());

        }


        [TestMethod]
        public void PartitionMask_VerifyOnlyMatchesOnFirstBit()
        {
            // 1* should match values starting with 0x8 only
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 1;
            mask.Value = (0x1 << 31);
            Assert.IsFalse(mask.Matches(0));
            Assert.IsTrue(mask.Matches(~0));
            Assert.IsTrue(mask.Matches(unchecked((int)0x80000000)));
            Assert.IsFalse(mask.Matches(0x7FFFFFFF));
            Assert.AreEqual("1", mask.ToString());
        }

        [TestMethod]
        public void PartitionMask_VeryifyEverythingMatchesZeroWithMask()
        {
            // Everything matches a zero-width mask
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 0;
            Assert.IsTrue(mask.Matches(0));
            Assert.IsTrue(mask.Matches(~0));
            Assert.AreEqual("", mask.ToString());
        }

        [TestMethod]
        public void PartitionMask_VerifyAllIsNotAltered()
        { 
            // Verify 'All' has not been altered by copy/use
            PartitionMask mask = PartitionMask.All;
            mask.BitCount = 1;
            mask.Value = 2;
            Assert.AreEqual(0, PartitionMask.All.BitCount);
            Assert.AreEqual(0, PartitionMask.All.Value);
        }

        [TestMethod]
        public void PartitionMask_BuildSet()
        {
            Assert.AreEqual("", String.Join(", ", (IEnumerable<PartitionMask>)PartitionMask.BuildSet(0)));
            Assert.AreEqual("0, 1", String.Join(", ", (IEnumerable<PartitionMask>)PartitionMask.BuildSet(1)));
            Assert.AreEqual("00, 01, 10, 11", String.Join(", ", (IEnumerable<PartitionMask>)PartitionMask.BuildSet(2)));
        }

        [TestMethod]
        public void PartitionMask_GetHashIndex()
        {
            const byte bitsPerByte = 8;
            byte bitCount = 2;

            Assert.AreEqual("00, 01, 10, 11", String.Join(", ", (IEnumerable<PartitionMask>)PartitionMask.BuildSet(bitCount)));

            for (byte b = 0; b < byte.MaxValue; ++b)
            {
                int value = b << (32 - bitsPerByte);        // shift so the test value is in the highest bits
                int expectedIndex = b >> (8 - bitCount);
                Assert.AreEqual(expectedIndex, PartitionMask.IndexOfHash(value, bitCount));
            }
        }
    }
}
