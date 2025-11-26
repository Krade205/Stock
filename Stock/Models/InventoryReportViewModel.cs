using System.Collections.Generic; // Thêm thư viện List

namespace Stock.Models
{
    public class InventoryReportViewModel
    {
        // Các chỉ số thống kê (Giữ nguyên)
        public int TotalImport { get; set; }
        public decimal TotalImportValue { get; set; }
        public int TotalExport { get; set; }
        public decimal TotalExportValue { get; set; }
        public int CurrentStock { get; set; }

        // --- THÊM MỚI: Danh sách sản phẩm để hiển thị bảng bên dưới ---
        public List<Product> ProductList { get; set; }
    }
}