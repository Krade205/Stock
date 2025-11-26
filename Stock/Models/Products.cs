using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stock.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public string? Code { get; set; }

        public string? Name { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }

        public int Quantity { get; set; } = 0;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }
        //[Column("image")] // Ánh xạ với cột 'image' trong SQL
        //public string? ImageName { get; set; }
    }
}