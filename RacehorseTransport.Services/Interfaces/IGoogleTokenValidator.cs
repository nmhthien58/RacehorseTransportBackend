using RacehorseTransport.Services.BusinessModels;

namespace RacehorseTransport.Services.Interfaces
{
    // Kiểm tra ID token mà frontend nhận được từ nút "Đăng nhập bằng Google"
    public interface IGoogleTokenValidator
    {
        // Token hợp lệ (đúng chữ ký của Google, đúng ứng dụng của mình, chưa hết hạn, email đã được Google xác nhận)
        // thì trả về thông tin người dùng; ngược lại ném UnauthorizedException.
        Task<GoogleUserInfo> ValidateAsync(string idToken);
    }
}
