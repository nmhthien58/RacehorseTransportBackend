using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RacehorseTransport.Repositories.Entities;

namespace RacehorseTransport.Repositories.Context
{
    // Database-first trên PostgreSQL: schema do các script trong thư mục /database quản lý (KHÔNG dùng EF Migrations).
    // Khi DB đổi cột/bảng: sửa script SQL trước, sau đó sửa Entity + cấu hình ở đây cho khớp.
    // Tên cột trong DB là snake_case (booking_id), tự suy ra từ tên property (BookingId)
    // nhờ UseSnakeCaseNamingConvention() ở ServiceCollectionExtensions - không cần HasColumnName.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UserToken> UserTokens { get; set; } = null!;
        public virtual DbSet<Horse> Horses { get; set; } = null!;
        public virtual DbSet<HorseDocument> HorseDocuments { get; set; } = null!;
        public virtual DbSet<BookingRequest> BookingRequests { get; set; } = null!;
        public virtual DbSet<BookingHorse> BookingHorses { get; set; } = null!;
        public virtual DbSet<BookingTransitCountry> BookingTransitCountries { get; set; } = null!;
        public virtual DbSet<DocumentType> DocumentTypes { get; set; } = null!;
        public virtual DbSet<CountryDocRequirement> CountryDocRequirements { get; set; } = null!;
        public virtual DbSet<DigitalDossier> DigitalDossiers { get; set; } = null!;
        public virtual DbSet<DossierDocument> DossierDocuments { get; set; } = null!;
        public virtual DbSet<TransportAsset> TransportAssets { get; set; } = null!;
        public virtual DbSet<Trip> Trips { get; set; } = null!;
        public virtual DbSet<TripCrew> TripCrew { get; set; } = null!;
        public virtual DbSet<TripHorse> TripHorses { get; set; } = null!;
        public virtual DbSet<RouteCheckpoint> RouteCheckpoints { get; set; } = null!;
        public virtual DbSet<HorseWelfareLog> HorseWelfareLogs { get; set; } = null!;
        public virtual DbSet<TripIncident> TripIncidents { get; set; } = null!;
        public virtual DbSet<HandoverAcceptance> HandoverAcceptances { get; set; } = null!;
        public virtual DbSet<SystemNotification> SystemNotifications { get; set; } = null!;
        public virtual DbSet<PriceItem> PriceItems { get; set; } = null!;
        public virtual DbSet<Payment> Payments { get; set; } = null!;
        public virtual DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- auth ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users", "identity");
                entity.HasKey(e => e.UserId);
            });

            modelBuilder.Entity<UserToken>(entity =>
            {
                entity.ToTable("user_tokens", "identity");
                entity.HasKey(e => e.TokenId);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            // ---------- racehorse ----------
            modelBuilder.Entity<Horse>(entity =>
            {
                entity.ToTable("horses", "racehorse");
                entity.HasKey(e => e.HorseId);

                entity.HasOne(e => e.Owner)
                      .WithMany(u => u.Horses)
                      .HasForeignKey(e => e.OwnerUserId);
            });

            modelBuilder.Entity<HorseDocument>(entity =>
            {
                entity.ToTable("horse_documents", "racehorse");
                entity.HasKey(e => e.DocumentId);

                entity.HasOne(e => e.Horse)
                      .WithMany(h => h.Documents)
                      .HasForeignKey(e => e.HorseId);

                entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocTypeId);
                entity.HasOne(e => e.UploadedBy).WithMany().HasForeignKey(e => e.UploadedByUserId);
            });

            // ---------- booking ----------
            modelBuilder.Entity<BookingRequest>(entity =>
            {
                entity.ToTable("booking_requests", "booking");
                entity.HasKey(e => e.BookingId);
                entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
                entity.Property(e => e.DeclaredValue).HasPrecision(18, 2);
                entity.Property(e => e.EstimatedTransitHours).HasPrecision(7, 1);
                entity.Property(e => e.DepositPercent).HasPrecision(5, 2);
                entity.Property(e => e.DepositAmount).HasPrecision(18, 2);
                entity.Property(e => e.ExtraCharges).HasPrecision(18, 2);
                entity.Property(e => e.PickupLat).HasPrecision(9, 6);
                entity.Property(e => e.PickupLng).HasPrecision(9, 6);
                entity.Property(e => e.DropoffLat).HasPrecision(9, 6);
                entity.Property(e => e.DropoffLng).HasPrecision(9, 6);
                entity.Property(e => e.QuoteBreakdown).HasColumnType("jsonb");

                entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerUserId);
                entity.HasOne(e => e.ReviewedBy).WithMany().HasForeignKey(e => e.ReviewedByUserId);
                entity.HasOne(e => e.AssignedSpecialist).WithMany().HasForeignKey(e => e.AssignedSpecialistId);
                entity.HasOne(e => e.AssignedCoordinator).WithMany().HasForeignKey(e => e.AssignedCoordinatorId);
            });

            modelBuilder.Entity<BookingHorse>(entity =>
            {
                entity.ToTable("booking_horses", "booking");
                entity.HasKey(e => e.BookingHorseId);
                entity.Property(e => e.DeclaredValue).HasPrecision(18, 2);
                entity.Property(e => e.AgreedValue).HasPrecision(18, 2);

                entity.HasOne(e => e.Booking)
                      .WithMany(b => b.BookingHorses)
                      .HasForeignKey(e => e.BookingId);

                entity.HasOne(e => e.Horse)
                      .WithMany(h => h.BookingHorses)
                      .HasForeignKey(e => e.HorseId);
            });

            modelBuilder.Entity<BookingTransitCountry>(entity =>
            {
                entity.ToTable("booking_transit_countries", "booking");
                entity.HasKey(e => new { e.BookingId, e.SequenceOrder });

                entity.HasOne(e => e.Booking)
                      .WithMany(b => b.TransitCountries)
                      .HasForeignKey(e => e.BookingId);
            });

            // ---------- clearance ----------
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("document_types", "clearance");
                entity.HasKey(e => e.DocTypeId);
            });

            modelBuilder.Entity<CountryDocRequirement>(entity =>
            {
                entity.ToTable("country_doc_requirements", "clearance");
                entity.HasKey(e => e.RequirementId);

                entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocTypeId);
            });

            modelBuilder.Entity<DigitalDossier>(entity =>
            {
                entity.ToTable("digital_dossiers", "clearance");
                entity.HasKey(e => e.DossierId);

                // Mỗi con ngựa trong đơn có đúng 1 bộ hồ sơ
                entity.HasOne(e => e.BookingHorse)
                      .WithOne(bh => bh.Dossier)
                      .HasForeignKey<DigitalDossier>(e => e.BookingHorseId);

                entity.HasOne(e => e.Specialist).WithMany().HasForeignKey(e => e.SpecialistUserId);
            });

            modelBuilder.Entity<DossierDocument>(entity =>
            {
                entity.ToTable("dossier_documents", "clearance");
                entity.HasKey(e => e.DocumentId);

                entity.HasOne(e => e.Dossier)
                      .WithMany(d => d.Documents)
                      .HasForeignKey(e => e.DossierId);

                entity.HasOne(e => e.DocumentType).WithMany().HasForeignKey(e => e.DocTypeId);
                entity.HasOne(e => e.UploadedBy).WithMany().HasForeignKey(e => e.UploadedByUserId);
                entity.HasOne(e => e.ReviewedBy).WithMany().HasForeignKey(e => e.ReviewedByUserId);
            });

            // ---------- fleet ----------
            modelBuilder.Entity<TransportAsset>(entity =>
            {
                entity.ToTable("transport_assets", "fleet");
                entity.HasKey(e => e.AssetId);
            });

            modelBuilder.Entity<Trip>(entity =>
            {
                entity.ToTable("trips", "fleet");
                entity.HasKey(e => e.TripId);
                entity.Property(e => e.PlannedCost).HasPrecision(18, 2);
                entity.Property(e => e.ActualCost).HasPrecision(18, 2);
                entity.Property(e => e.KpiScore).HasPrecision(5, 2);

                entity.HasOne(e => e.Vehicle).WithMany().HasForeignKey(e => e.VehicleId);
                entity.HasOne(e => e.PlannedBy).WithMany().HasForeignKey(e => e.PlannedByUserId);
                entity.HasOne(e => e.ApprovedBy).WithMany().HasForeignKey(e => e.ApprovedByUserId);
                entity.HasOne(e => e.ClosedBy).WithMany().HasForeignKey(e => e.ClosedByUserId);
            });

            modelBuilder.Entity<TripCrew>(entity =>
            {
                entity.ToTable("trip_crew", "fleet");
                entity.HasKey(e => e.TripCrewId);

                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.Crew)
                      .HasForeignKey(e => e.TripId);

                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<TripHorse>(entity =>
            {
                entity.ToTable("trip_horses", "fleet");
                entity.HasKey(e => e.TripHorseId);

                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.TripHorses)
                      .HasForeignKey(e => e.TripId);

                entity.HasOne(e => e.BookingHorse)
                      .WithMany(bh => bh.TripHorses)
                      .HasForeignKey(e => e.BookingHorseId);

                entity.HasOne(e => e.Handover).WithMany().HasForeignKey(e => e.HandoverId);
                entity.HasOne(e => e.Escort).WithMany().HasForeignKey(e => e.EscortUserId);
            });

            modelBuilder.Entity<RouteCheckpoint>(entity =>
            {
                entity.ToTable("route_checkpoints", "fleet");
                entity.HasKey(e => e.CheckpointId);
                entity.Property(e => e.Latitude).HasPrecision(9, 6);
                entity.Property(e => e.Longitude).HasPrecision(9, 6);
                entity.Property(e => e.DistanceKmFromPrevious).HasPrecision(10, 2);

                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.Checkpoints)
                      .HasForeignKey(e => e.TripId);

                entity.HasOne(e => e.Booking).WithMany().HasForeignKey(e => e.BookingId);
            });

            // ---------- tracking ----------
            modelBuilder.Entity<HorseWelfareLog>(entity =>
            {
                entity.ToTable("horse_welfare_logs", "tracking");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.CabinTemp).HasPrecision(4, 1);
                entity.Property(e => e.WaterIntakeLiters).HasPrecision(4, 1);

                entity.HasOne(e => e.TripHorse)
                      .WithMany(th => th.WelfareLogs)
                      .HasForeignKey(e => e.TripHorseId);

                entity.HasOne(e => e.Checkpoint).WithMany().HasForeignKey(e => e.CheckpointId);
                entity.HasOne(e => e.RecordedBy).WithMany().HasForeignKey(e => e.RecordedByUserId);
            });

            // ---------- incident ----------
            modelBuilder.Entity<TripIncident>(entity =>
            {
                entity.ToTable("trip_incidents", "incident");
                entity.HasKey(e => e.IncidentId);
                entity.Property(e => e.AdditionalCost).HasPrecision(18, 2);
                entity.Property(e => e.RouteChange).HasColumnType("jsonb");

                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.Incidents)
                      .HasForeignKey(e => e.TripId);

                entity.HasOne(e => e.ReportedBy).WithMany().HasForeignKey(e => e.ReportedByUserId);
                entity.HasOne(e => e.ApprovedBy).WithMany().HasForeignKey(e => e.ApprovedByUserId);
                entity.HasOne(e => e.AffectedTripHorse).WithMany().HasForeignKey(e => e.AffectedTripHorseId);
            });

            // ---------- acceptance ----------
            modelBuilder.Entity<HandoverAcceptance>(entity =>
            {
                entity.ToTable("handover_acceptances", "acceptance");
                entity.HasKey(e => e.HandoverId);

                entity.HasOne(e => e.Trip)
                      .WithMany(t => t.Handovers)
                      .HasForeignKey(e => e.TripId);

                entity.HasOne(e => e.Booking).WithMany().HasForeignKey(e => e.BookingId);

                entity.HasOne(e => e.Driver).WithMany().HasForeignKey(e => e.DriverUserId);

                entity.HasOne(e => e.Checkpoint).WithMany().HasForeignKey(e => e.CheckpointId);
            });

            // ---------- common ----------
            modelBuilder.Entity<SystemNotification>(entity =>
            {
                entity.ToTable("system_notifications", "common");
                entity.HasKey(e => e.NotificationId);

                entity.HasOne(e => e.Recipient).WithMany().HasForeignKey(e => e.RecipientUserId);
            });

            // ---------- pricing ----------
            modelBuilder.Entity<PriceItem>(entity =>
            {
                entity.ToTable("price_items", "pricing");
                entity.HasKey(e => e.PriceItemId);
                entity.Property(e => e.Value).HasPrecision(18, 4);
            });

            // ---------- billing (R-02) ----------
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("payments", "billing");
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.ExchangeRate).HasPrecision(18, 4);
                entity.Property(e => e.RawResponse).HasColumnType("jsonb");

                entity.HasOne(e => e.Booking).WithMany(b => b.Payments).HasForeignKey(e => e.BookingId);
                entity.HasOne(e => e.RefundOf).WithMany().HasForeignKey(e => e.RefundOfPaymentId);
                entity.HasOne(e => e.CreatedBy).WithMany().HasForeignKey(e => e.CreatedByUserId);
            });

            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.ToTable("system_settings", "common");
                entity.HasKey(e => e.SettingKey);
            });

            // DB không có ON DELETE CASCADE ở bất kỳ khóa ngoại nào.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Cột timestamptz của PostgreSQL chỉ nhận giờ UTC: đổi mọi DateTimeOffset về UTC trước khi ghi.
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        }

        private sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
        {
            public UtcDateTimeOffsetConverter() : base(v => v.ToUniversalTime(), v => v)
            {
            }
        }
    }
}
