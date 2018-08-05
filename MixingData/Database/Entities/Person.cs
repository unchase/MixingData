using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MixingData.Database.Entities
{
    public class Person
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string Patronymic { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Address { get; set; }

    }
}
