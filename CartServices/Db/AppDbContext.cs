using CartServices.Models;
using Microsoft.EntityFrameworkCore;

namespace CartServices.Db;


public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(c => c.Id);
            
            entity.HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Carts_UserId");
            
            entity.HasIndex(c => c.SessionId)
                .HasDatabaseName("IX_Carts_SessionId");
            
            entity.HasIndex(c => c.UpdatedAt)
                .HasDatabaseName("IX_Carts_UpdatedAt");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            
            entity.HasIndex(i => i.CartId)
                .HasDatabaseName("IX_CartItems_CartId");
            
            entity.HasIndex(i => i.ProductId)
                .HasDatabaseName("IX_CartItems_ProductId");
            
            // Связь с Cart
            entity.HasOne<Cart>()
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}