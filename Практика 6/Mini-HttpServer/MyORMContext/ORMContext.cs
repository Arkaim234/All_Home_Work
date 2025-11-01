using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
        /// Создает новую запись в таблице
        /// </summary>
        public T Create<T>(T entity, string tableName) where T : class
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                // Получаем свойства объекта через рефлексию
                PropertyInfo[] properties = typeof(T).GetProperties();

                // Формируем списки колонок и параметров (исключая Id для автоинкремента)
                List<string> columns = new List<string>();
                List<string> parameters = new List<string>();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower() != "id") // Пропускаем Id
                    {
                        columns.Add(prop.Name);
                        parameters.Add($"@{prop.Name}");
                    }
                }

                string sql = $"INSERT INTO {tableName} ({string.Join(", ", columns)}) " +
                             $"VALUES ({string.Join(", ", parameters)}) RETURNING Id";

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    // Добавляем параметры
                    foreach (var prop in properties)
                    {
                        if (prop.Name.ToLower() != "id")
                        {
                            command.Parameters.AddWithValue($"@{prop.Name}", prop.GetValue(entity) ?? DBNull.Value);
                        }
                    }

                    // Получаем Id созданной записи
                    var newId = command.ExecuteScalar();

                    // Устанавливаем Id в объект
                    PropertyInfo idProperty = typeof(T).GetProperty("Id");
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
        public T ReadById<T>(int id, string tableName) where T : class, new()
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
                            // Маппинг данных из таблицы в объект
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

                // Получаем свойства объекта
                PropertyInfo[] properties = typeof(T).GetProperties();
                List<string> setStatements = new List<string>();

                foreach (var prop in properties)
                {
                    if (prop.Name.ToLower() != "id") // Пропускаем Id
                    {
                        setStatements.Add($"{prop.Name} = @{prop.Name}");
                    }
                }

                string sql = $"UPDATE {tableName} SET {string.Join(", ", setStatements)} WHERE Id = @id";

                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                // Добавляем параметры для всех свойств
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

        private T MapReaderToObject<T>(NpgsqlDataReader reader) where T : class, new()
        {
            T obj = new T();
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                try
                {
                    // Проверяем, есть ли колонка с таким именем
                    int ordinal = reader.GetOrdinal(prop.Name);

                    if (!reader.IsDBNull(ordinal))
                    {
                        object value = reader.GetValue(ordinal);

                        // Преобразуем тип данных при необходимости
                        if (value != null && prop.PropertyType != value.GetType())
                        {
                            value = Convert.ChangeType(value, prop.PropertyType);
                        }

                        prop.SetValue(obj, value);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // Колонка не найдена в результате - пропускаем
                    continue;
                }
            }

            return obj;
        }
    }
}