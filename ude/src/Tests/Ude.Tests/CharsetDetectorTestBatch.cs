using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ude.Tests
{
    [TestClass]
    public class CharsetDetectorTestBatch
    {
        // Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)
        const string DATA_ROOT = "../../Data";

        ICharsetDetector detector;

        [TestInitialize]
        public void SetUp()
        {
            detector = new CharsetDetector();
        }

        [TestCleanup]
        public void TearDown()
        {
            detector = null;
        }

        [TestMethod]
        public void TestLatin1()
        {
            Process(Charsets.WIN1252, "latin1");
        }

        [TestMethod]
        public void TestCJK()
        {
            Process(Charsets.GB18030, "gb18030");
            Process(Charsets.BIG5, "big5");
            Process(Charsets.SHIFT_JIS, "shiftjis");
            Process(Charsets.EUCJP, "eucjp");
            Process(Charsets.EUCKR, "euckr");
            Process(Charsets.EUCTW, "euctw");
            Process(Charsets.ISO2022_JP, "iso2022jp");
            Process(Charsets.ISO2022_KR, "iso2022kr");
        }

        [TestMethod]
        public void TestHebrew()
        {
            Process(Charsets.WIN1255, "windows1255");
        }

        [TestMethod]
        public void TestGreek()
        {
            Process(Charsets.ISO_8859_7, "iso88597");
            //Process(Charsets.WIN1253, "windows1253");
        }

        [TestMethod]
        public void TestCyrillic()
        {
            Process(Charsets.WIN1251, "windows1251");
            Process(Charsets.KOI8R, "koi8r");
            Process(Charsets.IBM855, "ibm855");
            Process(Charsets.IBM866, "ibm866");
            Process(Charsets.MAC_CYRILLIC, "maccyrillic");
        }

        [TestMethod]
        public void TestBulgarian()
        { }

        [TestMethod]
        public void TestUTF8()
        {
            Process(Charsets.UTF8, "utf8");
        }

        void Process(string charset, string dirname)
        {
            var path = Path.Combine(DATA_ROOT, dirname);
            if (!Directory.Exists(path))
                return;

            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                using (var fs = new FileStream(file, FileMode.Open))
                {
                    Console.WriteLine("Analysing {0}", file);
                    detector.Feed(fs);
                    detector.DataEnd();
                    Console.WriteLine("{0} : {1} {2}",
                        file, detector.Charset, detector.Confidence);
                    Assert.AreEqual(charset, detector.Charset);
                    detector.Reset();
                }
            }
        }
    }
}
