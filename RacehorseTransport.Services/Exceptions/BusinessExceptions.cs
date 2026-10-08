namespace RacehorseTransport.Services.Exceptions
{
    // Service ném các exception này; ExceptionHandlingMiddleware ở tầng API đổi sang HTTP status tương ứng.
    // Controller KHÔNG cần try/catch.

    // 400 - vi phạm quy tắc nghiệp vụ (dữ liệu sai, sai trạng thái, trùng mã...)
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message)
        {
        }
    }

    // 404 - không tìm thấy dữ liệu
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    // 401 - chưa đăng nhập / sai thông tin đăng nhập / token hết hạn
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message)
        {
        }
    }

    // 403 - đã đăng nhập nhưng không có quyền trên dữ liệu này (ví dụ khách xem đơn của người khác)
    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
