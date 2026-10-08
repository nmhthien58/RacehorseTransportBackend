namespace RacehorseTransport.Services.Interfaces
{
    // Sinh mã nghiệp vụ dạng PREFIX-NĂM-SỐ, ví dụ BKG-2026-0001. Tiền tố lấy từ CodePrefixes.
    public interface ICodeGenerator
    {
        Task<string> NextAsync(string prefix);
    }
}
