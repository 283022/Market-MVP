namespace MenuServices.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
    
    // Дополнительные поля
    public string Category { get; set; }         
    public bool IsActive { get; set; } = true;     
    public bool IsStopped { get; set; } = false;   
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}