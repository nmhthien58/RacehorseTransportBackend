using RacehorseTransport.Repositories.Context;
using RacehorseTransport.Repositories.Interfaces;

namespace RacehorseTransport.Repositories.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

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

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action)
        {
            // Đã ở trong transaction (service gọi lồng nhau) thì dùng chung, không mở cái mới.
            if (_context.Database.CurrentTransaction != null)
            {
                return await action();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                // Bỏ các thay đổi còn treo để DbContext không ghi lại chúng ở lần SaveChanges sau.
                _context.ChangeTracker.Clear();
                throw;
            }
        }
    }
}
