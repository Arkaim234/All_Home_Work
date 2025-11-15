using MigrationLib;

namespace ConsoleApp.Models
{
    /// <summary>
    /// Пример доменной модели пользователя, которая мапится на таблицу users в базе.
    /// </summary>
    [Table("users")]
    public class User
    {
        [PrimaryKey]
        public int Id { get; set; }
        
        [Column]
        public string Name { get; set; } = "";
    }

}