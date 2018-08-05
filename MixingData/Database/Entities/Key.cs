using System;
using System.ComponentModel.DataAnnotations;

namespace MixingData.Database.Entities
{
    public class Key
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public Guid Key1 { get; set; }
        [Required]
        public Guid Key2 { get; set; }
    }
}
