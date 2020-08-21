// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arriba.Test.Platform
{
    [TestClass]
    public class CrossPlatformTests
    {
        /// <summary>
        /// Tests created to help track down what appears to be a sporadic failure on Ubuntu and .NET Core
        /// </summary>
        [TestMethod]
        public void PartitionMaskTestsFailureOnLinux11512()
        {
            var value = 11512;
            byte partitionBits = 1;
            var expectedPartition = 0;
            var p = new PartitionConvert<int>(partitionBits);
            Assert.AreEqual(expectedPartition, p.GetPartition(value));
        }

        /// <summary>
        /// Tests created to help track down what appears to be a sporadic failure on Ubuntu and .NET Core
        /// </summary>
        [TestMethod]
        public void PartitionMaskTestsFailureOnLinux11643()
        {
            var value = 11643;
            byte partitionBits = 1;
            var expectedPartition = 1;
            var p = new PartitionConvert<int>(partitionBits);
            Assert.AreEqual(expectedPartition, p.GetPartition(value));
        }
    }
}
