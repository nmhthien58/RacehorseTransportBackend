using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Repositories.Context;
using RacehorseTransport.Repositories.Interfaces;

namespace RacehorseTransport.Repositories.Implementations
{
    public class CodeCounterRepository : ICodeCounterRepository
    {
        private readonly AppDbContext _context;

        public CodeCounterRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> NextValueAsync(string prefix, int year)
        {
            // Một lệnh UPSERT duy nhất: PostgreSQL khóa dòng trong lúc cập nhật nên không thể có hai request nhận cùng một số.
            var values = await _context.Database
                .SqlQuery<int>($@"
                    INSERT INTO common.code_counters (prefix, year, last_value)
                    VALUES ({prefix}, {year}, 1)
                    ON CONFLICT (prefix, year) DO UPDATE SET last_value = common.code_counters.last_value + 1
                    RETURNING last_value AS ""Value""")
                .ToListAsync();

            return values.Single();
        }
    }
}
