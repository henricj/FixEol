using System;

using FixEol;

if (args.Length < 1)
    args = new[] { Environment.CurrentDirectory };

var transform = new EncodingAndEolTransform
{
    OutputBomPolicy = EncodingAndEolTransform.BomPolicy.Never
    //OutputEncoding = Encoding.Unicode
};

try
{
    using var fileProcessor = new FileProcessor();

    var files = await fileProcessor.ProcessFilesAsync(args, transform.TransformFileAsync);

    foreach (var file in files)
        Console.WriteLine("{0}", file);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
