using MenuServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuServices.Db;

public class ApplicationDbContext: Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}