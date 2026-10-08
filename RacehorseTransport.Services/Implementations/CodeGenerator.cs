using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.Interfaces;

namespace RacehorseTransport.Services.Implementations
{
    public class CodeGenerator : ICodeGenerator
    {
        private readonly ICodeCounterRepository _counterRepo;

        public CodeGenerator(ICodeCounterRepository counterRepo)
        {
            _counterRepo = counterRepo;
        }

        public async Task<string> NextAsync(string prefix)
        {
            int year = DateTimeOffset.UtcNow.Year;
            int number = await _counterRepo.NextValueAsync(prefix, year);

            // Gọi bên trong IUnitOfWork.ExecuteInTransactionAsync thì nghiệp vụ lỗi sẽ rollback luôn bộ đếm,
            // không bị nhảy số.
            return $"{prefix}-{year}-{number:D4}";
        }
    }
}
