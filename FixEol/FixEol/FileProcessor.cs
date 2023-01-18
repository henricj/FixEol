using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FixEol
{
    public sealed class FileProcessor : IAsyncDisposable
    {
        static readonly EnumerationOptions Options = new() { IgnoreInaccessible = true, RecurseSubdirectories = true };
        readonly TempDirManager _tempDirManager = new();

        public bool NoChanges { get; init; } = false;

        #region IDisposable Members

        public async ValueTask DisposeAsync()
        {
            await _tempDirManager.DisposeAsync().ConfigureAwait(false);
        }

        #endregion

        public async Task<IEnumerable<string>> ProcessFilesAsync(string[] args, Func<EncodingInformation, Stream, Stream, Task<bool>> transform)
        {
            var fileTasks = args.AsParallel()
                                .SelectMany(arg =>
                                            {
                                                try
                                                {
                                                    var attr = File.GetAttributes(arg);

                                                    if (FileAttributes.Directory == (attr & FileAttributes.Directory))
                                                        return Directory.EnumerateFiles(arg, "*.txt", SearchOption.AllDirectories);

                                                    if (0 == (attr & (FileAttributes.ReadOnly | FileAttributes.Offline | FileAttributes.ReparsePoint)))
                                                    {
                                                        var fileInfo = new FileInfo(arg);

                                                        return new[] { fileInfo.FullName };
                                                    }
                                                }
                                                catch (IOException)
                                                { }

                                                // Assume it is a glob...
                                                try
                                                {
                                                    return Directory.EnumerateFiles(".", arg, Options);
                                                }
                                                catch (IOException)
                                                { }

                                                return Array.Empty<string>();
                                            })
                                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                                .Select(filename => ProcessFileAsync(filename, transform))
                                .ToArray();

            await Task.WhenAll(fileTasks).ConfigureAwait(false);

            return fileTasks.Select(fileTask => fileTask.Result)
                            .Where(fileName => null != fileName)
                            .ToArray();
        }

        async Task<string> ProcessFileAsync(string filename, Func<EncodingInformation, Stream, Stream, Task<bool>> transform)
        {
            var fileInfo = new FileInfo(filename);

            if (fileInfo.Length < 1)
                return fileInfo.FullName;

            var lastWrite = fileInfo.LastWriteTimeUtc;
            var lastLength = fileInfo.Length;

            try
            {
                await using var inputStream = await CreateReadStreamAsync(fileInfo).ConfigureAwait(false);

                var encoding = await EncodingInformation.DetectEncodingAsync(inputStream).ConfigureAwait(false);
                if (null == encoding)
                {
                    Console.WriteLine($"Unable to determine encoding for {fileInfo.FullName}");
                    return null;
                }

                inputStream.Seek(0, SeekOrigin.Begin);

                var tempDir = await _tempDirManager.GetDirectoryAsync(fileInfo.Directory).ConfigureAwait(false);
                var newFile = CreateTempFilename(tempDir, fileInfo);

                try
                {
                    await using (var outputStream = NoChanges
                                     ? Stream.Null
                                     : new FileStream(newFile, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                         lastLength > 1024 * 1024 ? 32 * 1024 : 4096,
                                         FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        if (!await transform(encoding, inputStream, outputStream).ConfigureAwait(false))
                            return null;
                    }

                    fileInfo.Refresh();

                    if (lastWrite == fileInfo.LastWriteTimeUtc && lastLength == fileInfo.Length)
                    {
                        if (NoChanges)
                            Trace.WriteLine($"Would replace {fileInfo.FullName}");
                        else
                            await ReplaceFileAsync(fileInfo, newFile, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (!NoChanges)
                    {
                        try
                        {
                            if (File.Exists(newFile))
                                File.Delete(newFile);
                        }
                        catch (Exception)
                        {
                            Debug.WriteLine($"Unable to cleanup {newFile} for {fileInfo.FullName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to process file {fileInfo.FullName}: {ex.Message}");
            }

            return fileInfo.FullName;
        }

        static async Task ReplaceFileAsync(FileInfo fileInfo, string newFile, CancellationToken cancellationToken)
        {
            var fullName = fileInfo.FullName;

            var backupFileName = fullName + ".bak";

            File.Delete(backupFileName);

            File.Replace(newFile, fullName, backupFileName);

            for (var retry = 0; ; ++retry)
            {
                if (retry > 0)
                    await Delays.LongDelay(TimeSpan.FromMilliseconds(100 << retry), cancellationToken).ConfigureAwait(false);

                try
                {
                    File.Delete(backupFileName);
                    break;
                }
                catch (IOException ex)
                {
                    if (retry >= 4)
                        throw;

                    Debug.WriteLine($"Unable to delete backup file {backupFileName}: {ex.Message}");
                }
            }
        }

        static async Task<Stream> CreateReadStreamAsync(FileInfo fileInfo)
        {
            var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (fileInfo.Length > 512 * 1024)
                return fileStream;

            await using (fileStream)
            {
                var ms = new MemoryStream((int)fileInfo.Length);

                await fileStream.CopyToAsync(ms).ConfigureAwait(false);

                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }
        }

        static string CreateTempFilename(DirectoryInfo tempDir, FileInfo fileInfo)
        {
            var baseTempFilename = Path.Combine(tempDir.FullName, Path.GetFileNameWithoutExtension(fileInfo.Name));

            for (var retry = 0; retry < 4; ++retry)
            {
                var tempFilename = Path.ChangeExtension(baseTempFilename, Guid.NewGuid().ToString("N"));

                if (!File.Exists(tempFilename))
                    return tempFilename;
            }

            throw new IOException("Unable to create temporary filename");
        }
    }
}
