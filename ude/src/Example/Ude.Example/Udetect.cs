using System;
using System.IO;

namespace Ude.Example
{
    public class Udetect
    {
        /// <summary>
        ///     Command line example: detects the encoding of the given file.
        /// </summary>
        /// <param name="args">a filename</param>
        public static void Main(String[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: udetect <filename>");
                return;
            }

            var filename = args[0];
            using (var fs = File.OpenRead(filename))
            {
                ICharsetDetector cdet = new CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();
                if (cdet.Charset != null)
                {
                    Console.WriteLine("Charset: {0}, confidence: {1}",
                        cdet.Charset, cdet.Confidence);
                }
                else
                    Console.WriteLine("Detection failed.");
            }
        }
    }
}
