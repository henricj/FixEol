using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FixEol
{
    public sealed class FileProcessor : IDisposable
    {
        readonly TempDirManager _tempDirManager = new TempDirManager();

        #region IDisposable Members

        public void Dispose()
        {
            _tempDirManager.Dispose();
        }

        #endregion

        public async Task<IEnumerable<string>> ProcessFilesAsync(string[] args, Func<Stream, Stream, Task<bool>> transform)
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

                                                return new string[] { };
                                            })
                                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                                .Select(filename => ProcessFileAsync(filename, transform))
                                .ToArray();

            await Task.WhenAll(fileTasks).ConfigureAwait(false);

            return fileTasks.Select(fileTask => fileTask.Result)
                            .Where(fileName => null != fileName)
                            .ToArray();
        }

        async Task<string> ProcessFileAsync(string filename, Func<Stream, Stream, Task<bool>> transform)
        {
            var fileInfo = new FileInfo(filename);

            if (fileInfo.Length < 1)
                return fileInfo.FullName;

            var lastWrite = fileInfo.LastWriteTimeUtc;
            var lastLength = fileInfo.Length;

            var tempDirTask = _tempDirManager.GetDirectoryAsync(fileInfo.Directory);

            var inputStreamTask = CreateReadStreamAsync(fileInfo);

            try
            {
                var tempDir = await tempDirTask.ConfigureAwait(false);

                var newFile = CreateTempFilename(tempDir, fileInfo);

                try
                {
                    using (var outputStream = new FileStream(newFile, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                        lastLength > 1024 * 1024 ? 32 * 1024 : 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        using (var inputStream = await inputStreamTask.ConfigureAwait(false))
                        {
                            if (!await transform(inputStream, outputStream).ConfigureAwait(false))
                                return null;
                        }
                    }

                    fileInfo.Refresh();

                    if (lastWrite == fileInfo.LastWriteTimeUtc && lastLength == fileInfo.Length)
                        ReplaceFile(fileInfo, newFile, tempDir);
                }
                finally
                {
                    try
                    {
                        if (File.Exists(newFile))
                            File.Delete(newFile);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Unable to cleanup {0} for {1}", newFile, fileInfo.FullName);
                    }
                }
            }
            catch
            {
                var t = Task.Run(async () =>
                                       {
                                           try
                                           {
                                               (await inputStreamTask.ConfigureAwait(false)).Dispose();
                                           }
                                           catch (Exception)
                                           {
                                               Debug.WriteLine("Input stream cleanup failed");
                                           }
                                       });
                throw;
            }

            return fileInfo.FullName;
        }

        static void ReplaceFile(FileInfo fileInfo, string newFile, DirectoryInfo tempDir)
        {
            File.SetCreationTimeUtc(newFile, fileInfo.CreationTimeUtc);
            File.SetLastWriteTimeUtc(newFile, fileInfo.LastWriteTimeUtc);
            File.SetLastAccessTimeUtc(newFile, fileInfo.LastAccessTimeUtc);

            var fullName = fileInfo.FullName;

            var backupFileName = CreateTempFilename(tempDir, fileInfo);

            File.Move(fullName, backupFileName);

            try
            {
                File.Move(newFile, fullName);
            }
            catch (IOException)
            {
                // Restore the original file to it's original name.
                File.Move(backupFileName, fullName);

                throw;
            }

            try
            {
                File.Delete(backupFileName);
            }
            catch (IOException)
            {
                Debug.WriteLine("Unable to delete backup file");
            }
        }

        static async Task<Stream> CreateReadStreamAsync(FileInfo fileInfo)
        {
            var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.None, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (fileInfo.Length > 512 * 1024)
                return fileStream;

            using (fileStream)
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
