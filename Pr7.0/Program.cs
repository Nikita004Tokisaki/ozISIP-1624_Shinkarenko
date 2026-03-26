using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;


public class Program
{
    private GameLogic _gameLogic;
    private List<Client> _clientQueue;
    private Client _currentClient; // Текущий клиент, который ждет ремонта


    public static void Main(string[] args) 
    {
        Program game = new Program();
        game.StartGame();
    }


    public Program()
    {
        string connectionString = @"Server=DESKTOP-9U8GDUK\MSSQLSERVER02;Database=CarServiceDB;Integrated Security=True;";

        try
        {
            _gameLogic = new GameLogic(connectionString);
            _clientQueue = new List<Client>();
            Console.WriteLine("База данных подключена. Запускаем автосервис!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n!!! ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ ДАННЫХ !!!");
            Console.WriteLine($"Пожалуйста, убедитесь, что SQL Server запущен и строка подключения верна.");
            Console.WriteLine($"SQL строка: {connectionString}");
            Console.WriteLine($"Сообщение об ошибке: {ex.Message}");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
            Environment.Exit(1); // Завершаем программу, если база не подключилась
        }
    }

    public void StartGame()
    {
        Console.WriteLine("\n=========================================");
        Console.WriteLine("=== Добро пожаловать в АвтоСервис! ===");
        Console.WriteLine("=========================================\n");

        Thread inventoryThread = new Thread(_gameLogic.ProcessDelayedInventory);
        inventoryThread.IsBackground = true;
        inventoryThread.Start();

        while (true)
        {
            _gameLogic.ProcessDelayedInventory();

            if (_currentClient == null)
            {
                _currentClient = _gameLogic.GenerateNewClient();
                if (_currentClient == null)
                {
                    Console.WriteLine("Нет доступных запчастей для генерации клиентов. Игра остановлена.");
                    break;
                }
                Console.WriteLine($"\n--- Прибыл новый клиент! ---");
            }

            DisplayClientInfo(_currentClient);
            DisplayWarehouseStatus();
            Console.WriteLine($"\nВаш баланс: {_gameLogic.GetPlayerBalance():C}");

            DisplayActionMenu();

            string choice = Console.ReadLine();

            ActionResult result = null;

            switch (choice.Trim().ToLower())
            {
                case "1":
                    result = HandleRepair();
                    break;
                case "2":
                    result = _gameLogic.RefuseClient(_currentClient);
                    _currentClient = null;
                    break;
                case "3":
                    HandlePurchase();
                    break;
                case "4":
                    result = _gameLogic.RiskRepair(_currentClient);
                    _currentClient = null;
                    break;
                case "exit":
                    Console.WriteLine("Спасибо за игру! До свидания!");
                    return;
                default:
                    Console.WriteLine("Некорректный ввод. Пожалуйста, выберите действие из меню.");
                    break;
            }

            if (result != null)
            {
                Console.WriteLine(result.Message);
                if (!result.Success && (result.Message.Contains("Ошибка") || result.Message.Contains("Критическая")))
                {
                    if (result.Message.Contains("Критическая"))
                    {
                        Console.WriteLine("Критическая ошибка. Игра будет остановлена.");
                        break;
                    }
                }
            }

            if (_currentClient == null)
            {
                if (_gameLogic.GetPlayerBalance() < 0)
                {
                    Console.WriteLine("\n!!! У вас закончились деньги! Игра окончена. !!!");
                    break;
                }
                Console.WriteLine("\n--- Готовимся к следующему клиенту... ---");
                Thread.Sleep(1500);
            }
            else
            {
                Thread.Sleep(1000);
            }
        }

        Console.WriteLine("\nИгра завершена. Нажмите любую клавишу для выхода.");
        Console.ReadKey();
    }

    private void DisplayClientInfo(Client client)
    {
        Console.WriteLine($"\n--- Клиент №{client.ClientID} ---");
        Console.WriteLine($"Машина: {client.MachineModel ?? "неизвестно"}");
        Console.WriteLine($"Сломанная деталь: {client.BrokenPartName}");
        Console.WriteLine($"Стоимость ремонта: {client.RepairCost:C}");
        if (client.FineForRefusal.HasValue) Console.WriteLine($"Штраф за отказ: {client.FineForRefusal.Value:C}");
        if (client.DamageCost.HasValue) Console.WriteLine($"Возможный ущерб: {client.DamageCost.Value:C}");
    }

    private void DisplayWarehouseStatus()
    {
        Console.WriteLine("\n--- Ваш склад ---");
        var parts = _gameLogic.GetAllParts().Where(p => p.CurrentStock > 0).ToList();
        if (!parts.Any())
        {
            Console.WriteLine("Склад пуст!");
        }
        else
        {
            foreach (var part in parts)
            {
                Console.WriteLine($"- {part.PartName}: {part.CurrentStock} шт. (Цена покупки: {part.PurchasePrice:C})");
            }
        }
        Console.WriteLine("-----------------");
    }

    private void DisplayActionMenu()
    {
        Console.WriteLine("\n--- Ваши действия ---");
        Console.WriteLine("1. Починить машину (если есть деталь)");
        Console.WriteLine("2. Отказать клиенту");
        Console.WriteLine("3. Купить запчасти");
        Console.WriteLine("4. Рискнуть (поставить случайную деталь, если нужной нет)");
        Console.WriteLine("Введите 'exit' для выхода.");
        Console.Write("Ваш выбор: ");
    }

    private ActionResult HandleRepair()
    {
        if (_currentClient == null)
        {
            return new ActionResult { Success = false, Message = "Нет активного клиента." };
        }

        var partOnStock = _gameLogic.GetPartByName(_currentClient.BrokenPartName);

        if (partOnStock == null)
        {
            return new ActionResult { Success = false, Message = $"Ошибка: Деталь '{_currentClient.BrokenPartName}' отсутствует в базе деталей." };
        }
        if (partOnStock.CurrentStock <= 0)
        {
            return new ActionResult { Success = false, Message = $"Деталь '{_currentClient.BrokenPartName}' закончилась на складе!" };
        }

        var result = _gameLogic.RepairClient(_currentClient, partOnStock);
        if (result.Success)
        {
            _currentClient = null; // Клиент обслужен
        }
        return result;
    }

    private void HandlePurchase()
    {
        Console.WriteLine("\n--- Меню Закупки ---");
        Console.WriteLine($"Ваш баланс: {_gameLogic.GetPlayerBalance():C}");
        Console.WriteLine("--------------------");
        Console.WriteLine("Введите команду в формате: 'Купить <Название детали> <Количество>' (Например: 'Купить Аккумулятор 2')");
        Console.WriteLine("Или напишите 'Назад' для выхода.");

        string input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input) || input.Trim().ToLower() == "назад") return;

        string[] parts = input.Trim().Split(' ');

        if (parts.Length < 3 || parts[0].ToLower() != "купить") 
        {
            Console.WriteLine("Ошибка: Неверный формат команды. Используйте: 'Купить <Название детали> <Количество>'");
            return;
        }

        // Извлекаем название детали
        string partName = string.Join(" ", parts.Skip(1).Take(parts.Length - 2)); 
        string quantityString = parts.Last();

        if (_gameLogic.ValidatePurchaseInput(partName, quantityString, out int quantity))
        {
            var result = _gameLogic.PurchasePart(partName, quantity);
            Console.WriteLine(result.Message);
        }
    }
}