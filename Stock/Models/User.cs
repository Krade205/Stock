using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stock.Models // Namespace phải là Stock.Models
{
    [Table("Users")] // Khớp với bảng Users trong SQL Server
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; } // Cột Họ tên
        public string Role { get; set; }     // Cột Quyền (Admin/User)
    }
}