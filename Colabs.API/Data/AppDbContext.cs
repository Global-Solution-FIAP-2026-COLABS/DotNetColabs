using Colabs.Data.Converters;
using Colabs.Models;
using Microsoft.EntityFrameworkCore;

namespace Colabs.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<HelpRequest> HelpRequests => Set<HelpRequest>();
    public DbSet<HelpRequestItem> HelpRequestItems => Set<HelpRequestItem>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<DonationItem> DonationItems => Set<DonationItem>();
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>
    /// Colunas, tipos e tamanhos ficam nas Data Annotations dos Models. Aqui ficam o que as
    /// annotations não expressam: nomes de constraints (iguais aos do schema original),
    /// relacionamentos, valores padrão, conversão de enums e CHECK constraints.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureInstitution(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureHelpRequest(modelBuilder);
        ConfigureHelpRequestItem(modelBuilder);
        ConfigureDonation(modelBuilder);
        ConfigureDonationItem(modelBuilder);
        ConfigureReview(modelBuilder);
    }

    /// <summary>
    /// O provider Oracle executa o CREATE TABLE dentro de um bloco PL/SQL (EXECUTE IMMEDIATE '...'),
    /// sem escapar as aspas simples do texto dos CHECK constraints. Duplicá-las evita o erro PLS-00103.
    /// </summary>
    private static string Sql(string value) => value.Replace("'", "''");

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_USER_ID_PK");

            e.HasIndex(x => x.Cpf).IsUnique().HasDatabaseName("DB_USER_CPF_UN");
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("DB_USER_EMAIL_UN");

            e.Property(x => x.Role)
                .HasConversion(new UpperSnakeCaseEnumConverter<UserRole>())
                .HasDefaultValue(UserRole.User);

            e.ToTable(t => t.HasCheckConstraint("DB_USER_ROLE_CK", Sql("\"ROLE\" IN ('USER', 'ADMIN')")));
        });
    }

    private static void ConfigureInstitution(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Institution>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_INSTITUTION_ID_PK");

            e.HasIndex(x => x.Cnpj).IsUnique().HasDatabaseName("DB_INSTITUTION_CNPJ_UN");
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_CATEGORY_ID_PK");

            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("DB_CATEGORY_NAME_UN");
        });
    }

    private static void ConfigureHelpRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HelpRequest>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_HELP_REQUEST_ID_PK");

            e.Property(x => x.Status)
                .HasConversion(new UpperSnakeCaseEnumConverter<HelpRequestStatus>())
                .HasDefaultValue(HelpRequestStatus.Open);

            e.HasOne(x => x.Category)
                .WithMany(c => c.HelpRequests)
                .HasForeignKey(x => x.IdCategory)
                .HasConstraintName("DB_HELP_REQUEST_ID_CATEGORY_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User)
                .WithMany(u => u.HelpRequests)
                .HasForeignKey(x => x.IdUser)
                .HasConstraintName("DB_HELP_REQUEST_ID_USER_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Institution)
                .WithMany(i => i.HelpRequests)
                .HasForeignKey(x => x.IdInstitution)
                .HasConstraintName("DB_HELP_REQUEST_ID_INSTITUTION_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("DB_HELP_REQUEST_STATUS_CK",
                Sql("\"STATUS\" IN ('OPEN', 'IN_PROGRESS', 'COMPLETED', 'CANCELED')")));    
        });
    }

    private static void ConfigureHelpRequestItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HelpRequestItem>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_HELP_REQUEST_ITEM_ID_PK");

            e.HasOne(x => x.HelpRequest)
                .WithMany(h => h.Items)
                .HasForeignKey(x => x.IdHelpRequest)
                .HasConstraintName("DB_HELP_REQUEST_ITEM_ID_HELP_REQUEST_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("DB_HELP_REQUEST_ITEM_QUANTITY_CK", "\"QUANTITY\" > 0"));
        });
    }

    private static void ConfigureDonation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Donation>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_DONATION_ID_PK");

            e.Property(x => x.Status)
                .HasConversion(new UpperSnakeCaseEnumConverter<DonationStatus>())
                .HasDefaultValue(DonationStatus.Available);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Donations)
                .HasForeignKey(x => x.IdCategory)
                .HasConstraintName("DB_DONATION_ID_CATEGORY_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User)
                .WithMany(u => u.Donations)
                .HasForeignKey(x => x.IdUser)
                .HasConstraintName("DB_DONATION_ID_USER_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Institution)
                .WithMany(i => i.Donations)
                .HasForeignKey(x => x.IdInstitution)
                .HasConstraintName("DB_DONATION_ID_INSTITUTION_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.HelpRequest)
                .WithMany(h => h.Donations)
                .HasForeignKey(x => x.IdHelpRequest)
                .HasConstraintName("DB_DONATION_ID_HELP_REQUEST_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("DB_DONATION_STATUS_CK",
                    Sql("\"STATUS\" IN ('AVAILABLE', 'MATCHED', 'DELIVERED', 'CANCELED')"));
                t.HasCheckConstraint("DB_DONATION_DONOR_CK",
                    "\"ID_USER\" IS NOT NULL OR \"ID_INSTITUTION\" IS NOT NULL");
            });
        });
    }

    private static void ConfigureDonationItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DonationItem>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_DONATION_ITEM_ID_PK");

            e.HasOne(x => x.Donation)
                .WithMany(d => d.Items)
                .HasForeignKey(x => x.IdDonation)
                .HasConstraintName("DB_DONATION_ITEM_ID_DONATION_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("DB_DONATION_ITEM_QUANTITY_CK", "\"QUANTITY\" > 0"));
        });
    }

    private static void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(x => x.Id).HasName("DB_REVIEW_ID_PK");

            e.HasOne(x => x.Donation)
                .WithMany(d => d.Reviews)
                .HasForeignKey(x => x.IdDonation)
                .HasConstraintName("DB_REVIEW_ID_DONATION_FK")
                .OnDelete(DeleteBehavior.Restrict);

            e.ToTable(t => t.HasCheckConstraint("DB_REVIEW_RATING_CK", "\"RATING\" BETWEEN 1 AND 5"));
        });
    }
}