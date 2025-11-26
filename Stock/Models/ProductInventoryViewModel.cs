using System.Collections.Generic;

namespace Stock.Models
{
    public class ProductInventoryViewModel
    {
        public Product Product { get; set; } // Thông tin sản phẩm (Tên, Mã...)

        public int TotalImport { get; set; } // Tổng nhập của SP này
        public int TotalExport { get; set; } // Tổng xuất của SP này
        public int CurrentStock { get; set; } // Tồn kho hiện tại

        // Danh sách lịch sử giao dịch của riêng SP này
        public List<StockTransaction> Transactions { get; set; }
    }
}