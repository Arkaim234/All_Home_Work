using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace MigrationLib
{
    public class ModelSnapshotBuilder
    {
        /// <summary>
        /// Строит снимок схемы БД по C#-моделям с атрибутами Table/Column/PrimaryKey.
        /// </summary>
        public ModelSnapshot BuildFromAssembly(Assembly asm)
        {
            var snapshot = new ModelSnapshot();

            var types = asm.GetTypes()
                .Where(t => t.GetCustomAttribute<TableAttribute>() != null);

            foreach (var type in types)
            {
                var tableAttr = type.GetCustomAttribute<TableAttribute>();
                if (tableAttr == null) continue;

                var table = new TableModel { Name = tableAttr.Name };

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    bool isPk = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                    bool isColumn = prop.GetCustomAttribute<ColumnAttribute>() != null || isPk;
                    if (!isColumn) continue;

                    var clrType = prop.PropertyType;
                    if (clrType != typeof(int) && clrType != typeof(string))
                        throw new NotSupportedException("Поддерживаются только int и string");

                    table.Columns.Add(new ColumnModel
                    {
                        Name = prop.Name.ToLower(), // поле Name -> колонка name
                        ClrType = clrType == typeof(int) ? "int" : "string",
                        IsPrimaryKey = isPk
                    });
                }

                snapshot.Tables.Add(table);
            }

            return snapshot;
        }
    }
}
