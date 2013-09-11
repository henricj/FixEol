using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ude.Core;

namespace Ude.Tests
{
    [TestClass]
    public class BitPackageTest
    {
        [TestMethod]
        public void TestPack()
        {
            Assert.AreEqual(BitPackage.Pack4bits(0, 0, 0, 0, 0, 0, 0, 0), 0);
            Assert.AreEqual(BitPackage.Pack4bits(1, 1, 1, 1, 1, 1, 1, 1), 286331153);
            Assert.AreEqual(BitPackage.Pack4bits(2, 2, 2, 2, 2, 2, 2, 2), 572662306);
            Assert.AreEqual(BitPackage.Pack4bits(15, 15, 15, 15, 15, 15, 15, 15), -1);
        }

        [TestMethod]
        public void TestUnpack()
        {
            int[] data =
            {
                BitPackage.Pack4bits(0, 1, 2, 3, 4, 5, 6, 7),
                BitPackage.Pack4bits(8, 9, 10, 11, 12, 13, 14, 15)
            };

            var pkg = new BitPackage(
                BitPackage.INDEX_SHIFT_4BITS,
                BitPackage.SHIFT_MASK_4BITS,
                BitPackage.BIT_SHIFT_4BITS,
                BitPackage.UNIT_MASK_4BITS,
                data);

            for (var i = 0; i < 16; i++)
            {
                var n = pkg.Unpack(i);
                Assert.AreEqual(n, i);
            }
        }
    }
}
