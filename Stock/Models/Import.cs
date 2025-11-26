using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stock.Models
{
    [Table("Imports")]
    public class Import
    {
        [Key] public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string? Provider { get; set; }
    }
}