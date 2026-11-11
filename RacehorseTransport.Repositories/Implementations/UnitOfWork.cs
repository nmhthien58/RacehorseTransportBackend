using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Context;
using RacehorseTransport.Repositories.Interfaces;

namespace RacehorseTransport.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly List<Func<Task>> _afterCommit = new();

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            await ExecuteInTransactionAsync(async () =>
            {
                await action();
                return true;
            });
        }

        public async Task LockAsync(string key)
        {
            if (_context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException("LockAsync must be called inside ExecuteInTransactionAsync.");
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({key}))");
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action)
        {
            // Đã ở trong transaction (service gọi lồng nhau) thì dùng chung, không mở cái mới.
            if (_context.Database.CurrentTransaction != null)
            {
                return await action();
            }

            TResult result;
            await using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    result = await action();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    // Bỏ các thay đổi còn treo để DbContext không ghi lại chúng ở lần SaveChanges sau.
                    _context.ChangeTracker.Clear();
                    _afterCommit.Clear();
                    throw;
                }
            }

            // Transaction đã đóng nên việc nào ở đây gọi lại RunAfterCommitAsync cũng được chạy ngay
            var callbacks = _afterCommit.ToList();
            _afterCommit.Clear();
            foreach (var callback in callbacks)
            {
                await callback();
            }

            return result;
        }

        public async Task RunAfterCommitAsync(Func<Task> callback)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                _afterCommit.Add(callback);
                return;
            }

            await callback();
        }
    }
}
