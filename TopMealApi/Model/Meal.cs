using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TopMealApi.Model
{
    [Table("Meal")]
    public class Meal
    {
        [Required]
        public int Id { get; set;}
        [Required]
        public int UserId { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public DateTime Time { get; set; }
        [Required]
        public int Calories { get; set; }
        [Required]
        public bool WithinLimit { get; set; } = true;
        [StringLength(255)]
        public string Description { get; set; }

        public Meal AssignFrom(Meal rhs)
        {
            Id = rhs.Id;
            UserId = rhs.UserId;
            Date = rhs.Date;
            Time = rhs.Time;
            Calories = rhs.Calories;
            WithinLimit = rhs.WithinLimit;
            Description = rhs.Description;
            return this;
        }
    }
}
