using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TopMealApi.Model
{
    [Table("DailyUserCalories")]
    public class DailyUserCalories
    {
        [Required]
        [Key, Column(Order = 0)]
        public int UserId { get; set; }
        [Required]
        [Key, Column(Order = 1)]
        public DateTime Date { get; set; }
        [Required]
        public long Calories { get; set;}

        public DailyUserCalories AssignFrom(DailyUserCalories rhs)
        {
            UserId = rhs.UserId;
            Date = rhs.Date;
            Calories = rhs.Calories;
            return this;
        }
    }
}