using System;

using FixEol;

if (args.Length < 1)
    args = new[] { Environment.CurrentDirectory };

var transform = new EncodingAndEolTransform
{
    OutputBomPolicy = EncodingAndEolTransform.BomPolicy.Never,
    TrimLines = false
    //OutputEncoding = Encoding.Unicode
};

try
{
    await using var fileProcessor = new FileProcessor { NoChanges = false };

    var files = await fileProcessor.ProcessFilesAsync(args, transform.TransformFileAsync);

    foreach (var file in files)
        Console.WriteLine("{0}", file);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
