using Microsoft.EntityFrameworkCore;

namespace GlobalGo.Infrastructure.Persistence;

public sealed class CreditEvaluationDbContext : DbContext
{
    public CreditEvaluationDbContext(DbContextOptions<CreditEvaluationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CreditEvaluationData> CreditEvaluations => Set<CreditEvaluationData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<CreditEvaluationData>();

        entity.ToTable("credit_evaluations");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.ApplicantDni).HasMaxLength(8).IsRequired();
        entity.Property(x => x.ApplicantFullName).HasMaxLength(120).IsRequired();
        entity.Property(x => x.Justification).HasMaxLength(500).IsRequired();
        entity.Property(x => x.Decision).HasConversion<string>().HasMaxLength(30).IsRequired();

        entity.HasIndex(x => x.ApplicantDni);
        entity.HasIndex(x => x.CreatedAtUtc);
    }
}