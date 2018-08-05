using System.Data.Entity;
using MixingData.Database.Entities;

namespace MixingData.Database
{
    public class PersonContext : DbContext
    {
        public PersonContext()
            :base("DbConnection")
        { }
          
        public DbSet<Person> Persons { get; set; }
        public DbSet<NewPerson> NewPersons { get; set; }
        public DbSet<Key> Keys { get; set; }
    }
}
