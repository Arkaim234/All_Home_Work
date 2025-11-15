using System;
using System.Collections.Generic;
using Npgsql;

namespace MigrationLib
{
    /// <summary>
    /// Адаптер для работы с PostgreSQL и таблицей _migrations (чтение, запись, транзакции).
    /// </summary>
    public class PostgresDatabaseAdapter
    {
        private readonly string _connectionString;

        public PostgresDatabaseAdapter(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void EnsureMigrationsTable()
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS _migrations (
    id SERIAL PRIMARY KEY,
    migration_name TEXT NOT NULL,
    applied_at TIMESTAMP NULL,
    model_snapshot TEXT NOT NULL,
    up_sql TEXT NOT NULL,
    down_sql TEXT NOT NULL
);";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        public MigrationRecord? GetLastMigration()
        {
            const string sql = @"SELECT id, migration_name, applied_at, model_snapshot, up_sql, down_sql
                                 FROM _migrations
                                 ORDER BY id DESC
                                 LIMIT 1;";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;

            return new MigrationRecord
            {
                Id = reader.GetInt32(0),
                MigrationName = reader.GetString(1),
                AppliedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                ModelSnapshotJson = reader.GetString(3),
                UpSql = reader.GetString(4),
                DownSql = reader.GetString(5)
            };
        }

        public List<MigrationRecord> GetAllMigrations()
        {
            const string sql = @"SELECT id, migration_name, applied_at, model_snapshot, up_sql, down_sql
                                 FROM _migrations
                                 ORDER BY id;";

            var list = new List<MigrationRecord>();

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MigrationRecord
                {
                    Id = reader.GetInt32(0),
                    MigrationName = reader.GetString(1),
                    AppliedAt = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                    ModelSnapshotJson = reader.GetString(3),
                    UpSql = reader.GetString(4),
                    DownSql = reader.GetString(5)
                });
            }

            return list;
        }

        public void InsertMigration(MigrationRecord record)
        {
            const string sql = @"
INSERT INTO _migrations (migration_name, applied_at, model_snapshot, up_sql, down_sql)
VALUES (@name, NULL, @snapshot, @up, @down)
RETURNING id;";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", record.MigrationName);
            cmd.Parameters.AddWithValue("@snapshot", record.ModelSnapshotJson);
            cmd.Parameters.AddWithValue("@up", record.UpSql);
            cmd.Parameters.AddWithValue("@down", record.DownSql);

            record.Id = Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void MarkMigrationApplied(int id)
        {
            const string sql = @"UPDATE _migrations SET applied_at = NOW() WHERE id = @id;";
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void MarkMigrationRolledBack(int id)
        {
            const string sql = @"UPDATE _migrations SET applied_at = NULL WHERE id = @id;";
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void ExecuteInTransaction(string sql)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new NpgsqlCommand(sql, conn, tx);
                cmd.ExecuteNonQuery();
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
