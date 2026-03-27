// Модели данных 

using System;

public class User
{
    public int UserID { get; set; }
    public string Username { get; set; }
    public string Password { get; set; } 
}

public class PVZ
{
    public int PvzID { get; set; }
    public string Address { get; set; }
}

public class Product
{
    public int ProductID { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int OrderID { get; set; }
    public int UserID { get; set; }
    public int PvzID { get; set; }
    public DateTime OrderDate { get; set; }
    public string Username { get; set; } // Для удобства при выводе
    public string PvzAddress { get; set; } // Для удобства при выводе
}

// Модель для детального просмотра товаров в заказе
public class OrderItemDetail
{
    public int OrderItemID { get; set; }
    public int OrderID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal ProductPrice { get; set; } // Цена товара на момент заказа
}

// Модель для элементов корзины (хранится в памяти)
public class CartItem
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; } // Актуальная цена
    public int Quantity { get; set; }
    public decimal Subtotal => Price * Quantity; // Сумма по позиции
}

// Модель для возврата результатов операций
public class ActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}