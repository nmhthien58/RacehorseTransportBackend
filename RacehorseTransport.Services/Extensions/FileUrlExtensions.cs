using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Extensions
{
    public static class FileUrlExtensions
    {
        // DB lưu đường dẫn file; trước khi trả model ra ngoài, đổi đường dẫn thành link xem được.
        // Không đổi được (file không còn, storage lỗi) thì gán null.
        public static async Task ResolveViewUrlsAsync<T>(
            this IFileStorageService fileStorage,
            IEnumerable<T> items,
            Func<T, string?> getPath,
            Action<T, string?> setUrl)
        {
            // Chốt đường dẫn trước khi gán: cùng một object có thể xuất hiện hai lần trong danh sách
            var pending = items
                .Select(item => (Item: item, Path: getPath(item)))
                .Where(x => !string.IsNullOrWhiteSpace(x.Path))
                .ToList();
            if (pending.Count == 0)
            {
                return;
            }

            var urls = await fileStorage.GetViewUrlsAsync(pending.Select(x => x.Path));
            foreach (var (item, path) in pending)
            {
                setUrl(item, urls.TryGetValue(path!, out string? url) ? url : null);
            }
        }
    }
}
