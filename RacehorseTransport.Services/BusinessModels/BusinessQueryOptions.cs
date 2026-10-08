namespace RacehorseTransport.Services.BusinessModels
{
    public class BusinessQueryOptions
    {
        public const int MaxSize = 100;

        public string? Search { get; set; }
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
    }

    public class PagedBusinessResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    }
}
