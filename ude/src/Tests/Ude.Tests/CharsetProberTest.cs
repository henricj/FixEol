using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ude.Core;

namespace Ude.Tests
{
    [TestClass]
    public class CharsetProberTest
    {
        [TestMethod]
        public void TestFilterWithEnglishLetter()
        {
            byte[] buf = { 0xBF, 0x68, 0x21, 0x21, 0x65, 0x6C, 0x6F, 0x21, 0x21 };
            var p = new DummyCharsetProber();
            p.TestFilterWithEnglishLetter(buf, 0, buf.Length);
        }

        [TestMethod]
        public void TestFilterWithoutEnglishLetter()
        {
            byte[] buf = { 0xEE, 0x21, 0x6C, 0x21, 0xEE, 0x6C, 0x6C };
            var p = new DummyCharsetProber();
            p.TestFilterWithoutEnglishLetter(buf, 0, buf.Length);
        }
    }

    public class DummyCharsetProber : CharsetProber
    {
        public byte[] TestFilterWithEnglishLetter(byte[] buf, int offset, int len)
        {
            return FilterWithEnglishLetters(buf, offset, len);
        }

        public byte[] TestFilterWithoutEnglishLetter(byte[] buf, int offset, int len)
        {
            return FilterWithoutEnglishLetters(buf, offset, len);
        }

        public override float GetConfidence()
        {
            return 0.0f;
        }

        public override void Reset()
        { }

        public override string GetCharsetName()
        {
            return null;
        }

        public override ProbingState HandleData(byte[] buf, int offset, int len)
        {
            return ProbingState.Detecting;
        }
    }
}
