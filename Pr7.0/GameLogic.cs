using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

public class GameLogic
{
    private readonly SqlHelper _sqlHelper;
    private GameStats _currentStats; // Кешируем статистику для быстрого доступа

    public GameLogic(string connectionString)
    {
        _sqlHelper = new SqlHelper(connectionString);
        LoadGameStats(); // Загружаем статистику при старте
    }


    private void LoadGameStats()
    {
        // Получаем первую (и единственную) запись статистики
        var dt = _sqlHelper.ExecuteDataTable("SELECT TOP 1 StatID, PlayerBalance, LastUpdate FROM GameStats ORDER BY StatID DESC");
        if (dt.Rows.Count > 0)
        {
            _currentStats = new GameStats
            {
                StatID = (int)dt.Rows[0]["StatID"],
                PlayerBalance = (decimal)dt.Rows[0]["PlayerBalance"],
                LastUpdate = (DateTime)dt.Rows[0]["LastUpdate"]
            };
        }
        else
        {
            // Если статистики нет (например, база только что создана), инициализируем
            _currentStats = new GameStats { StatID = 1, PlayerBalance = 1000.00m, LastUpdate = DateTime.Now };
            _sqlHelper.ExecuteNonQuery(
                "INSERT INTO GameStats (PlayerBalance, LastUpdate) VALUES (@Balance, @UpdateDate)",
                SqlHelper.CreateParameter("@Balance", _currentStats.PlayerBalance, SqlDbType.Decimal),
                SqlHelper.CreateParameter("@UpdateDate", _currentStats.LastUpdate, SqlDbType.DateTime)
            );
        }
    }

    public decimal GetPlayerBalance() => _currentStats.PlayerBalance;

    private void UpdatePlayerBalance(decimal newBalance)
    {
        _currentStats.PlayerBalance = newBalance;
        _currentStats.LastUpdate = DateTime.Now;
        _sqlHelper.ExecuteNonQuery(
            "UPDATE GameStats SET PlayerBalance = @Balance, LastUpdate = @UpdateDate WHERE StatID = @StatID",
            SqlHelper.CreateParameter("@Balance", newBalance, SqlDbType.Decimal),
            SqlHelper.CreateParameter("@UpdateDate", _currentStats.LastUpdate, SqlDbType.DateTime),
            SqlHelper.CreateParameter("@StatID", _currentStats.StatID, SqlDbType.Int)
        );
    }


    public List<Part> GetAllParts()
    {
        var dt = _sqlHelper.ExecuteDataTable("SELECT PartID, PartName, PurchasePrice, CurrentStock, Supplier FROM Parts ORDER BY PartName");
        return dt.AsEnumerable().Select(row => new Part
        {
            PartID = row.Field<int>("PartID"),
            PartName = row.Field<string>("PartName"),
            PurchasePrice = row.Field<decimal>("PurchasePrice"),
            CurrentStock = row.Field<int>("CurrentStock"),
            Supplier = row.Field<string>("Supplier")
        }).ToList();
    }

    public Part GetPartByName(string partName)
    {
        var dt = _sqlHelper.ExecuteDataTable("SELECT PartID, PartName, PurchasePrice, CurrentStock, Supplier FROM Parts WHERE PartName = @PartName",
            SqlHelper.CreateParameter("@PartName", partName, SqlDbType.NVarChar));
        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            return new Part
            {
                PartID = (int)row["PartID"],
                PartName = (string)row["PartName"],
                PurchasePrice = (decimal)row["PurchasePrice"],
                CurrentStock = (int)row["CurrentStock"],
                Supplier = (string)row["Supplier"]
            };
        }
        return null;
    }

    public Part GetPartById(int partId)
    {
        var dt = _sqlHelper.ExecuteDataTable("SELECT PartID, PartName, PurchasePrice, CurrentStock, Supplier FROM Parts WHERE PartID = @PartID",
            SqlHelper.CreateParameter("@PartID", partId, SqlDbType.Int));
        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            return new Part
            {
                PartID = (int)row["PartID"],
                PartName = (string)row["PartName"],
                PurchasePrice = (decimal)row["PurchasePrice"],
                CurrentStock = (int)row["CurrentStock"],
                Supplier = (string)row["Supplier"]
            };
        }
        return null;
    }

    public void UpdatePartStock(int partId, int newStock)
    {
        _sqlHelper.ExecuteNonQuery("UPDATE Parts SET CurrentStock = @NewStock WHERE PartID = @PartID",
            SqlHelper.CreateParameter("@NewStock", newStock, SqlDbType.Int),
            SqlHelper.CreateParameter("@PartID", partId, SqlDbType.Int));
    }

 

    // Генерирует случайного клиента
    public Client GenerateNewClient()
    {

        var availableParts = GetAllParts();
        if (!availableParts.Any()) return null; // Нет запчастей, нет клиентов

        Random rand = new Random();
        Part brokenPart = availableParts[rand.Next(availableParts.Count)];

        decimal repairCost = Math.Round(brokenPart.PurchasePrice * (decimal)(rand.NextDouble() * 0.5 + 1.2) + 10.00m, 2); // Деталь + работа
        decimal fineForRefusal = Math.Round(repairCost * 0.3m, 2); // Штраф 30% от стоимости
        decimal damageCost = Math.Round(repairCost * 1.5m, 2); // Ущерб 150% от стоимости

        return new Client
        {
            MachineModel = GetRandomMachineModel(),
            BrokenPartName = brokenPart.PartName,
            RepairCost = repairCost,
            FineForRefusal = fineForRefusal,
            DamageCost = damageCost
        };
    }

    private string GetRandomMachineModel()
    {
        string[] models = { "Седан", "Внедорожник", "Хэтчбек", "Купе", "Минивэн", "Пикап" };
        Random rand = new Random();
        return models[rand.Next(models.Length)];
    }

    public ActionResult RepairClient(Client client, Part selectedPart)
    {
        if (selectedPart.CurrentStock > 0)
        {
            // Деталь есть, выполняем ремонт
            UpdatePartStock(selectedPart.PartID, selectedPart.CurrentStock - 1);
            decimal newBalance = GetPlayerBalance() + client.RepairCost;
            UpdatePlayerBalance(newBalance);

      
            return new ActionResult { Success = true, Message = $"Ремонт выполнен! Вы заработали {client.RepairCost:C}. Баланс: {GetPlayerBalance():C}." };
        }
        else
        {
            return new ActionResult { Success = false, Message = $"Ошибка: Деталь '{selectedPart.PartName}' закончилась на складе!" };
        }
    }

    public ActionResult RefuseClient(Client client)
    {
        decimal balance = GetPlayerBalance();
        if (client.FineForRefusal.HasValue)
        {
            balance -= client.FineForRefusal.Value;
            UpdatePlayerBalance(balance);
            return new ActionResult { Success = true, Message = $"Вы отказались от клиента. Штраф: {client.FineForRefusal.Value:C}. Баланс: {balance:C}." };
        }
        else
        {
            return new ActionResult { Success = true, Message = $"Клиент отказан (штрафа нет). Баланс: {balance:C}." };
        }
    }

    public ActionResult RiskRepair(Client client)
    {
        var availableParts = GetAllParts().Where(p => p.CurrentStock > 0).ToList();

        if (!availableParts.Any())
        {
            return new ActionResult { Success = false, Message = "Критическая ошибка: нет ни одной запчасти на складе для риск-ремонта!" };
        }

        Random rand = new Random();
        Part randomPart = availableParts[rand.Next(availableParts.Count)];

    
        UpdatePartStock(randomPart.PartID, randomPart.CurrentStock - 1);
        decimal balance = GetPlayerBalance();
        balance += client.RepairCost; // Получили оплату за ремонт

        // Возмещаем ущерб
        if (client.DamageCost.HasValue)
        {
            balance -= client.DamageCost.Value;
            return new ActionResult
            {
                Success = false, // Риск не удался
                Message = $"Вы поставили случайную деталь: '{randomPart.PartName}' (вместо '{client.BrokenPartName}'). Клиент недоволен! Ущерб: {client.DamageCost.Value:C}. Баланс: {balance:C}."
            };
        }
        else
        {
   
            return new ActionResult { Success = true, Message = $"Вы поставили случайную деталь: '{randomPart.PartName}'. Клиент доволен (случайно)! Баланс: {balance:C}." };
        }
    }



    public ActionResult PurchasePart(string partName, int quantity)
    {
        if (quantity <= 0)
            return new ActionResult { Success = false, Message = "Количество должно быть больше нуля." };

        var partToBuy = GetPartByName(partName);
        if (partToBuy == null)
            return new ActionResult { Success = false, Message = $"Запчасть '{partName}' не найдена." };

        decimal totalCost = partToBuy.PurchasePrice * quantity;
        decimal currentBalance = GetPlayerBalance();

        if (currentBalance < totalCost)
            return new ActionResult { Success = false, Message = $"Недостаточно средств. Нужно {totalCost:C}, у вас {currentBalance:C}." };

        // Проводим транзакцию: списываем деньги, добавляем в историю закупок
        UpdatePlayerBalance(currentBalance - totalCost);

        // Рассчитываем дату прибытия (через 2 клиента)

        DateTime arrivalDate = DateTime.Now.AddDays(2);

        _sqlHelper.ExecuteNonQuery(
            "INSERT INTO InventoryHistory (PartID, Quantity, PurchaseDate, ArrivalDate, Cost) VALUES (@PartID, @Quantity, @PurchaseDate, @ArrivalDate, @Cost)",
            SqlHelper.CreateParameter("@PartID", partToBuy.PartID, SqlDbType.Int),
            SqlHelper.CreateParameter("@Quantity", quantity, SqlDbType.Int),
            SqlHelper.CreateParameter("@PurchaseDate", DateTime.Now, SqlDbType.DateTime),
            SqlHelper.CreateParameter("@ArrivalDate", arrivalDate, SqlDbType.DateTime),
            SqlHelper.CreateParameter("@Cost", totalCost, SqlDbType.Decimal)
        );

        return new ActionResult { Success = true, Message = $"Заказ на {quantity} шт. '{partToBuy.PartName}' на сумму {totalCost:C} оформлен. Детали прибудут {arrivalDate:d}." };
    }

    public void ProcessDelayedInventory()
    {
        var dt = _sqlHelper.ExecuteDataTable("SELECT ih.HistoryID, ih.PartID, ih.Quantity, ih.Cost, p.PartName FROM InventoryHistory ih JOIN Parts p ON ih.PartID = p.PartID WHERE ih.ArrivalDate <= @CurrentDate",
            SqlHelper.CreateParameter("@CurrentDate", DateTime.Now, SqlDbType.DateTime));

        if (dt.Rows.Count == 0) return;

        foreach (DataRow row in dt.Rows)
        {
            int historyId = (int)row["HistoryID"];
            int partId = (int)row["PartID"];
            string partName = (string)row["PartName"];
            int quantity = (int)row["Quantity"];

            // Обновляем склад
            var part = GetPartById(partId); // Получаем актуальное количество
            if (part != null)
            {
                UpdatePartStock(partId, part.CurrentStock + quantity);
                Console.WriteLine($"\n--- Прибыли запчасти: {quantity} шт. '{partName}' ---");
            }

            _sqlHelper.ExecuteNonQuery("DELETE FROM InventoryHistory WHERE HistoryID = @HistoryID",
                SqlHelper.CreateParameter("@HistoryID", historyId, SqlDbType.Int));
        }
    }


    public bool ValidatePurchaseInput(string partName, string quantityString, out int quantity)
    {
        if (string.IsNullOrWhiteSpace(partName))
        {
            Console.WriteLine("Ошибка: Название детали не может быть пустым.");
            quantity = 0;
            return false;
        }

        if (!int.TryParse(quantityString, out quantity) || quantity <= 0)
        {
            Console.WriteLine("Ошибка: Количество должно быть положительным числом.");
            return false;
        }

        if (GetPartByName(partName) == null)
        {
            Console.WriteLine($"Ошибка: Деталь '{partName}' не существует.");
            return false;
        }

        return true;
    }


    public bool IsValidNewPartData(string name, decimal price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Ошибка: Имя детали не может быть пустым."); return false; }
        if (price < 0) { Console.WriteLine("Ошибка: Цена покупки не может быть отрицательной."); return false; }
        if (stock < 0) { Console.WriteLine("Ошибка: Количество на складе не может быть отрицательным."); return false; }
        if (GetPartByName(name) != null) { Console.WriteLine($"Ошибка: Деталь '{name}' уже существует."); return false; }
        return true;
    }
}
