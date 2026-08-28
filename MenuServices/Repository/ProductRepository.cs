using MenuServices.Db;

namespace MenuServices.Repository;

public class ProductRepository(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;
    
}