using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace MyORMLibrary
{
    public class ORMContext
    {
        private readonly string _connectionString;

        public ORMContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Возвращает первую запись, соответствующую условию, или null
        /// </summary>
        public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate, string tableName) where T : class, new()
        {
            var sqlQuery = BuildSqlQuery(predicate, tableName, singleResult: true);
            return ExecuteQuerySingle<T>(sqlQuery);
        }

        /// <summary>
        /// Возвращает все записи, соответствующие условию
        /// </summary>
        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate, string tableName) where T : class, new()
        {
            var sqlQuery = BuildSqlQuery(predicate, tableName, singleResult: false);
            return ExecuteQueryMultiple<T>(sqlQuery);
        }

        /// <summary>
        /// Строит SQL-запрос на основе LINQ-выражения
        /// </summary>
        private string BuildSqlQuery<T>(Expression<Func<T, bool>> predicate, string tableName, bool singleResult)
        {
            var whereClause = ParseExpression(predicate.Body);
            var limitClause = singleResult ? "LIMIT 1" : string.Empty;

            return $"SELECT * FROM {tableName} WHERE {whereClause} {limitClause}".Trim();
        }

        /// <summary>
        /// Разбирает дерево выражений в SQL WHERE-условие
        /// </summary>
        private string ParseExpression(Expression expression)
        {
            if (expression is BinaryExpression binary)
            {
                var left = ParseExpression(binary.Left);
                var right = ParseExpression(binary.Right);
                var op = GetSqlOperator(binary.NodeType);
                return $"({left} {op} {right})";
            }
            else if (expression is MemberExpression member)
            {
                return member.Member.Name;
            }
            else if (expression is ConstantExpression constant)
            {
                return FormatConstant(constant.Value);
            }
            else if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Not)
            {
                var operand = ParseExpression(unary.Operand);
                return $"NOT {operand}";
            }

            throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
        }

        /// <summary>
        /// Преобразует тип операции Expression в SQL-оператор
        /// </summary>
        private string GetSqlOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Unsupported node type: {nodeType}")
            };
        }

        /// <summary>
        /// Форматирует константное значение для SQL
        /// </summary>
        private string FormatConstant(object? value)
        {
            if (value == null)
                return "NULL";

            if (value is string str)
                return $"'{str.Replace("'", "''")}'"; // Экранирование одинарных кавычек

            if (value is bool b)
                return b ? "TRUE" : "FALSE";

            if (value is DateTime dt)
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";

            return value.ToString() ?? "NULL";
        }

        /// <summary>
        /// Выполняет SQL-запрос и возвращает одну запись
        /// </summary>
        private T? ExecuteQuerySingle<T>(string query) where T : class, new()
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToObject<T>(reader);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Выполняет SQL-запрос и возвращает коллекцию записей
        /// </summary>
        private IEnumerable<T> ExecuteQueryMultiple<T>(string query) where T : class, new()
        {
            var results = new List<T>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(MapReaderToObject<T>(reader));
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Создает новую запись в таблице
        /// </summary>
        public T Create<T>(T entity, string tableName) where T : class
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                PropertyInfo[] properties = typeof(T).GetProperties();
                List<string> columns = new List<string>();
                List<string> parameters = new List<string>();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower() != "id")
                    {
                        columns.Add(prop.Name);
                        parameters.Add($"@{prop.Name}");
                    }
                }

                string sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) " +
                             $"VALUES ({string.Join(", ", parameters)}) RETURNING Id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    foreach (var prop in properties)
                    {
                        if (prop.Name.ToLower() != "id")
                        {
                            command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);
                        }
                    }

                    var newId = command.ExecuteScalar();

                    PropertyInfo? idProperty = typeof(T).GetProperty("Id");
                    if (idProperty != null && newId != null)
                    {
                        idProperty.SetValue(entity, Convert.ToInt32(newId));
                    }

                    return entity;
                }
            }
        }

        /// <summary>
        /// Читает запись по Id
        /// </summary>
        public T? ReadById<T>(int id, string tableName) where T : class, new()
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM {tableName} WHERE Id = @id";
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToObject<T>(reader);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Читает все записи из таблицы
        /// </summary>
        public List<T> ReadByAll<T>(string tableName) where T : class, new()
        {
            List<T> results = new List<T>();

            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                string sql = $"SELECT * FROM {tableName}";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(MapReaderToObject<T>(reader));
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Обновляет запись в таблице
        /// </summary>
        public void Update<T>(int id, T entity, string tableName) where T : class
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                PropertyInfo[] properties = typeof(T).GetProperties();
                List<string> setStatements = new List<string>();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower() != "id")
                    {
                        setStatements.Add($"{prop.Name} = @{prop.Name}");
                    }
                }

                string sql = $"UPDATE {tableName} SET {string.Join(", ", setStatements)} WHERE Id = @id";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower() != "id")
                    {
                        command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);
                    }
                }

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Удаляет запись из таблицы
        /// </summary>
        public void Delete(int id, string tableName)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                string sql = $"DELETE FROM {tableName} WHERE Id = @id";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Маппер: преобразует строку из DataReader в объект типа T
        /// </summary>
        private T MapReaderToObject<T>(NpgsqlDataReader reader) where T : class, new()
        {
            T obj = new T();
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                try
                {
                    int ordinal = reader.GetOrdinal(prop.Name);

                    if (!reader.IsDBNull(ordinal))
                    {
                        object value = reader.GetValue(ordinal);

                        if (value != null && prop.PropertyType != value.GetType())
                        {
                            value = Convert.ChangeType(value, prop.PropertyType);
                        }

                        prop.SetValue(obj, value);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    continue;
                }
            }

            return obj;
        }
    }
}