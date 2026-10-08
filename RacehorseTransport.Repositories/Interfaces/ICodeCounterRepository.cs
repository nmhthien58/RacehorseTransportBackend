namespace RacehorseTransport.Repositories.Interfaces
{
    // common.code_counters
    public interface ICodeCounterRepository
    {
        // Tăng bộ đếm của (prefix, year) lên 1 và trả về giá trị mới. An toàn khi nhiều request chạy cùng lúc.
        Task<int> NextValueAsync(string prefix, int year);
    }
}
