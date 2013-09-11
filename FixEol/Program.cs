using System;
using System.Text;

namespace FixEol
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                args = new[]
                       {
                           Environment.CurrentDirectory
                       };
            }

            var transform = new EncodingAndEolTransform
                            {
                                //OutputBomPolicy = EncodingAndEolTransform.BomPolicy.CopyUtf8OrForce
                                //OutputEncoding = Encoding.Unicode
                            };

            try
            {
                using (var fileProcessor = new FileProcessor())
                {
                    var files = fileProcessor.ProcessFilesAsync(args, transform.TransformFileAsync).Result;

                    foreach (var file in files)
                        Console.WriteLine("{0}", file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
