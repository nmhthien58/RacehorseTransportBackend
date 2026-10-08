using RacehorseTransport.API.Extensions;
using RacehorseTransport.API.Middlewares;
using RacehorseTransport.Repositories.Context;

namespace RacehorseTransport.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Cấu hình riêng của từng máy (chuỗi kết nối có mật khẩu), không commit lên git
            // Thêm lại biến môi trường và tham số dòng lệnh phía sau để chúng vẫn được ưu tiên hơn file (dùng khi deploy).
            builder.Configuration
                .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            // Chi tiết từng phần nằm trong Extensions/ServiceCollectionExtensions.cs
            builder.Services.AddRepositories(builder.Configuration);
            builder.Services.AddCaching(builder.Configuration);
            builder.Services.AddBusinessServices(builder.Configuration);
            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddApiControllers();
            builder.Services.AddFrontendCors(builder.Configuration);
            builder.Services.AddSwaggerDocs();

            var app = builder.Build();

            app.Logger.LogInformation("Database: {Database}", ServiceCollectionExtensions.DescribeDatabase(app.Configuration));

            // Configure HTTP request pipeline
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Racehorse Transport API v1");
                c.RoutePrefix = "swagger";
            });

            app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

            // GET /health -> 200 nếu API chạy và kết nối được database, ngược lại 503
            app.MapGet("/health", async (AppDbContext db) =>
            {
                bool databaseOk = await db.Database.CanConnectAsync();
                var body = new { status = databaseOk ? "Healthy" : "Unhealthy", database = databaseOk, timestamp = DateTime.UtcNow };
                return databaseOk ? Results.Ok(body) : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
            }).ExcludeFromDescription();

            app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
