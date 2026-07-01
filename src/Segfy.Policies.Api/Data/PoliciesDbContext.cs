using Microsoft.EntityFrameworkCore;
using Segfy.Policies.Api.Models;

namespace Segfy.Policies.Api.Data;

public sealed class PoliciesDbContext(DbContextOptions<PoliciesDbContext> options) : DbContext(options)
{
    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.ToTable("Policies");

            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.PolicyNumber).IsUnique();

            entity.Property(policy => policy.PolicyNumber)
                .HasMaxLength(13)
                .IsRequired();

            entity.Property(policy => policy.InsuredDocument)
                .HasMaxLength(14)
                .IsRequired();

            entity.Property(policy => policy.VehiclePlate)
                .HasMaxLength(7)
                .IsRequired();

            entity.Property(policy => policy.MonthlyPremium)
                .HasPrecision(12, 2)
                .IsRequired();

            entity.Property(policy => policy.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
