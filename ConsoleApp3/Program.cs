using System;
using System.Collections.Generic;
using System.Globalization;

namespace DailyExpensesTracker
{
    class Program
    {
        // Класс для хранения информации о расходе
        class Expense
        {
            public string Name { get; set; }
            public decimal Amount { get; set; }

            public Expense(string name, decimal amount)
            {
                Name = name;
                Amount = amount;
            }
        }

        static void Main(string[] args)
        {
            List<Expense> expenses = new List<Expense>();
            int maxOperations = 40;
            int minOperations = 2;

            Console.WriteLine("Введите количество операций (от 2 до 40):");
            int countOperations = 0;
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out countOperations))
                {
                    if (countOperations >= minOperations && countOperations <= maxOperations)
                        break;
                }
                Console.WriteLine("Некорректный ввод. Попробуйте снова:");
            }

            // Ввод расходов
            for (int i = 0; i < countOperations; i++)
            {
                Console.WriteLine($"Введите данные для операции {i + 1} в формате: Название услуги или товара; Количество денег");
                string input = Console.ReadLine();
                string[] parts = input.Split(';');

                if (parts.Length != 2)
                {
                    Console.WriteLine("Некорректный формат. Попробуйте снова.");
                    i--;
                    continue;
                }

                string name = parts[0].Trim();
                string amountStr = parts[1].Trim();

                if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    expenses.Add(new Expense(name, amount));
                }
                else
                {
                    Console.WriteLine("Некорректная сумма. Попробуйте снова.");
                    i--;
                }
            }

            // Основное меню
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nМеню:");
                Console.WriteLine("1. Вывод данных");
                Console.WriteLine("2. Статистика");
                Console.WriteLine("3. Сортировка по цене");
                Console.WriteLine("4. Конвертация валюты");
                Console.WriteLine("5. Поиск по названию");
                Console.WriteLine("0. Выход");
                Console.Write("Выберите пункт меню: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        OutputData(expenses);
                        break;
                    case "2":
                        ShowStatistics(expenses);
                        break;
                    case "3":
                        BubbleSortExpenses(expenses);
                        Console.WriteLine("Отсортировано по цене (возрастание).");
                        break;
                    case "4":
                        ConvertCurrency(expenses);
                        break;
                    case "5":
                        SearchByName(expenses);
                        break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Некорректный выбор. Попробуйте снова.");
                        break;
                }
            }
        }

        static void OutputData(List<Expense> expenses)
        {
            Console.WriteLine("\nВсе расходы:");
            foreach (var expense in expenses)
            {
                Console.WriteLine($"{expense.Name} - {expense.Amount} руб");
            }
        }

        static void ShowStatistics(List<Expense> expenses)
        {
            if (expenses.Count == 0)
            {
                Console.WriteLine("Нет данных для статистики.");
                return;
            }

            decimal sum = 0;
            decimal max = decimal.MinValue;
            decimal min = decimal.MaxValue;

            foreach (var expense in expenses)
            {
                sum += expense.Amount;
                if (expense.Amount > max) max = expense.Amount;
                if (expense.Amount < min) min = expense.Amount;
            }

            decimal average = sum / expenses.Count;

            Console.WriteLine($"\nСтатистика:");
            Console.WriteLine($"Общая сумма: {sum} руб");
            Console.WriteLine($"Среднее: {average:F2} руб");
            Console.WriteLine($"Максимальное: {max} руб");
            Console.WriteLine($"Минимальное: {min} руб");
        }

        static void BubbleSortExpenses(List<Expense> expenses)
        {
            int n = expenses.Count;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (expenses[j].Amount > expenses[j + 1].Amount)
                    {
                        var temp = expenses[j];
                        expenses[j] = expenses[j + 1];
                        expenses[j + 1] = temp;
                    }
                }
            }
        }

        static void ConvertCurrency(List<Expense> expenses)
        {
            Console.WriteLine("Выберите валюту для конвертации:");
            Console.WriteLine("1. USD");
            Console.WriteLine("2. EUR");
            Console.WriteLine("3. CNY");
            Console.WriteLine("4. Ввести свой курс");
            Console.Write("Ваш выбор: ");

            string choice = Console.ReadLine();
            decimal course = 0;
            switch (choice)
            {
                case "1":
                    course = 0.013m; // пример курса, 1 рубль = 0.013 USD
                    break;
                case "2":
                    course = 0.012m; // пример курса
                    break;
                case "3":
                    course = 0.095m; // пример курса
                    break;
                case "4":
                    Console.Write("Введите курс (1 единица валюты = сколько рублей): ");
                    if (!decimal.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out course))
                    {
                        Console.WriteLine("Некорректный курс.");
                        return;
                    }
                    break;
                default:
                    Console.WriteLine("Некорректный выбор.");
                    return;
            }

            Console.WriteLine($"\nРасходы в выбранной валюте:");
            foreach (var expense in expenses)
            {
                decimal converted = expense.Amount * course;
                Console.WriteLine($"{expense.Name} - {converted:F2} (по курсу {course})");
            }
        }

        static void SearchByName(List<Expense> expenses)
        {
            Console.Write("Введите название для поиска: ");
            string searchTerm = Console.ReadLine().ToLower();

            bool found = false;
            foreach (var expense in expenses)
            {
                if (expense.Name.ToLower().Contains(searchTerm))
                {
                    Console.WriteLine($"{expense.Name} - {expense.Amount} руб");
                    found = true;
                }
            }

            if (!found)
                Console.WriteLine("Ничего не найдено по вашему запросу.");
        }
    }
}
