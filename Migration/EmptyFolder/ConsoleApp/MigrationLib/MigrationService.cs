using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace MigrationLib
{
    /// <summary>
    /// Высокоуровневый сервис миграций: создание, применение, откат и статус миграций.
    /// </summary>
    public class MigrationService
    {
        private readonly PostgresDatabaseAdapter _db;
        private readonly MigrationGenerator _generator;
        private readonly ModelSnapshotBuilder _snapshotBuilder;
        private readonly Assembly _modelsAssembly;

        public MigrationService(
            PostgresDatabaseAdapter db,
            MigrationGenerator generator,
            ModelSnapshotBuilder snapshotBuilder,
            Assembly modelsAssembly)
        {
            _db = db;
            _generator = generator;
            _snapshotBuilder = snapshotBuilder;
            _modelsAssembly = modelsAssembly;
        }

        public MigrationRecord CreateMigration()
        {
            _db.EnsureMigrationsTable();

            var newSnapshot = _snapshotBuilder.BuildFromAssembly(_modelsAssembly);
            var last = _db.GetLastMigration();

            ModelSnapshot oldSnapshot;
            if (last == null)
                oldSnapshot = new ModelSnapshot();
            else
                oldSnapshot = JsonSerializer.Deserialize<ModelSnapshot>(last.ModelSnapshotJson)
                              ?? new ModelSnapshot();

            var (up, down) = _generator.GenerateMigration(oldSnapshot, newSnapshot);

            var name = $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";

            var record = new MigrationRecord
            {
                MigrationName = name,
                AppliedAt = null,
                ModelSnapshotJson = JsonSerializer.Serialize(newSnapshot),
                UpSql = up,
                DownSql = down
            };

            _db.InsertMigration(record);
            return record;
        }

        public MigrationRecord ApplyLastMigration()
        {
            var last = _db.GetLastMigration();
            if (last == null)
                throw new InvalidOperationException("Нет миграций для применения");

            if (last.AppliedAt != null)
                throw new InvalidOperationException("Последняя миграция уже применена");

            if (string.IsNullOrWhiteSpace(last.UpSql))
                throw new InvalidOperationException("UpSql пустой");

            _db.ExecuteInTransaction(last.UpSql);
            _db.MarkMigrationApplied(last.Id);

            return last;
        }

        public MigrationRecord RollbackLastMigration()
        {
            var last = _db.GetLastMigration();
            if (last == null || last.AppliedAt == null)
                throw new InvalidOperationException("Нет применённой миграции для отката");

            if (string.IsNullOrWhiteSpace(last.DownSql))
                throw new InvalidOperationException("DownSql пустой");

            _db.ExecuteInTransaction(last.DownSql);
            _db.MarkMigrationRolledBack(last.Id);

            return last;
        }

        public object GetStatus()
        {
            _db.EnsureMigrationsTable();

            var modelSnapshot = _snapshotBuilder.BuildFromAssembly(_modelsAssembly);
            var last = _db.GetLastMigration();

            ModelSnapshot dbSnapshot;
            if (last == null)
                dbSnapshot = new ModelSnapshot();
            else
                dbSnapshot = JsonSerializer.Deserialize<ModelSnapshot>(last.ModelSnapshotJson)
                             ?? new ModelSnapshot();

            var dbTables = dbSnapshot.Tables.Select(t => t.Name).OrderBy(x => x).ToList();
            var modelTables = modelSnapshot.Tables.Select(t => t.Name).OrderBy(x => x).ToList();

            var missingInDb = modelTables.Except(dbTables).ToList();
            var extraInDb = dbTables.Except(modelTables).ToList();

            var migrations = _db.GetAllMigrations();

            return new
            {
                schemaDiff = new
                {
                    missingInDb,
                    extraInDb
                },
                migrations = migrations.Select(m => new
                {
                    m.Id,
                    m.MigrationName,
                    m.AppliedAt
                }).ToList()
            };
        }

        public List<MigrationRecord> GetLog()
        {
            _db.EnsureMigrationsTable();
            return _db.GetAllMigrations();
        }
    }
}
