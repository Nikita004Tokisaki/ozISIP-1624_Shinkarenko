using System;
using System.Collections.Generic;
using System.Globalization;

namespace StoreInventory
{
    public enum Category
    {
        Electronics,  // Электроника
        Grocery,      // Продукты
        Clothing      // Одежда
    }

    public class Product
    {
        public string Code { get; private set; }      // Уникальный код
        public string Name { get; private set; }      // Название
        public decimal Price { get; private set; }    // Цена
        public int Quantity { get; private set; }     // Количество на складе
        public Category Category { get; private set; }


        public bool IsInStock => Quantity > 0;

        public Product(string code, string name, decimal price, int quantity, Category category)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be empty");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty");
            if (price < 0)
                throw new ArgumentException("Price cannot be negative");
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative");

            Code = code;
            Name = name;
            Price = price;
            Quantity = quantity;
            Category = category;
        }


        public void AddQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Supply amount must be positive");
            Quantity += amount;
        }


        public void RemoveQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Sell amount must be positive");
            if (amount > Quantity)
                throw new InvalidOperationException("Not enough stock to sell");
            Quantity -= amount;
        }

        public override string ToString()
        {
            return $"[{Code}] {Name} | Цена: {Price:C2} | Кол-во: {Quantity} | В наличии: {IsInStock} | Категория: {Category}";
        }
    }

    public class InventoryManager
    {
        private readonly List<Product> products = new List<Product>();
        private int nextCode = 1;  // следующий числовой код

        public IReadOnlyList<Product> Products => products.AsReadOnly();

        public InventoryManager()
        {
            // Наполняем 5 тестовыми товарами
            AddProduct("Ноутбук", 75000m, 10, Category.Electronics);
            AddProduct("Телефон", 45000m, 5, Category.Electronics);
            AddProduct("Молоко", 65.5m, 50, Category.Grocery);
            AddProduct("Хлеб", 30m, 100, Category.Grocery);
            AddProduct("Футболка", 1200m, 20, Category.Clothing);
        }

        public Product AddProduct(string name, decimal price, int quantity, Category category)
        {
            string code = nextCode.ToString();
            nextCode++;
            var prod = new Product(code, name, price, quantity, category);
            products.Add(prod);
            return prod;
        }


        public bool DeleteProduct(string code)
        {
            var prod = FindByCode(code);
            if (prod == null) return false;
            return products.Remove(prod);
        }


        public bool OrderSupply(string code, int amount)
        {
            var prod = FindByCode(code);
            if (prod == null) return false;
            prod.AddQuantity(amount);
            return true;
        }


        public bool SellProduct(string code, int amount)
        {
            var prod = FindByCode(code);
            if (prod == null) return false;
            prod.RemoveQuantity(amount);
            return true;
        }


        public Product FindByCode(string code)
        {
            foreach (var p in products)
                if (p.Code == code)
                    return p;
            return null;
        }


        public List<Product> FindByName(string namePart)
        {
            List<Product> result = new List<Product>();
            foreach (var p in products)
            {
                if (p.Name.IndexOf(namePart, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    result.Add(p);
            }
            return result;
        }


        public List<Product> FindByCategory(Category category)
        {
            List<Product> result = new List<Product>();
            foreach (var p in products)
            {
                if (p.Category == category)
                    result.Add(p);
            }
            return result;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var inventory = new InventoryManager();
            while (true)
            {
                Console.WriteLine("\n--- Меню магазина ---");
                Console.WriteLine("1. Добавить товар");
                Console.WriteLine("2. Удалить товар");
                Console.WriteLine("3. Заказать поставку");
                Console.WriteLine("4. Продать товар");
                Console.WriteLine("5. Поиск товара");
                Console.WriteLine("6. Показать все товары");
                Console.WriteLine("0. Выход");
                Console.Write("Выберите опцию: ");
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            AddNewProduct(inventory);
                            break;
                        case "2":
                            DeleteProduct(inventory);
                            break;
                        case "3":
                            OrderSupply(inventory);
                            break;
                        case "4":
                            SellProduct(inventory);
                            break;
                        case "5":
                            SearchProduct(inventory);
                            break;
                        case "6":
                            ShowAll(inventory);
                            break;
                        case "0":
                            return;
                        default:
                            Console.WriteLine("Неверный выбор, повторите.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // Ловим все ошибки валидации и не даём программе упасть
                    Console.WriteLine("Ошибка: " + ex.Message);
                }
            }
        }

        static void AddNewProduct(InventoryManager inv)
        {
            Console.Write("Название: ");
            string name = Console.ReadLine();
            Console.Write("Цена: ");
            decimal price = ReadDecimalNonNegative();
            Console.Write("Количество: ");
            int qty = ReadIntNonNegative();
            Console.WriteLine("Категории: 0=Electronics, 1=Grocery, 2=Clothing");
            Console.Write("Выберите категорию (число): ");
            int cat = ReadIntInRange(0, 2);

            var prod = inv.AddProduct(name, price, qty, (Category)cat);
            Console.WriteLine("Добавлен: " + prod);
        }

        static void DeleteProduct(InventoryManager inv)
        {
            Console.Write("Введите код товара для удаления: ");
            string code = Console.ReadLine();
            if (inv.DeleteProduct(code))
                Console.WriteLine("Товар удалён.");
            else
                Console.WriteLine("Товар с таким кодом не найден.");
        }

        static void OrderSupply(InventoryManager inv)
        {
            Console.Write("Код товара: ");
            string code = Console.ReadLine();
            Console.Write("Сколько привезти: ");
            int amount = ReadIntPositive();
            if (inv.OrderSupply(code, amount))
                Console.WriteLine("Поставка оформлена.");
            else
                Console.WriteLine("Товар не найден.");
        }

        static void SellProduct(InventoryManager inv)
        {
            Console.Write("Код товара: ");
            string code = Console.ReadLine();
            Console.Write("Сколько продать: ");
            int amount = ReadIntPositive();
            if (inv.SellProduct(code, amount))
                Console.WriteLine("Продажа выполнена.");
            else
                Console.WriteLine("Товар не найден либо недостаточный остаток.");
        }

        static void SearchProduct(InventoryManager inv)
        {
            Console.WriteLine("1-По коду, 2-По названию, 3-По категории");
            string m = Console.ReadLine();
            switch (m)
            {
                case "1":
                    Console.Write("Введите код: ");
                    var byCode = inv.FindByCode(Console.ReadLine());
                    Console.WriteLine(byCode != null ? byCode.ToString() : "Не найдено");
                    break;
                case "2":
                    Console.Write("Часть названия: ");
                    var list2 = inv.FindByName(Console.ReadLine());
                    if (list2.Count == 0) Console.WriteLine("Не найдено");
                    else list2.ForEach(p => Console.WriteLine(p));
                    break;
                case "3":
                    Console.WriteLine("0=Electronics,1=Grocery,2=Clothing");
                    int c = ReadIntInRange(0, 2);
                    var list3 = inv.FindByCategory((Category)c);
                    if (list3.Count == 0) Console.WriteLine("Не найдено");
                    else list3.ForEach(p => Console.WriteLine(p));
                    break;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }

        static void ShowAll(InventoryManager inv)
        {
            Console.WriteLine("=== ВСЕ ТОВАРЫ ===");
            foreach (var p in inv.Products)
                Console.WriteLine(p);
        }


        static decimal ReadDecimalNonNegative()
        {
            while (true)
            {
                string s = Console.ReadLine();
                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal val) && val >= 0)
                    return val;
                Console.Write("Неверный ввод, введите неотрицательное число: ");
            }
        }

        static int ReadIntPositive()
        {
            while (true)
            {
                string s = Console.ReadLine();
                if (int.TryParse(s, out int val) && val > 0)
                    return val;
                Console.Write("Неверный ввод, введите целое > 0: ");
            }
        }

        static int ReadIntNonNegative()
        {
            while (true)
            {
                string s = Console.ReadLine();
                if (int.TryParse(s, out int val) && val >= 0)
                    return val;
                Console.Write("Неверный ввод, введите целое >= 0: ");
            }
        }

        static int ReadIntInRange(int min, int max)
        {
            while (true)
            {
                string s = Console.ReadLine();
                if (int.TryParse(s, out int val) && val >= min && val <= max)
                    return val;
                Console.Write($"Неверный ввод, введите число от {min} до {max}: ");
            }
        }
    }
}