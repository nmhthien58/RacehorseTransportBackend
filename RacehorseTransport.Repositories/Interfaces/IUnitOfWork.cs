namespace RacehorseTransport.Repositories.Interfaces
{
    // Mỗi lệnh Add/Update/Delete của repository tự SaveChanges.
    // Khi một nghiệp vụ ghi nhiều bảng (ví dụ duyệt đơn + tạo thông báo), bọc trong transaction này
    // để hoặc tất cả cùng thành công, hoặc tất cả cùng rollback.
    public interface IUnitOfWork
    {
        Task ExecuteInTransactionAsync(Func<Task> action);
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action);
    }
}
