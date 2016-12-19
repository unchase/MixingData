using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

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
