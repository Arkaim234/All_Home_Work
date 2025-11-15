using System;
using System.Collections.Generic;
using System.Text;

namespace MigrationLib
{
    /// <summary>
    /// Интерфейс генератора SQL под конкретную СУБД на основе моделей таблиц и колонок.
    /// </summary>
    public interface ISqlGenerator
    {
        string MapClrTypeToSql(string clrType);
        string GenerateCreateTable(TableModel table);
        string GenerateDropTable(string tableName);
        string GenerateAddColumn(string tableName, ColumnModel column);
        string GenerateDropColumn(string tableName, string columnName);

        // смена типа и управление PK
        string GenerateAlterColumnType(string tableName, ColumnModel oldColumn, ColumnModel newColumn);
        string GenerateDropPrimaryKey(string tableName);
        string GenerateAddPrimaryKey(string tableName, string columnName);
    }

    /// <summary>
    /// Реализация генератора SQL для PostgreSQL (CREATE TABLE, ALTER TABLE и т.п.).
    /// </summary>
    public class PostgresSqlGenerator : ISqlGenerator
    {
        public string MapClrTypeToSql(string clrType) => clrType switch
        {
            "int" => "INTEGER",
            "string" => "TEXT",
            _ => throw new NotSupportedException()
        };

        public string GenerateCreateTable(TableModel table)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {table.Name} (");

            var defs = new List<string>();
            foreach (var col in table.Columns)
            {
                var sqlType = MapClrTypeToSql(col.ClrType);
                var def = $"{col.Name} {sqlType}";
                if (col.IsPrimaryKey)
                    def += " PRIMARY KEY";
                defs.Add(def);
            }

            sb.AppendLine(string.Join(",\n", defs));
            sb.AppendLine(");");
            return sb.ToString();
        }

        public string GenerateDropTable(string tableName)
            => $"DROP TABLE {tableName};";

        public string GenerateAddColumn(string tableName, ColumnModel column)
            => $"ALTER TABLE {tableName} ADD COLUMN {column.Name} {MapClrTypeToSql(column.ClrType)};";

        public string GenerateDropColumn(string tableName, string columnName)
            => $"ALTER TABLE {tableName} DROP COLUMN {columnName};";

        // смена типа колонки
        public string GenerateAlterColumnType(string tableName, ColumnModel oldColumn, ColumnModel newColumn)
        {
            // oldColumn и newColumn имеют одинаковое Name, только ClrType отличается
            var sqlType = MapClrTypeToSql(newColumn.ClrType);

            // В Postgres смена типа с явным кастом через USING
            return
                $"ALTER TABLE {tableName} ALTER COLUMN {newColumn.Name} TYPE {sqlType} USING {newColumn.Name}::{sqlType};";
        }

        // НОВОЕ: удалить PK
        public string GenerateDropPrimaryKey(string tableName)
        {
            // Мы рассчитываем, что PK создавался как "PRIMARY KEY" без имени,
            // тогда Postgres даёт ему имя {table}_pkey
            return $"ALTER TABLE {tableName} DROP CONSTRAINT {tableName}_pkey;";
        }

        // добавить PK
        public string GenerateAddPrimaryKey(string tableName, string columnName)
        {
            return $"ALTER TABLE {tableName} ADD PRIMARY KEY ({columnName});";
        }
    }
}
