using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stock.Models
{
    [Table("StockTransactions")]
    public class StockTransaction
    {
        [Key]
        public int Id { get; set; }

        // --- KHÓA NGOẠI ---
        [Required]
        public int ProductId { get; set; } // Chỉ lưu ID sản phẩm

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; } //  hiển thị tên sản phẩm ra View
        // --------------------------------------

        // Phần Nhập
        public DateTime ImportDateTime { get; set; } = DateTime.Now;
        public int ImportQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ImportPrice { get; set; } = 0;

        // Phần Xuất
        public DateTime? ExportDateTime { get; set; }
        public int? ExportQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ExportPrice { get; set; } = 0;

        // Tồn kho sau giao dịch
        public int StockRemaining { get; set; }

        // Ghi chú
        public string? Note { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính (Cái, Hộp...)")]
        public string Unit { get; set; }

    }
}