using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Globalization; // Для форматирования валюты


public class Program
{
    private MarketplaceLogic _marketplace;
    private string _connectionString;

    // --- ТОЧКА ВХОДА В ПРОГРАММУ ---
    public static void Main(string[] args)
    {
        Program app = new Program();
        app.Run();
    }
    // --- КОНЕЦ ТОЧКИ ВХОДА ---

    // Конструктор программы
    public Program()
    {
        _connectionString = @"Server=DESKTOP-9U8GDUK\MSSQLSERVER02;Database=GMWOG_DB;Integrated Security=True;"; 

        try
        {
            // Инициализация MarketplaceLogic с использованием строки подключения
            _marketplace = new MarketplaceLogic(_connectionString);
            Console.WriteLine("Маркетплейс GMWOG запущен!");
        }
        catch (Exception ex)
        {
            // Обработка ошибки подключения к базе данных
            Console.WriteLine($"\n!!! ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ ДАННЫХ !!!");
            Console.WriteLine($"Пожалуйста, убедитесь, что SQL Server запущен и строка подключения верна.");
            Console.WriteLine($"SQL строка: {_connectionString}");
            Console.WriteLine($"Сообщение об ошибке: {ex.Message}");
            Console.WriteLine($"\nДля создания базы данных выполните SQL-скрипт.");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
            Environment.Exit(1); // Завершаем приложение при ошибке подключения
        }
    }

    // Основной цикл работы программы 
    public void Run()
    {
        Console.WriteLine("\n========================================");
        Console.WriteLine("=== Добро пожаловать в GMWOG Market! ===");
        Console.WriteLine("========================================");

        while (true) // Бесконечный цикл, пока пользователь не введет "exit"
        {
            DisplayMainMenu(); // Отображаем главное меню
            string mainMenuChoice = Console.ReadLine(); // Читаем ввод пользователя

            // Обрабатываем выбор пользователя
            switch (mainMenuChoice)
            {
                case "1": // Просмотр товаров
                    ViewProducts();
                    break;
                case "2": // Регистрация
                    RegisterUser();
                    break;
                case "3": // Вход
                    LoginUser();
                    break;
                case "4": // Просмотр заказов (для зарегистрированных)
                    ViewOrders();
                    break;
                case "5": // Корзина
                    ManageCart();
                    break;
                case "6": // Выход из аккаунта
                    _marketplace.Logout();
                    Console.WriteLine("Вы вышли из аккаунта.");
                    break;
                case "exit": // Выход из программы
                    Console.WriteLine("Спасибо за использование GMWOG Market! До свидания!");
                    return; 
                default: // Неверный ввод
                    Console.WriteLine("Неверный ввод. Пожалуйста, выберите действие из меню.");
                    break;
            }
            Console.WriteLine("\nНажмите Enter для продолжения..."); // Пауза перед следующим действием
            Console.ReadLine();
        }
    }

    // Отображение главного меню 
    private void DisplayMainMenu()
    {
        Console.WriteLine("\n--- Главное меню ---");
        if (_marketplace.CurrentUser == null) // Если пользователь не авторизован
        {
            Console.WriteLine("1. Просмотреть товары");
            Console.WriteLine("2. Регистрация");
            Console.WriteLine("3. Вход");
        }
        else // Если пользователь авторизован
        {
            Console.WriteLine($"Вы вошли как: {_marketplace.CurrentUser.Username}");
            Console.WriteLine("1. Просмотреть товары");
            Console.WriteLine("4. Просмотреть мои заказы");
            Console.WriteLine("5. Корзина");
            Console.WriteLine("6. Выход из аккаунта");
        }
        Console.WriteLine("Введите 'exit' для завершения работы.");
        Console.Write("Ваш выбор: ");
    }

    // Метод для просмотра всех товаров 
    private void ViewProducts()
    {
        Console.WriteLine("\n--- Каталог товаров ---");
        var products = _marketplace.GetAllProducts();
        if (!products.Any()) // Если товаров нет
        {
            Console.WriteLine("Товаров пока нет.");
            return;
        }

        var ruCulture = CultureInfo.GetCultureInfo("ru-RU"); // Создаем объект для русского форматирования валюты

        Console.WriteLine($"{"ID",-5} | {"Название",-30} | {"Цена",-10}");
        Console.WriteLine("---");
        foreach (var product in products)
        {
            // Форматируем цену с использованием русской культуры
            Console.WriteLine($"{product.ProductID,-5} | {product.Name,-30} | {product.Price.ToString("C", ruCulture),-10}");
        }
        Console.WriteLine("---");
    }

    // Метод для регистрации нового пользователя 
    private void RegisterUser()
    {
        Console.WriteLine("\n--- Регистрация нового пользователя ---");
        Console.Write("Имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Пароль (минимум 6 символов): ");
        string password = ReadPassword(); // Используем метод для скрытого ввода пароля
        Console.Write("Подтвердите пароль: ");
        string confirmPassword = ReadPassword();

        var result = _marketplace.Register(username, password, confirmPassword); // Вызываем метод регистрации
        Console.WriteLine(result.Message); // Выводим результат
    }

    // Метод для входа пользователя 
    private void LoginUser()
    {
        Console.WriteLine("\n--- Вход в аккаунт ---");
        Console.Write("Имя пользователя: ");
        string username = Console.ReadLine();
        Console.Write("Пароль: ");
        string password = ReadPassword();

        var result = _marketplace.Login(username, password); // Вызываем метод входа
        Console.WriteLine(result.Message); // Выводим результат
    }

    // Метод для просмотра заказов пользователя 
    private void ViewOrders()
    {
        if (_marketplace.CurrentUser == null) // Проверяем, авторизован ли пользователь
        {
            Console.WriteLine("Пожалуйста, войдите в аккаунт, чтобы просмотреть заказы.");
            return;
        }

        Console.WriteLine($"\n--- Ваши заказы ({_marketplace.CurrentUser.Username}) ---");
        var orders = _marketplace.GetUserOrders(_marketplace.CurrentUser.UserID); // Получаем список заказов

        if (!orders.Any()) // Если заказов нет
        {
            Console.WriteLine("У вас пока нет заказов.");
            return;
        }

        var ruCulture = CultureInfo.GetCultureInfo("ru-RU"); // Для форматирования валюты

        foreach (var order in orders) // Итерируем по каждому заказу
        {
            Console.WriteLine($"\nЗаказ №{order.OrderID} от {order.OrderDate:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  ПВЗ: {order.PvzAddress}");
            Console.WriteLine("  Товары:");

            var orderItems = _marketplace.GetOrderDetails(order.OrderID); // Получаем детали заказа
            decimal totalOrderSum = 0;
            foreach (var item in orderItems) // Итерируем по товарам в заказе
            {
                decimal itemSubtotal = item.Quantity * item.ProductPrice; // Расчет суммы по позиции
                Console.WriteLine($"    - {item.ProductName} (ID: {item.ProductID}) x {item.Quantity} шт. = {itemSubtotal.ToString("C", ruCulture)}");
                totalOrderSum += itemSubtotal; // Добавляем к общей сумме заказа
            }
            Console.WriteLine($"  Общая сумма заказа: {totalOrderSum.ToString("C", ruCulture)}");
        }
    }

    // Метод для управления корзиной 
    private void ManageCart()
    {
        if (_marketplace.CurrentUser == null) // Проверяем авторизацию
        {
            Console.WriteLine("Пожалуйста, войдите в аккаунт, чтобы управлять корзиной.");
            return;
        }

        while (true) // Цикл для меню корзины
        {
            Console.WriteLine("\n--- Ваша корзина ---");
            var cartContents = _marketplace.GetCartContents(); // Получаем содержимое корзины

            string cartMenuChoice; // Одна переменная для выбора меню корзины

            if (!cartContents.Any()) // Если корзина пуста
            {
                Console.WriteLine("Ваша корзина пуста.");
                Console.WriteLine("1. Добавить товар");
                Console.WriteLine("2. Назад");
                Console.Write("Ваш выбор: ");
                cartMenuChoice = Console.ReadLine();

                if (cartMenuChoice == "2") break; // Выход из меню корзины
                if (cartMenuChoice == "1")
                {
                    ViewProductsAndAdd(); // Переходим к добавлению товара
                    continue; // Перезапускаем цикл для обновления корзины
                }
                else
                {
                    Console.WriteLine("Неверный ввод.");
                    continue; // Перезапускаем цикл
                }
            }
            else // Если корзина не пуста
            {
                var ruCulture = CultureInfo.GetCultureInfo("ru-RU"); // Форматирование валюты

                Console.WriteLine($"{"ID товара",-8} | {"Название",-30} | {"Цена",-10} | {"Кол-во",-7} | {"Сумма",-10}");
                Console.WriteLine("---");
                foreach (var item in cartContents) // Выводим список товаров в корзине
                {
                    Console.WriteLine($"{item.ProductID,-8} | {item.ProductName,-30} | {item.Price.ToString("C", ruCulture),-10} | {item.Quantity,-7} | {item.Subtotal.ToString("C", ruCulture),-10}");
                }
                Console.WriteLine("---");
                Console.WriteLine($"Итого: {_marketplace.GetCartTotal().ToString("C", ruCulture)}"); // Выводим итоговую сумму

                // Отображаем меню действий для корзины
                Console.WriteLine("\nДействия:");
                Console.WriteLine("1. Добавить товар");
                Console.WriteLine("2. Удалить товар");
                Console.WriteLine("3. Оформить заказ");
                Console.WriteLine("4. Назад");
                Console.Write("Ваш выбор: ");

                cartMenuChoice = Console.ReadLine(); // Читаем выбор пользователя
                switch (cartMenuChoice)
                {
                    case "1": // Добавить товар
                        ViewProductsAndAdd();
                        break;
                    case "2": // Удалить товар
                        RemoveItemFromCart();
                        break;
                    case "3": // Оформить заказ
                        Checkout(); // Переходим к оформлению заказа
                        return; // Выходим из меню корзины после успешного оформления
                    case "4": // Назад
                        return; // Выходим из меню корзины
                    default:
                        Console.WriteLine("Неверный ввод.");
                        break;
                }
            }
        }
    }

    // Вспомогательный метод: Посмотреть товары и добавить в корзину 
    private void ViewProductsAndAdd()
    {
        ViewProducts(); // Показываем каталог
        Console.Write("Введите ID товара для добавления в корзину (или 'назад'): ");
        string input = Console.ReadLine();
        if (input.ToLower() == "назад") return; // Выход, если введено "назад"

        if (int.TryParse(input, out int productId)) // Пытаемся преобразовать ввод в ID товара
        {
            Console.Write("Введите количество: ");
            string quantityInput = Console.ReadLine();
            if (int.TryParse(quantityInput, out int quantity) && quantity > 0) // Проверяем количество
            {
                _marketplace.AddToCart(productId, quantity); // Добавляем товар в корзину
            }
            else
            {
                Console.WriteLine("Неверное количество.");
            }
        }
        else
        {
            Console.WriteLine("Неверный ID товара.");
        }
    }

    // Вспомогательный метод: Удалить товар из корзины 
    private void RemoveItemFromCart()
    {
        Console.Write("Введите ID товара для удаления из корзины (или 'назад'): ");
        string input = Console.ReadLine();
        if (input.ToLower() == "назад") return; // Выход, если введено "назад"

        if (int.TryParse(input, out int productId)) // Пытаемся преобразовать ввод в ID товара
        {
            _marketplace.RemoveFromCart(productId); // Удаляем товар из корзины
        }
        else
        {
            Console.WriteLine("Неверный ID товара.");
        }
    }

    // Метод оформления заказа
    private void Checkout()
    {
        Console.WriteLine("\n--- Оформление заказа ---");
        var pvzList = _marketplace.GetAllPVZ(); // Получаем список доступных ПВЗ
        if (!pvzList.Any())
        {
            Console.WriteLine("Нет доступных пунктов выдачи заказов. Оформление заказа невозможно.");
            return;
        }

        Console.WriteLine("Доступные пункты выдачи заказов:");
        // Выводим список ПВЗ
        foreach (var pvz in pvzList)
        {
            Console.WriteLine($"{pvz.PvzID}. {pvz.Address}");
        }

        Console.Write("Выберите ID ПВЗ для доставки: ");
        string pvzInput = Console.ReadLine();

        // Пытаемся получить ID выбранного ПВЗ
        if (int.TryParse(pvzInput, out int selectedPvzId) && selectedPvzId > 0)
        {
            // Вызываем метод Checkout из MarketplaceLogic, передавая выбранный ПВЗ ID
            var result = _marketplace.Checkout(selectedPvzId); // Передаем выбранный ID ПВЗ
            Console.WriteLine(result.Message);
            // Если заказ успешно оформлен, корзина будет очищена в MarketplaceLogic
        }
        else
        {
            Console.WriteLine("Неверный ID ПВЗ.");
        }
    }

    // Вспомогательный метод для скрытого ввода пароля 
    private string ReadPassword()
    {
        string password = "";
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(intercept: true); // Читаем клавишу, не выводя ее на экран
            if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
            {
                password += key.KeyChar; // Добавляем символ к паролю
                Console.Write("*"); // Отображаем звездочку вместо символа
            }
            else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password.Substring(0, password.Length - 1); // Удаляем последний символ из пароля
                Console.Write("\b \b"); // Стираем звездочку с экрана
            }
        } while (key.Key != ConsoleKey.Enter); // Продолжаем, пока не будет нажата Enter
        Console.WriteLine(); // Переход на новую строку после ввода пароля
        return password;
    }
}