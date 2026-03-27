using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

public partial class MarketplaceLogic
{
    private readonly SqlHelper _sqlHelper;
    private User _currentUser;
    private Dictionary<int, CartItem> _userCart;
    private string _connectionString; // Храним строку подключения

    // Строка подключения передается в конструктор
    public MarketplaceLogic(string connectionString)
    {
        _sqlHelper = new SqlHelper(connectionString); // SqlHelper теперь использует строку подключения
        _userCart = new Dictionary<int, CartItem>();
        _connectionString = connectionString; // Сохраняем строку подключения
    }

    public User CurrentUser => _currentUser;

    // --- Хеширование пароля ---
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    // --- Регистрация пользователя ---
    public ActionResult Register(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            return new ActionResult { Success = false, Message = "Все поля обязательны для заполнения." };
        }
        if (password != confirmPassword)
        {
            return new ActionResult { Success = false, Message = "Пароли не совпадают." };
        }
        if (password.Length < 6)
        {
            return new ActionResult { Success = false, Message = "Пароль должен быть не менее 6 символов." };
        }

        string hashedPassword = HashPassword(password);

        try
        {
            // Для операций, не требующих транзакции, используем перегрузки SqlHelper без транзакции
            _sqlHelper.ExecuteNonQuery(
                "INSERT INTO Users (Username, Password) VALUES (@Username, @Password)",
                SqlHelper.CreateParameter("@Username", username, SqlDbType.NVarChar),
                SqlHelper.CreateParameter("@Password", hashedPassword, SqlDbType.NVarChar)
            );
            return new ActionResult { Success = true, Message = "Регистрация прошла успешно! Теперь вы можете войти." };
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            return new ActionResult { Success = false, Message = $"Пользователь с именем '{username}' уже существует." };
        }
        catch (Exception ex)
        {
            return new ActionResult { Success = false, Message = $"Ошибка регистрации: {ex.Message}" };
        }
    }

    // --- Вход пользователя ---
    public ActionResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new ActionResult { Success = false, Message = "Имя пользователя и пароль обязательны." };
        }

        string hashedPassword = HashPassword(password);

        // Для операций, не требующих транзакции
        var dt = _sqlHelper.ExecuteDataTable(
            "SELECT UserID, Username, Password FROM Users WHERE Username = @Username",
            SqlHelper.CreateParameter("@Username", username, SqlDbType.NVarChar)
        );

        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            string storedPasswordHash = (string)row["Password"];

            if (hashedPassword == storedPasswordHash)
            {
                _currentUser = new User
                {
                    UserID = (int)row["UserID"],
                    Username = (string)row["Username"],
                    Password = storedPasswordHash
                };
                _userCart.Clear();
                return new ActionResult { Success = true, Message = $"Добро пожаловать, {_currentUser.Username}!" };
            }
            else
            {
                return new ActionResult { Success = false, Message = "Неверный пароль." };
            }
        }
        else
        {
            return new ActionResult { Success = false, Message = "Пользователь с таким именем не найден." };
        }
    }

    public void Logout()
    {
        _currentUser = null;
        _userCart.Clear();
    }

    // --- Просмотр всех товаров (без транзакции) ---
    public List<Product> GetAllProducts()
    {
        var dt = _sqlHelper.ExecuteDataTable(
            "SELECT ProductID, Name, Price FROM Products ORDER BY Name"
        );
        return dt.AsEnumerable().Select(row => new Product
        {
            ProductID = row.Field<int>("ProductID"),
            Name = row.Field<string>("Name"),
            Price = row.Field<decimal>("Price")
        }).ToList();
    }

    // --- Добавление товара в корзину ---
    public void AddToCart(int productId, int quantity)
    {
        if (_currentUser == null)
        {
            Console.WriteLine("Пожалуйста, войдите или зарегистрируйтесь, чтобы добавить товары в корзину.");
            return;
        }
        if (quantity <= 0)
        {
            Console.WriteLine("Количество должно быть больше нуля.");
            return;
        }

        // Получаем товар (без транзакции)
        var dt = _sqlHelper.ExecuteDataTable(
            "SELECT ProductID, Name, Price FROM Products WHERE ProductID = @ProductID",
            SqlHelper.CreateParameter("@ProductID", productId, SqlDbType.Int)
        );

        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            var product = new Product
            {
                ProductID = (int)row["ProductID"],
                Name = (string)row["Name"],
                Price = (decimal)row["Price"]
            };

            if (_userCart.ContainsKey(productId))
            {
                _userCart[productId].Quantity += quantity;
            }
            else
            {
                _userCart.Add(productId, new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }
            Console.WriteLine($"'{product.Name}' добавлен в корзину в количестве {quantity} шт.");
        }
        else
        {
            Console.WriteLine("Товар не найден.");
        }
    }

    // --- Получение содержимого корзины ---
    public List<CartItem> GetCartContents()
    {
        if (_currentUser == null) return new List<CartItem>();

        foreach (var item in _userCart.Values.ToList())
        {
            var product = GetProductById(item.ProductID); // Получаем актуальные данные о товаре (без транзакции)
            if (product != null)
            {
                item.Price = product.Price; // Обновляем цену
            }
            else
            {
                _userCart.Remove(item.ProductID); // Удаляем, если товара нет
            }
        }
        return _userCart.Values.ToList();
    }

    // --- Получение товара по ID (без транзакции) ---
    public Product GetProductById(int productId)
    {
        var dt = _sqlHelper.ExecuteDataTable(
            "SELECT ProductID, Name, Price FROM Products WHERE ProductID = @ProductID",
            SqlHelper.CreateParameter("@ProductID", productId, SqlDbType.Int)
        );
        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            return new Product
            {
                ProductID = (int)row["ProductID"],
                Name = (string)row["Name"],
                Price = (decimal)row["Price"]
            };
        }
        return null;
    }

    // --- Удаление товара из корзины ---
    public void RemoveFromCart(int productId)
    {
        if (_userCart.ContainsKey(productId))
        {
            _userCart.Remove(productId);
            Console.WriteLine("Товар удален из корзины.");
        }
        else
        {
            Console.WriteLine("Товара нет в корзине.");
        }
    }

    // --- Получение общей суммы корзины ---
    public decimal GetCartTotal()
    {
        return _userCart.Values.Sum(item => item.Subtotal);
    }

    // --- Получение списка доступных ПВЗ (без транзакции) ---
    public List<PVZ> GetAllPVZ()
    {
        var dt = _sqlHelper.ExecuteDataTable(
            "SELECT PvzID, Address FROM PVZ"
        );
        return dt.AsEnumerable().Select(row => new PVZ
        {
            PvzID = row.Field<int>("PvzID"),
            Address = row.Field<string>("Address")
        }).ToList();
    }

    // --- Оформление заказа (с управлением соединением и транзакцией) ---
    // Метод теперь принимает выбранный ID ПВЗ
    public ActionResult Checkout(int pvzId)
    {
        if (_currentUser == null)
        {
            return new ActionResult { Success = false, Message = "Для оформления заказа необходимо войти в аккаунт." };
        }
        if (_userCart.Count == 0)
        {
            return new ActionResult { Success = false, Message = "Ваша корзина пуста." };
        }
        if (pvzId <= 0)
        {
            return new ActionResult { Success = false, Message = "Не выбран пункт выдачи заказов." };
        }

        SqlConnection connection = null;
        SqlTransaction transaction = null;

        try
        {
            connection = new SqlConnection(_connectionString); // Создаем соединение, используя строку из класса
            connection.Open();
            transaction = connection.BeginTransaction(); // Начинаем транзакцию

            // 1. Создаем основной заказ
            var orderId = _sqlHelper.ExecuteScalar(
                "INSERT INTO Orders (UserID, PvzID) VALUES (@UserID, @PvzID); SELECT SCOPE_IDENTITY();",
                connection,
                transaction,
                SqlHelper.CreateParameter("@UserID", _currentUser.UserID, SqlDbType.Int),
                SqlHelper.CreateParameter("@PvzID", pvzId, SqlDbType.Int)
            );
            int newOrderId = Convert.ToInt32(orderId);

            // 2. Добавляем товары из корзины в OrderItems
            foreach (var item in _userCart.Values)
            {
                _sqlHelper.ExecuteNonQuery(
                    "INSERT INTO OrderItems (OrderID, ProductID, Quantity) VALUES (@OrderID, @ProductID, @Quantity)",
                    connection,
                    transaction,
                    SqlHelper.CreateParameter("@OrderID", newOrderId, SqlDbType.Int),
                    SqlHelper.CreateParameter("@ProductID", item.ProductID, SqlDbType.Int),
                    SqlHelper.CreateParameter("@Quantity", item.Quantity, SqlDbType.Int)
                );
            }

            transaction.Commit(); // Фиксируем все изменения
            _userCart.Clear(); // Очищаем корзину

            return new ActionResult { Success = true, Message = $"Заказ №{newOrderId} успешно оформлен!" };
        }
        catch (Exception ex)
        {
            // Откатываем изменения, если транзакция была начата
            if (transaction != null)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    Console.WriteLine($"Критическая ошибка: произошла ошибка при откате транзакции: {rollbackEx.Message}");
                }
            }
            return new ActionResult { Success = false, Message = $"Ошибка оформления заказа: {ex.Message}" };
        }
        finally
        {
            // Освобождаем ресурсы
            if (transaction != null)
            {
                transaction.Dispose();
            }
            if (connection != null)
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                connection.Dispose();
            }
        }
    }

    // --- Получение заказов пользователя (без транзакции) ---
    public List<Order> GetUserOrders(int userId)
    {
        var dt = _sqlHelper.ExecuteDataTable(
            @"SELECT o.OrderID, o.UserID, o.PvzID, o.OrderDate, u.Username, p.Address AS PvzAddress 
              FROM Orders o 
              JOIN Users u ON o.UserID = u.UserID 
              JOIN PVZ p ON o.PvzID = p.PvzID 
              WHERE o.UserID = @UserID 
              ORDER BY o.OrderDate DESC",
            SqlHelper.CreateParameter("@UserID", userId, SqlDbType.Int)
        );

        return dt.AsEnumerable().Select(row => new Order
        {
            OrderID = row.Field<int>("OrderID"),
            UserID = row.Field<int>("UserID"),
            PvzID = row.Field<int>("PvzID"),
            OrderDate = row.Field<DateTime>("OrderDate"),
            Username = row.Field<string>("Username"),
            PvzAddress = row.Field<string>("PvzAddress")
        }).ToList();
    }

    // --- Получение деталей заказа (без транзакции) ---
    public List<OrderItemDetail> GetOrderDetails(int orderId)
    {
        var dt = _sqlHelper.ExecuteDataTable(
            @"SELECT oi.OrderItemID, oi.OrderID, oi.ProductID, p.Name AS ProductName, oi.Quantity, p.Price AS ProductPrice
              FROM OrderItems oi
              JOIN Products p ON oi.ProductID = p.ProductID
              WHERE oi.OrderID = @OrderID",
            SqlHelper.CreateParameter("@OrderID", orderId, SqlDbType.Int)
        );

        return dt.AsEnumerable().Select(row => new OrderItemDetail
        {
            OrderItemID = row.Field<int>("OrderItemID"),
            OrderID = row.Field<int>("OrderID"),
            ProductID = row.Field<int>("ProductID"),
            ProductName = row.Field<string>("ProductName"),
            Quantity = row.Field<int>("Quantity"),
            ProductPrice = row.Field<decimal>("ProductPrice")
        }).ToList();
    }
}