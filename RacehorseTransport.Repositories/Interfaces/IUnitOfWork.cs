namespace RacehorseTransport.Repositories.Interfaces
{
    // Mỗi lệnh Add/Update/Delete của repository tự SaveChanges.
    // Khi một nghiệp vụ ghi nhiều bảng (ví dụ duyệt đơn + tạo thông báo), bọc trong transaction này
    // để hoặc tất cả cùng thành công, hoặc tất cả cùng rollback.
    public interface IUnitOfWork
    {
        Task ExecuteInTransactionAsync(Func<Task> action);
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action);

        // Việc chỉ được làm khi dữ liệu đã thật sự lưu (ví dụ đẩy thông báo realtime qua SignalR).
        // Đang trong transaction: chờ tới khi transaction ngoài cùng commit; rollback thì bỏ.
        // Không có transaction: chạy ngay.
        Task RunAfterCommitAsync(Func<Task> callback);

        // Khóa theo tên, giữ tới khi transaction hiện tại kết thúc (pg_advisory_xact_lock).
        // Dùng để hai request cùng lúc cho cùng một việc phải xếp hàng, ví dụ chặn báo trùng sự cố.
        Task LockAsync(string key);
    }
}
