using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MigrationLib
{
    /// <summary>
    /// Сравнивает старую и новую схему и строит SQL для применения и отката миграции.
    /// </summary>
    public class MigrationGenerator
    {
        private readonly ISqlGenerator _sql;

        public MigrationGenerator(ISqlGenerator sql)
        {
            _sql = sql;
        }

        // oldSnapshot -> текущая схема БД (по последней миграции)
        // newSnapshot -> то, что описано C# моделями
        public (string upSql, string downSql) GenerateMigration(ModelSnapshot oldSnapshot, ModelSnapshot newSnapshot)
        {
            var up = new StringBuilder();
            var down = new StringBuilder();

            var oldTables = oldSnapshot.Tables.ToDictionary(t => t.Name);
            var newTables = newSnapshot.Tables.ToDictionary(t => t.Name);

            // Новые таблицы
            foreach (var newTable in newTables.Values)
            {
                if (!oldTables.ContainsKey(newTable.Name))
                {
                    up.AppendLine(_sql.GenerateCreateTable(newTable));
                    down.Insert(0, _sql.GenerateDropTable(newTable.Name) + "\n");
                }
            }

            // Удалённые таблицы
            foreach (var oldTable in oldTables.Values)
            {
                if (!newTables.ContainsKey(oldTable.Name))
                {
                    up.AppendLine(_sql.GenerateDropTable(oldTable.Name));
                    down.Insert(0, _sql.GenerateCreateTable(oldTable) + "\n");
                }
            }

            // Таблицы, которые есть и там, и там
            foreach (var newTable in newTables.Values)
            {
                if (!oldTables.TryGetValue(newTable.Name, out var oldTable))
                    continue;

                var oldCols = oldTable.Columns.ToDictionary(c => c.Name);
                var newCols = newTable.Columns.ToDictionary(c => c.Name);

                // Новые колонки
                foreach (var col in newCols.Values)
                {
                    if (!oldCols.ContainsKey(col.Name))
                    {
                        up.AppendLine(_sql.GenerateAddColumn(newTable.Name, col));
                        down.Insert(0, _sql.GenerateDropColumn(newTable.Name, col.Name) + "\n");
                    }
                }

                // Удалённые колонки
                foreach (var col in oldCols.Values)
                {
                    if (!newCols.ContainsKey(col.Name))
                    {
                        up.AppendLine(_sql.GenerateDropColumn(newTable.Name, col.Name));
                        down.Insert(0, _sql.GenerateAddColumn(newTable.Name, col) + "\n");
                    }
                }

                // Изменение типа существующих колонок
                foreach (var newCol in newCols.Values)
                {
                    if (!oldCols.TryGetValue(newCol.Name, out var oldCol))
                        continue;

                    if (oldCol.ClrType != newCol.ClrType)
                    {
                        // up: old -> new
                        up.AppendLine(_sql.GenerateAlterColumnType(newTable.Name, oldCol, newCol));
                        // down: new -> old (обратный каст)
                        down.Insert(0, _sql.GenerateAlterColumnType(newTable.Name, newCol, oldCol) + "\n");
                    }
                }

                // Смена PRIMARY KEY (одна колонка на таблицу)
                string? oldPk = oldCols.Values.FirstOrDefault(c => c.IsPrimaryKey)?.Name;
                string? newPk = newCols.Values.FirstOrDefault(c => c.IsPrimaryKey)?.Name;

                if (oldPk != newPk)
                {
                    // Варианты:
                    // - был PK, не стало -> DROP PK
                    // - не было PK, появился -> ADD PK
                    // - был на одной колонке, стал на другой -> DROP + ADD

                    // UP 
                    if (oldPk != null)
                    {
                        up.AppendLine(_sql.GenerateDropPrimaryKey(newTable.Name));
                    }
                    if (newPk != null)
                    {
                        up.AppendLine(_sql.GenerateAddPrimaryKey(newTable.Name, newPk));
                    }

                    // DOWN (обратная последовательность)
                    // сначала отменяем то, что добавили в up
                    if (newPk != null)
                    {
                        down.Insert(0, _sql.GenerateDropPrimaryKey(newTable.Name) + "\n");
                    }
                    if (oldPk != null)
                    {
                        down.Insert(0, _sql.GenerateAddPrimaryKey(newTable.Name, oldPk) + "\n");
                    }
                }
            }

            return (up.ToString(), down.ToString());
        }
    }
}
