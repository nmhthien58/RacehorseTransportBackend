using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RacehorseTransport.Services.Extensions
{
    internal static class DbUpdateExceptionExtensions
    {
        public static bool IsUniqueViolation(this DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            };
        }

        public static bool IsForeignKeyViolation(this DbUpdateException exception)
        {
            return exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ForeignKeyViolation
            };
        }
    }
}
