using Microsoft.EntityFrameworkCore;
using OrderServices.Model;

namespace OrderServices.Db;

public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserId)
                .IsRequired();

            entity.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(o => o.OrderUpdate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(o => o.OrderStatus)
                .IsRequired()
                .HasConversion<int>(); // Храним enum как int в БД

            entity.Property(o => o.TimeToEndPending)
                .IsRequired(false);
            
            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.ProductId)
                .IsRequired();

            entity.Property(i => i.Quantity)
                .IsRequired();

            entity.Property(i => i.UnitPrice)
                .IsRequired()
                .HasPrecision(10, 2); // decimal(10,2) в БД

            entity.Property(i => i.ProductName)
                .IsRequired()
                .HasMaxLength(200);
        });
    }
}