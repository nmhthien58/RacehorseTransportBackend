using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RacehorseTransport.API.BackgroundJobs;
using RacehorseTransport.API.Hubs;
using RacehorseTransport.API.ResponseModels;
using RacehorseTransport.Repositories.Context;
using RacehorseTransport.Repositories.Implementations;
using RacehorseTransport.Repositories.Interfaces;
using RacehorseTransport.Services.Implementations;
using RacehorseTransport.Services.Interfaces;
using RacehorseTransport.Services.Options;
using StackExchange.Redis;

namespace RacehorseTransport.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string CorsPolicyName = "Frontend";

        // 1. DbContext + Repositories
        public static IServiceCollection AddRepositories(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = ResolveConnectionString(configuration);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention(); // BookingId <-> booking_id
            });

            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICodeCounterRepository, CodeCounterRepository>();

            return services;
        }

        // Chọn database: "Database:Use" là TÊN một chuỗi trong ConnectionStrings (ví dụ "Local" hoặc "Supabase").
        // Bỏ trống thì dùng "DefaultConnection" (bản deploy đặt biến ConnectionStrings__DefaultConnection).
        public static string ResolveConnectionString(IConfiguration configuration)
        {
            string name = configuration["Database:Use"] is { Length: > 0 } use ? use : "DefaultConnection";
            string? connectionString = configuration.GetConnectionString(name);

            if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains('<'))
            {
                throw new InvalidOperationException(
                    $"ConnectionStrings:{name} is empty or still has <placeholders>. Fill it in appsettings.Local.json (see appsettings.Local.example.json), or change Database:Use.");
            }

            return connectionString;
        }

        // Tên database đang dùng, để ghi ra log lúc khởi động (không bao giờ ghi mật khẩu)
        public static string DescribeDatabase(IConfiguration configuration)
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(ResolveConnectionString(configuration));
            string name = configuration["Database:Use"] is { Length: > 0 } use ? use : "DefaultConnection";
            return $"{name} -> {builder.Database} @ {builder.Host}:{builder.Port}";
        }

        // 2. Services (nghiệp vụ). MỖI SERVICE MỚI PHẢI ĐĂNG KÝ THÊM 1 DÒNG Ở ĐÂY.
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BookingOptions>(configuration.GetSection(BookingOptions.SectionName));
            services.Configure<TransitOptions>(configuration.GetSection(TransitOptions.SectionName));
            services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
            services.Configure<VnPayOptions>(configuration.GetSection(VnPayOptions.SectionName));
            services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.SectionName));
            services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
            services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));

            // Hạ tầng dùng chung
            services.AddScoped<ICacheService, DistributedCacheService>();
            services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
            services.AddSingleton<ITokenService, JwtTokenService>();
            services.AddScoped<ICodeGenerator, CodeGenerator>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<INotificationReadService, NotificationReadService>();

            // Chuông realtime: thông báo mới được đẩy qua SignalR hub /hubs/notifications
            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
            services.AddSingleton<INotificationPusher, SignalRNotificationPusher>();
            services.AddHttpClient<IFileStorageService, SupabaseFileStorageService>(client => client.Timeout = TimeSpan.FromSeconds(30));

            // Đăng nhập bằng Google: kiểm tra ID token bằng khóa công khai của Google
            services.AddHttpClient<IGoogleTokenValidator, GoogleIdTokenValidator>(client => client.Timeout = TimeSpan.FromSeconds(15));

            // Email: mail được xếp hàng rồi EmailDispatchJob gửi ở nền. Cách gửi chọn bằng Email:Provider.
            services.AddSingleton<IEmailQueue, EmailQueue>();
            services.AddScoped<IEmailService, EmailService>();
            string emailProvider = configuration[$"{EmailOptions.SectionName}:Provider"] ?? string.Empty;
            if (emailProvider.Equals(EmailProviders.GmailApi, StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<IEmailSender, GmailApiEmailSender>(client => client.Timeout = TimeSpan.FromSeconds(30));
            }
            else if (emailProvider.Equals(EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IEmailSender, SmtpEmailSender>();
            }
            else
            {
                services.AddScoped<IEmailSender, DisabledEmailSender>();
            }

            // Nghiệp vụ
            services.AddHttpClient<IDistanceService, DistanceService>(client => client.Timeout = TimeSpan.FromSeconds(5));

            // Bản đồ (BE-01): Goong cho Việt Nam, Nominatim + OSRM cho phần còn lại. Thứ tự đăng ký = thứ tự ưu tiên.
            services.Configure<GeoOptions>(configuration.GetSection(GeoOptions.SectionName));
            // Goong nhận khóa trên URL: tắt log mặc định của HttpClient (log in cả URL) để khóa không lộ ra log
            services.AddHttpClient<GoongGeoProvider>().RemoveAllLoggers();
            services.AddHttpClient<NominatimGeocodingProvider>();
            services.AddHttpClient<OsrmRoutingProvider>();
            services.AddScoped<IGeocodingProvider>(sp => sp.GetRequiredService<GoongGeoProvider>());
            services.AddScoped<IGeocodingProvider>(sp => sp.GetRequiredService<NominatimGeocodingProvider>());
            services.AddScoped<IRoutingProvider>(sp => sp.GetRequiredService<GoongGeoProvider>());
            services.AddScoped<IRoutingProvider>(sp => sp.GetRequiredService<OsrmRoutingProvider>());
            services.AddScoped<IGeoService, GeoService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDelayCompensationService, DelayCompensationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoutePlanService, RoutePlanService>();
            services.AddScoped<IBookingQuoteService, BookingQuoteService>();
            services.AddScoped<IHorseService, HorseService>();
            services.AddScoped<IHorseDocumentService, HorseDocumentService>();
            services.AddSingleton<ITransitTimeEstimator, TransitTimeEstimator>();
            services.AddScoped<IClearanceScheduleService, ClearanceScheduleService>();

            // Thanh toán (R-02): cổng VNPay thật + cổng giả lập; PaymentGatewayResolver chọn cổng theo cấu hình
            services.AddScoped<ISystemSettingService, SystemSettingService>();
            services.AddHttpClient<VnPayGateway>(client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<VnPayGateway>());
            services.AddScoped<IPaymentGateway, MockPaymentGateway>();
            services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();
            services.AddScoped<IBookingActivator, BookingActivator>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IPricingService, PricingService>();
            services.AddScoped<IPriceItemService, PriceItemService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<ITripService, TripService>();
            services.AddScoped<ITripPlanningService, TripPlanningService>();
            services.AddScoped<ITripExecutionService, TripExecutionService>();
            services.AddScoped<ITrackingService, TrackingService>();
            services.AddScoped<IIncidentService, IncidentService>();
            services.AddScoped<ITripCompletionService, TripCompletionService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IDossierService, DossierService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<ITransportAssetService, TransportAssetService>();
            services.AddScoped<ICountryDocRequirementService, CountryDocRequirementService>();
            // Việc chạy nền
            services.AddHostedService<BookingExpiryJob>();
            services.AddHostedService<EmailDispatchJob>();

            return services;
        }

        // 3. Cache: Redis nếu có ConnectionStrings:Redis, không thì cache trong bộ nhớ (đủ để chạy dev)
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            string? redisConnection = configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(redisConnection))
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    var redisOptions = ConfigurationOptions.Parse(redisConnection);
                    redisOptions.AbortOnConnectFail = false; // Redis tắt thì API vẫn khởi động được
                    redisOptions.ConnectTimeout = 2000;
                    redisOptions.SyncTimeout = 2000;
                    redisOptions.AsyncTimeout = 2000;

                    options.ConfigurationOptions = redisOptions;
                    options.InstanceName = "racehorse:";
                });
            }

            return services;
        }

        // 4. JWT authentication + authorization theo Role
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(JwtOptions.SectionName);
            var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

            if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
            {
                throw new InvalidOperationException(
                    "Jwt:Key is missing or shorter than 32 characters. Set it in appsettings.Development.json or the Jwt__Key environment variable.");
            }

            services.Configure<JwtOptions>(jwtSection);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false; // giữ nguyên tên claim ngắn: sub, email, name, role
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = JwtClaimNames.FullName,
                        RoleClaimType = JwtClaimNames.Role
                    };

                    // 401 / 403 cũng trả về khuôn ApiResponse để FE xử lý thống nhất
                    options.Events = new JwtBearerEvents
                    {
                        // WebSocket của trình duyệt không gửi được header Authorization,
                        // nên kết nối SignalR mang token trên query string ?access_token=...
                        OnMessageReceived = context =>
                        {
                            string? accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        },
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(
                                ApiResponse<object?>.ErrorResult("Authentication is required. Access token is missing, invalid or expired."));
                        },
                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(
                                ApiResponse<object?>.ErrorResult("You do not have permission to perform this action."));
                        }
                    };
                });

            services.AddAuthorization();

            return services;
        }

        // 5. Controllers + JSON + lỗi validation theo khuôn ApiResponse
        public static IServiceCollection AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    // [ApiController] tự kiểm tra ModelState; ở đây chỉ đổi định dạng lỗi trả về.
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        return new BadRequestObjectResult(ApiResponse<object?>.ErrorResult("Validation failed", errors));
                    };
                });

            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            return services;
        }

        // 6. CORS cho frontend (danh sách origin nằm ở Cors:AllowedOrigins)
        public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
        {
            string[] origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
                });
            });

            return services;
        }

        // 7. Swagger / OpenAPI có nút Authorize để dán access token
        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Racehorse Transport API",
                    Version = "v1",
                    Description = "SWP391 Group 5 - Cross-Border Racehorse Transport System"
                });

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste the access token returned by POST /api/auth/login (without the 'Bearer ' prefix).",
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                };
                c.AddSecurityDefinition("Bearer", bearerScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearerScheme, Array.Empty<string>() } });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }
    }
}
