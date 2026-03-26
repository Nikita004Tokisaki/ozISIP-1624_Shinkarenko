

using System;

public class Part
{
    public int PartID { get; set; }
    public string PartName { get; set; }
    public decimal PurchasePrice { get; set; }
    public int CurrentStock { get; set; }
    public string Supplier { get; set; }
}

public class Client
{
    public int ClientID { get; set; }
    public string MachineModel { get; set; } // Может быть null
    public string BrokenPartName { get; set; }
    public decimal RepairCost { get; set; }
    public decimal? FineForRefusal { get; set; } // Nullable
    public decimal? DamageCost { get; set; } // Nullable
}

public class InventoryItem // Для отображения покупок с задержкой
{
    public int PartID { get; set; }
    public string PartName { get; set; }
    public int QuantityPurchased { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ArrivalDate { get; set; }
}

public class GameStats
{
    public int StatID { get; set; }
    public decimal PlayerBalance { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class ActionResult // Для возврата результата действия
{
    public bool Success { get; set; }
    public string Message { get; set; }
}