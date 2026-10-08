using RacehorseTransport.API.RequestModels;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.API.Mappers
{
    // Dùng chung cho mọi endpoint danh sách có tìm kiếm / sắp xếp / phân trang
    public static class QueryApiMapper
    {
        public static BusinessQueryOptions ToBusinessOptions(this QueryOptionsRequest request)
        {
            return new BusinessQueryOptions
            {
                Search = request.Search,
                Sort = request.Sort,
                Page = request.Page,
                Size = request.Size
            };
        }

        public static PaginationMetadata ToPagination<T>(this PagedBusinessResult<T> result)
        {
            return new PaginationMetadata
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalPages
            };
        }
    }
}
