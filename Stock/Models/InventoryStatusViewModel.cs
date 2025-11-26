namespace Stock.Models
{
    public class InventoryStatusViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }

        public int TotalImport { get; set; } // Tổng nhập
        public int TotalExport { get; set; } // Tổng xuất
        public int CurrentStock { get; set; } // Tồn hiện tại
    }
}