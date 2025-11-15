using System;
using System.Collections.Generic;

namespace MigrationLib
{
    /// <summary>
    /// Атрибут для пометки C#-класса как таблицы в базе данных.
    /// </summary>

    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public string Name { get; }
        public TableAttribute(string name) => Name = name;
    }
    /// <summary>
    /// Атрибут для пометки свойства как обычной колонки таблицы.
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute { }

    /// <summary>
    /// Атрибут для пометки свойства как первичного ключа таблицы.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute { }

    /// <summary>
    /// Модель одной колонки в схеме БД (имя, тип и признак первичного ключа).
    /// </summary>

    public class ColumnModel
    {
        public string Name { get; set; } = "";
        public string ClrType { get; set; } = "";  // "int" или "string"
        public bool IsPrimaryKey { get; set; }
    }

    /// <summary>
    /// Модель таблицы в схеме БД: содержит имя и список колонок.
    /// </summary>
    public class TableModel
    {
        public string Name { get; set; } = "";
        public List<ColumnModel> Columns { get; set; } = new();
    }

    /// <summary>
    /// Снимок схемы базы данных, то есть набор таблиц с колонками на определённый момент.
    /// </summary>
    public class ModelSnapshot
    {
        public List<TableModel> Tables { get; set; } = new();
    }

    /// <summary>
    /// Запись о миграции из таблицы _migrations, хранит имя, время применения, снапшот модели и SQL.
    /// </summary>

    public class MigrationRecord
    {
        public int Id { get; set; }
        public string MigrationName { get; set; } = "";
        public DateTime? AppliedAt { get; set; }
        public string ModelSnapshotJson { get; set; } = "";
        public string UpSql { get; set; } = "";
        public string DownSql { get; set; } = "";
    }
}
