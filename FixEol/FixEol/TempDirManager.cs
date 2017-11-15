using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FixEol
{
    public sealed class TempDirManager
    {
        readonly ConcurrentDictionary<string, Task<DirectoryInfo>> _tempDirs = new ConcurrentDictionary<string, Task<DirectoryInfo>>(StringComparer.InvariantCultureIgnoreCase);

        public void Dispose()
        {
            var cleanupTasks = new List<Task>(_tempDirs.Count);

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
                    async t =>
                    {
                        if (t.IsFaulted)
                        {
                            var ex = t.Exception;

                            if (null != ex)
                                Debug.WriteLine("Directory cleanup failed: " + ex.Message);

                            return;
                        }

                        var directoryInfo = t.Result;

                        for (var retry = 0; retry < 5; ++retry)
                        {
                            if (retry > 0)
                                await Delays.LongDelay(TimeSpan.FromMilliseconds(100 << retry), CancellationToken.None).ConfigureAwait(false);

                            try
                            {
                                directoryInfo.Refresh();

                                if (directoryInfo.Exists)
                                    directoryInfo.Delete(true);
                            }
                            catch (IOException ex)
                            {
                                if (retry >= 4)
                                    throw;

                                Debug.WriteLine("Directory cleanup of {0} failed, retrying: {1}", directoryInfo.FullName, ex.Message);
                            }
                        }
                    }, TaskContinuationOptions.RunContinuationsAsynchronously);

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
                if (_tempDirs.TryGetValue(parentDirectory.FullName, out var directoryInfoTask))
                    return directoryInfoTask;

                var task = new Task<Task<DirectoryInfo>>(
                    async () =>
                    {
                        for (var retry = 0; ; ++retry)
                        {
                            var path = Path.Combine(parentDirectory.FullName, "temp-" + Path.GetRandomFileName());

                            var directoryInfo = new DirectoryInfo(path);

                            if (directoryInfo.Exists)
                                return directoryInfo;

                            if (retry > 0)
                                await Delays.LongDelay(TimeSpan.FromMilliseconds(50 << retry), CancellationToken.None).ConfigureAwait(false);

                            try
                            {
                                directoryInfo.Create();

                                return directoryInfo;
                            }
                            catch (IOException ex)
                            {
                                if (retry >= 4)
                                    throw;

                                Debug.WriteLine($"Unable to create temp directory {path}, retrying: {ex.Message}");
                            }
                        }
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
