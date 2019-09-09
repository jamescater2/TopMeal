using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TopMealApi.Model
{
    [Table("User")]
    public class User
    {
        [Required]
        public int Id { get; set;}
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        [Required]
        public int RoleId { get; set; }
        [Required]
        public int DailyCalories { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

        public User AssignFrom(User rhs)
        {
            Id = rhs.Id;
            Name = rhs.Name;
            RoleId = rhs.RoleId;
            DailyCalories = rhs.DailyCalories;
            PasswordHash = rhs.PasswordHash;
            PasswordSalt = rhs.PasswordSalt;
            return this;
        }
    }
}