namespace RacehorseTransport.API.ResponseModels
{
    // Mọi response (kể cả lỗi) đều theo một khuôn: { success, message, data, errors }
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public static ApiResponse<T?> SuccessResult(T? data, string message = "Request processed successfully")
        {
            return new ApiResponse<T?>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null
            };
        }

        public static ApiResponse<T?> ErrorResult(string message, object? errors = null)
        {
            return new ApiResponse<T?>
            {
                Success = false,
                Message = message,
                Data = default,
                Errors = errors ?? new[] { message }
            };
        }
    }

    public class PaginationMetadata
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class ApiCollectionResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "Request processed successfully";
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public object? Errors { get; set; } = null;
        public PaginationMetadata Pagination { get; set; } = new();

        public static ApiCollectionResponse<T> SuccessResult(IEnumerable<T> data, PaginationMetadata pagination, string message = "Request processed successfully")
        {
            return new ApiCollectionResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Errors = null,
                Pagination = pagination
            };
        }
    }
}
