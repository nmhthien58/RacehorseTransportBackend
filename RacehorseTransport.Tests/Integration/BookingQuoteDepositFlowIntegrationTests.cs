using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using RacehorseTransport.API.Extensions;
using RacehorseTransport.Repositories.Constants;
using RacehorseTransport.Services.BusinessModels;
using RacehorseTransport.Services.Interfaces;
using Xunit;

namespace RacehorseTransport.Tests.Integration;

public class BookingQuoteDepositFlowIntegrationTests
{
    [PostgreSqlIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public async Task GroundBooking_FollowsReviewRouteQuoteAcceptanceAndDepositFlow()
    {
        string adminConnection = Environment.GetEnvironmentVariable("POSTGRES_INTEGRATION_CONNECTION")
            ?? "Host=127.0.0.1;Port=5433;Database=postgres;Username=postgres;Password=postgres";
        string databaseName = $"racehorse_be18_{Guid.NewGuid():N}";
        var databaseConnection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName }.ConnectionString;

        await CreateDatabaseAsync(adminConnection, databaseName);
        try
        {
            await ApplySqlAsync(databaseConnection, "database/01_schema.sql");
            await ApplySqlAsync(databaseConnection, "database/02_seed.sql");

            await using var provider = BuildProvider(databaseConnection);
            await using var scope = provider.CreateAsyncScope();
            var services = scope.ServiceProvider;

            var customer = new CurrentUser { UserId = 5, Role = UserRoles.Customer };
            var manager = new CurrentUser { UserId = 15, Role = UserRoles.LogisticsManager };
            var coordinator = new CurrentUser { UserId = 17, Role = UserRoles.FleetCoordinator };

            var bookingService = services.GetRequiredService<IBookingService>();
            var routeService = services.GetRequiredService<IRoutePlanService>();
            var quoteService = services.GetRequiredService<IBookingQuoteService>();
            var contractService = services.GetRequiredService<IContractService>();
            var paymentService = services.GetRequiredService<IPaymentService>();

            var booking = await bookingService.CreateBookingAsync(new CreateBookingModel
            {
                PickupAddress = "Ba Vi integration stable",
                PickupCountryCode = "VN",
                DropoffAddress = "Vientiane integration racecourse",
                DropoffCountryCode = "LA",
                PickupWindowStart = DateTimeOffset.UtcNow.AddDays(30),
                PickupWindowEnd = DateTimeOffset.UtcNow.AddDays(32),
                DeliveryDeadline = DateTimeOffset.UtcNow.AddDays(40),
                TransportMode = TransportModes.Ground,
                DistanceKm = 760,
                PickupLat = 21.08m,
                PickupLng = 105.38m,
                DropoffLat = 17.97m,
                DropoffLng = 102.60m,
                Recipient = new RecipientInputModel { RecipientIsCustomer = true },
                AcceptPolicy = true,
                PolicyVersion = "1.1",
                Horses = new List<CreateBookingHorseModel>
                {
                    new() { HorseId = 1, StallClass = "Shared", InsurancePlan = "None" }
                }
            }, customer);

            Assert.Equal(BookingStatuses.Submitted, booking.Status);

            booking = await bookingService.ApproveBookingAsync(booking.BookingId, 16, manager);
            Assert.Equal(BookingStatuses.PreApproved, booking.Status);

            var route = await routeService.CreateAsync(booking.BookingId, new SaveRoutePlanModel
            {
                Notes = "BE-18 PostgreSQL integration route",
                Stops = new List<RoutePlanStopInputModel>
                {
                    new()
                    {
                        Name = "Cau Treo border gate",
                        StopType = "BorderGate",
                        Address = "Ha Tinh, Vietnam",
                        Lat = 18.38m,
                        Lng = 105.18m,
                        CountryCode = "VN",
                        RestMinutes = 60
                    }
                }
            }, coordinator);
            route = await routeService.SubmitAsync(route.RoutePlanId, coordinator);
            Assert.Equal("Submitted", route.Status);

            route = await routeService.ApproveAsync(route.RoutePlanId, manager);
            Assert.Equal("Approved", route.Status);

            booking = await bookingService.GetBookingByIdAsync(booking.BookingId, customer);
            Assert.Equal(BookingStatuses.Quoted, booking.Status);
            Assert.True(booking.EstimatedCost > 0);

            var contract = await contractService.GetCurrentAsync(booking.BookingId, customer);
            booking = await quoteService.AcceptQuoteAsync(booking.BookingId, new ContractAcceptanceModel
            {
                ContractId = contract.ContractId,
                ContractSha256 = contract.FileSha256,
                AcceptPolicy = true,
                PolicyVersion = "1.1",
                IpAddress = "127.0.0.1",
                UserAgent = "BE-18 integration test"
            }, customer);
            Assert.Equal(BookingStatuses.AwaitingDeposit, booking.Status);

            var payment = await paymentService.CreatePaymentAsync(booking.BookingId, PaymentTypes.Deposit, "127.0.0.1", customer);
            var callback = await paymentService.CompleteMockPaymentAsync(payment.Payment.TxnRef, true, customer);
            Assert.True(callback.Success);

            booking = await bookingService.GetBookingByIdAsync(booking.BookingId, customer);
            Assert.Equal(BookingStatuses.Approved, booking.Status);
            Assert.Equal(booking.Payment.DepositAmount, booking.Payment.DepositPaid);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await DropDatabaseAsync(adminConnection, databaseName);
        }
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Integration"] = connectionString,
                ["Database:Use"] = "Integration",
                ["Payment:Provider"] = "Mock",
                ["Email:Provider"] = "Disabled",
                ["Email:FrontendBaseUrl"] = "http://localhost:5173"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddRepositories(configuration);
        services.AddCaching(configuration);
        services.AddBusinessServices(configuration);
        services.RemoveAll<IGeoService>();
        services.AddScoped<IGeoService, IntegrationGeoService>();
        services.RemoveAll<IFileStorageService>();
        services.AddSingleton<IFileStorageService, IntegrationFileStorageService>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task ApplySqlAsync(string connectionString, string relativePath)
    {
        string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        string sql = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, relativePath));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class IntegrationGeoService : IGeoService
    {
        public Task<IReadOnlyList<GeoPlaceModel>> SearchAsync(string query, string? countryCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GeoPlaceModel>>(Array.Empty<GeoPlaceModel>());

        public Task<GeoPlaceModel?> ReverseAsync(double lat, double lng, CancellationToken cancellationToken = default) =>
            Task.FromResult<GeoPlaceModel?>(null);

        public Task<GeoRouteModel> RouteAsync(IReadOnlyList<GeoPointModel> points, bool includeCountries = false, CancellationToken cancellationToken = default)
        {
            var legs = points.Zip(points.Skip(1), (from, to) => new GeoRouteLegModel
            {
                DistanceKm = 380,
                DurationHours = 7,
                Provider = "Integration",
                Polyline = new List<GeoPointModel> { from, to }
            }).ToList();
            return Task.FromResult(new GeoRouteModel
            {
                Legs = legs,
                TotalDistanceKm = legs.Sum(x => x.DistanceKm),
                TotalDurationHours = legs.Sum(x => x.DurationHours),
                Polyline = points.ToList(),
                CountryCodes = includeCountries ? new List<string> { "VN", "LA" } : null
            });
        }

        public Task<IReadOnlyList<string>> CountriesAlongAsync(IReadOnlyList<GeoPointModel> polyline, CancellationToken cancellationToken = default)
        {
            bool reachesLaos = polyline.LastOrDefault()?.CountryCode == "LA";
            IReadOnlyList<string> result = reachesLaos ? new[] { "VN", "LA" } : new[] { "VN" };
            return Task.FromResult(result);
        }
    }

    private sealed class IntegrationFileStorageService : IFileStorageService
    {
        public Task<StoredFileModel> UploadAsync(Stream content, string fileName, long sizeBytes, string category) =>
            Task.FromResult(new StoredFileModel
            {
                Path = $"{category}/integration/{fileName}",
                Url = $"https://integration.invalid/{category}/{fileName}",
                UrlExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                FileName = fileName,
                ContentType = "application/pdf",
                SizeBytes = sizeBytes
            });

        public Task<SignedFileUrlModel> CreateSignedUrlAsync(string path) =>
            Task.FromResult(new SignedFileUrlModel { Path = path, Url = $"https://integration.invalid/{path}", UrlExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });

        public Task<IReadOnlyDictionary<string, string>> GetViewUrlsAsync(IEnumerable<string?> paths) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(paths.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct()!
                .ToDictionary(x => x!, x => $"https://integration.invalid/{x}"));

        public bool IsValidPath(string? path, string? category = null) => !string.IsNullOrWhiteSpace(path);
    }
}

public sealed class PostgreSqlIntegrationFactAttribute : FactAttribute
{
    public PostgreSqlIntegrationFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_POSTGRES_INTEGRATION_TESTS"), "1", StringComparison.Ordinal))
        {
            Skip = "Set RUN_POSTGRES_INTEGRATION_TESTS=1 to run against PostgreSQL on port 5433.";
        }
    }
}
