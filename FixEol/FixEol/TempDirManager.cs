using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FixEol
{
    public sealed class TempDirManager
    {
        readonly ConcurrentDictionary<string, Task<DirectoryInfo>> _tempDirs = new ConcurrentDictionary<string, Task<DirectoryInfo>>(StringComparer.InvariantCultureIgnoreCase);

        public void Dispose()
        {
            var cleanupTasks = new List<Task>();

            foreach (var dirTask in _tempDirs.Values)
            {
                if (dirTask.IsFaulted || dirTask.IsCanceled)
                {
                    var ex = dirTask.Exception;

                    if (null != ex)
                        Debug.WriteLine("Directory cleanup failed: " + ex.Message);

                    continue;
                }

                var cleanupTask = dirTask.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            var ex = t.Exception;

                            if (null != ex)
                                Debug.WriteLine("Directory cleanup failed: " + ex.Message);

                            return;
                        }

                        var directoryInfo = t.Result;

                        try
                        {
                            directoryInfo.Refresh();

                            if (directoryInfo.Exists)
                                directoryInfo.Delete(true);
                        }
                        catch (IOException ex)
                        {
                            Debug.WriteLine("Directory cleanup of {0} failed: {1}", directoryInfo.FullName, ex.Message);
                        }
                    });

                cleanupTasks.Add(cleanupTask);
            }

            try
            {
                Task.WaitAll(cleanupTasks.ToArray());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Directory cleanup failed: " + ex.Message);
            }
        }

        public Task<DirectoryInfo> GetDirectoryAsync(DirectoryInfo parentDirectory)
        {
            for (; ; )
            {
                Task<DirectoryInfo> directoryInfoTask;
                if (_tempDirs.TryGetValue(parentDirectory.FullName, out directoryInfoTask))
                    return directoryInfoTask;

                var task = new Task<Task<DirectoryInfo>>(
                    async () =>
                    {
                        for (var retry = 0; retry < 5; ++retry)
                        {
                            var path = Path.Combine(parentDirectory.FullName, "temp-" + Path.GetRandomFileName());

                            var directoryInfo = new DirectoryInfo(path);

                            if (directoryInfo.Exists)
                                return directoryInfo;

                            try
                            {
                                directoryInfo.Create();

                                return directoryInfo;
                            }
                            catch (IOException)
                            {
                                if (retry > 2)
                                    throw;
                            }

                            await Delays.ShortDelay().ConfigureAwait(false);
                        }

                        throw new IOException("Unable to create directory");
                    });

                if (_tempDirs.TryAdd(parentDirectory.FullName, task.Unwrap()))
                {
                    task.Start();

                    return task.Unwrap();
                }
            }
        }
    }
}
