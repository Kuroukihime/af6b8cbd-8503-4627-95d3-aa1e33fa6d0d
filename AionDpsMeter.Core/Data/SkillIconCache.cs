using System.Collections.Concurrent;
using System.Net.Http;

namespace AionDpsMeter.Core.Data
{
    public sealed class SkillIconCache : IDisposable
    {
        private static SkillIconCache? instance;
        private static readonly object InstanceLock = new();

        public static SkillIconCache Instance
        {
            get
            {
                if (instance is null)
                    lock (InstanceLock)
                        instance ??= new SkillIconCache();
                return instance;
            }
        }

        private readonly string cacheDirectory;
        private readonly HttpClient httpClient;

       
        private readonly ConcurrentDictionary<string, string> memoryCache = new(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, SemaphoreSlim> downloadLocks = new(StringComparer.Ordinal);

        private SkillIconCache()
        {
            cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconCache", "Skills");
            Directory.CreateDirectory(cacheDirectory);

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0");

        }

        public async Task<string?> GetLocalPathAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

          
            if (memoryCache.TryGetValue(url, out var cached))
                return cached;

            var fileName = SanitizeFileName(url);
            var localPath = Path.Combine(cacheDirectory, fileName);

            if (File.Exists(localPath))
            {
                memoryCache[url] = localPath;
                return localPath;
            }

            var semaphore = downloadLocks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (File.Exists(localPath))
                {
                    memoryCache[url] = localPath;
                    return localPath;
                }

                var bytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                await File.WriteAllBytesAsync(localPath, bytes).ConfigureAwait(false);
                memoryCache[url] = localPath;
                return localPath;
            }
            catch
            {
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public string? GetLocalPathOrStartDownload(string url, Action? onDownloaded = null)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (memoryCache.TryGetValue(url, out var cached))
                return cached;

            var fileName = SanitizeFileName(url);
            var localPath = Path.Combine(cacheDirectory, fileName);
            if (File.Exists(localPath))
            {
                memoryCache[url] = localPath;
                return localPath;
            }

            _ = Task.Run(async () =>
            {
                var result = await GetLocalPathAsync(url).ConfigureAwait(false);
                if (result is not null)
                    onDownloaded?.Invoke();
            });

            return null;
        }

        private static string SanitizeFileName(string url)
        {
          
            var lastSlash = url.LastIndexOf('/');
            var name = lastSlash >= 0 ? url[(lastSlash + 1)..] : url;

            var q = name.IndexOf('?');
            if (q >= 0) name = name[..q];

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }
    }
}
