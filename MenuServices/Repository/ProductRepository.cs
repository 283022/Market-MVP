using MenuServices.Db;
using MenuServices.DTOs;
using MenuServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuServices.Repository;

public class ProductRepository(ApplicationDbContext context) : IProductRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IEnumerable<Product>> GetMenuAsync(MenuQueryParams queryParams)
    {
        var query = _context.Products
            .Where(p => p.IsActive && !p.IsStopped)
            .AsQueryable();

        // Фильтрация
        if (!string.IsNullOrWhiteSpace(queryParams.Filter))
        {
            query = query.Where(p => p.Name.Contains(queryParams.Filter) || 
                                     p.Description.Contains(queryParams.Filter));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            query = query.Where(p => p.Category == queryParams.Category);
        }

        // Пагинация
        return await query
            .OrderBy(p => p.Name)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var product = new Product { Id = id };
        _context.Products.Remove(product);
        return Task.CompletedTask;
    }

    public async Task<int> GetTotalCountAsync(MenuQueryParams queryParams)
    {
        var query = _context.Products
            .Where(p => p.IsActive && !p.IsStopped)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Filter))
        {
            query = query.Where(p => p.Name.Contains(queryParams.Filter) || 
                                     p.Description.Contains(queryParams.Filter));
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            query = query.Where(p => p.Category == queryParams.Category);
        }

        return await query.CountAsync();
    }
}

