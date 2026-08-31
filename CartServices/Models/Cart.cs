namespace CartServices.Models;

public class Cart
{
    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }      // null = анонимная корзина
    public string? SessionId { get; private set; } // для анонимных пользователей
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Navigation
    public List<CartItem> Items { get; private set; } = new();

    // Приватный конструктор (для EF Core и фабрики)
    private Cart(Guid userId, string? sessionId)
    {
        Id = Guid.NewGuid();
        UserId = userId == Guid.Empty ? null : userId;
        SessionId = sessionId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
    
    private Cart() { }

    //  Фабричный метод
    public static Cart Create(Guid? userId, string? sessionId)
    {
        if (!userId.HasValue && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Either UserId or SessionId must be provided");
        
        if (userId.HasValue && !string.IsNullOrEmpty(sessionId))
        {
            return new Cart(userId.Value, null);
        }
        //  Анонимный пользователь
        if (!userId.HasValue && !string.IsNullOrEmpty(sessionId))
        {
            return new Cart(Guid.Empty, sessionId);
        }

        //  Авторизованный пользователь без сессии
        return new Cart(userId.Value, null);
    }

    //  Методы для изменения состояния
    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        UserId = userId;
        SessionId = null; // Очищаем анонимную сессию
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(CartItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        // Проверяем, есть ли уже такой товар
        var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existingItem != null)
        {
            existingItem.AddQuantity(item.Quantity) ;
        }
        else
        {
            Items.Add(item);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ClearItems()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}