using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Exceptions;

namespace RacehorseTransport.Services.Extensions
{
    public static class QueryableExtensions
    {
        // sort = "name,-createdAt": sắp theo name tăng dần rồi createdAt giảm dần
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortQuery, string defaultSortField = "Id")
        {
            if (string.IsNullOrWhiteSpace(sortQuery))
            {
                var defaultProp = typeof(T).GetProperty(defaultSortField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                           ?? typeof(T).GetProperties().FirstOrDefault();

                if (defaultProp != null)
                {
                    return OrderByProperty(query, defaultProp.Name, descending: false, isFirst: true);
                }
                return query;
            }

            var sortParams = sortQuery.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool isFirst = true;

            foreach (var param in sortParams)
            {
                bool descending = param.StartsWith("-");
                string propertyName = descending ? param[1..] : param;

                var prop = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || (prop.PropertyType != typeof(string) && !prop.PropertyType.IsValueType))
                {
                    throw new BusinessException($"Unknown sort field '{propertyName}'.");
                }

                query = OrderByProperty(query, prop.Name, descending, isFirst);
                isFirst = false;
            }

            return query;
        }

        // Đếm tổng, cắt trang và map Entity -> BusinessModel
        public static async Task<PagedBusinessResult<TModel>> ToPagedResultAsync<TEntity, TModel>(
            this IQueryable<TEntity> query,
            BusinessQueryOptions options,
            Func<TEntity, TModel> map)
        {
            int page = options.Page > 0 ? options.Page : 1;
            int size = options.Size > 0 ? Math.Min(options.Size, BusinessQueryOptions.MaxSize) : 10;

            int totalItems = await query.CountAsync();
            var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            return new PagedBusinessResult<TModel>
            {
                Items = items.Select(map).ToList(),
                Page = page,
                PageSize = size,
                TotalItems = totalItems
            };
        }

        private static IQueryable<T> OrderByProperty<T>(IQueryable<T> source, string propertyName, bool descending, bool isFirst)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.PropertyOrField(parameter, propertyName);
            var lambda = Expression.Lambda(property, parameter);

            string methodName = isFirst
                ? (descending ? "OrderByDescending" : "OrderBy")
                : (descending ? "ThenByDescending" : "ThenBy");

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(lambda));

            return source.Provider.CreateQuery<T>(resultExpression);
        }
    }
}
