using MenuServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuServices.Db;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Настройка сущности Product
        modelBuilder.Entity<Product>(entity =>
        {
            // Первичный ключ
            entity.HasKey(p => p.Id);

            // Свойства
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.Price)
                .IsRequired()
                .HasPrecision(10, 2); // decimal(10,2) для цен

            entity.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            entity.Property(p => p.Category)
                .HasMaxLength(100);

            // Индексы для быстрого поиска
            entity.HasIndex(p => p.Category)
                .HasDatabaseName("IX_Products_Category");

            entity.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Products_IsActive");

            entity.HasIndex(p => p.IsStopped)
                .HasDatabaseName("IX_Products_IsStopped");

            // Составной индекс для частых запросов
            entity.HasIndex(p => new { p.IsActive, p.IsStopped })
                .HasDatabaseName("IX_Products_IsActive_IsStopped");

            // Индекс для поиска по имени (для LIKE запросов)
            entity.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Products_Name");
        });
    }
}