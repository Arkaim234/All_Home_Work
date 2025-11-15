# Система миграций БД с HTTP API

Курсовой/лабораторный проект по теме: **«Система миграций базы данных с HTTP API»**.  
Цель работы — реализовать собственную библиотеку миграций без использования ORM и управлять ими через простой HTTP-сервис на `HttpListener`.

Проект состоит из двух частей:

- **MigrationLib** — библиотека для работы с миграциями;
- **ConsoleApp** — консольный HTTP-сервис, который предоставляет JSON API.

---

## 1. Основная идея

Схема базы данных описывается обычными C#-классам с атрибутами:

```csharp
[Table("users")]
class User
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; }
}
```

Библиотека через рефлексию:

1. Сканирует сборку с моделями.
2. Строит **снимок схемы** (таблицы и колонки).
3. Сравнивает старый снимок (из БД) и новый (из моделей).
4. Генерирует SQL:
   - `up_sql` — как изменить схему до нового состояния;
   - `down_sql` — как откатить изменения назад.
5. Сохраняет миграцию в таблицу `_migrations`.

Все действия по применению/откату выполняются транзакционно.

Управление происходит через HTTP-API:

- `/migrate/create` — создать новую миграцию,
- `/migrate/apply` — применить последнюю,
- `/migrate/rollback` — откатить последнюю,
- `/migrate/status` — показать статус,
- `/migrate/log` — показать лог.

Ответы в формате JSON.

---

## 2. Возможности

Библиотека миграций умеет:

- строить структуру БД по C#-моделям;
- автоматически генерировать SQL:
  - создание/удаление таблиц;
  - добавление/удаление колонок;
  - смена типа колонки (`int` ↔ `string`);
  - смена первичного ключа (одна колонка на таблицу);
- применять миграции к PostgreSQL в транзакции;
- откатывать миграции с использованием `down_sql`;
- хранить историю миграций в таблице `_migrations`.

HTTP-сервис умеет:

- отдавать статус схемы и список миграций;
- создавать, применять и откатывать миграции через простые GET-запросы;
- отдавать все ответы в формате JSON.

---

## 3. Ограничения (по заданию)

- Поддерживаются только типы `int` и `string`.
- Нельзя использовать ORM (EF Core, Dapper и т.д.).
- SQL для таблиц и колонок **не пишется вручную** для каждой модели — генерируется автоматически.
- Нельзя пересоздавать всю базу при каждом запуске.
- Откат обязателен и должен использовать snapshot модели.
- HTTP-сервер реализован через `HttpListener`, API открытый (без ключей).
- Все операции изменения базы выполняются транзакционно.

---

## 4. Структура решения

```text
Solution
├─ MigrationLib/           # библиотека миграций
│  ├─ Schema.cs            # атрибуты, ColumnModel, TableModel, ModelSnapshot, MigrationRecord
│  ├─ ModelSnapshotBuilder.cs
│  ├─ SqlGenerator.cs      # ISqlGenerator, PostgresSqlGenerator
│  ├─ MigrationGenerator.cs
│  ├─ PostgresDatabaseAdapter.cs
│  └─ MigrationService.cs
│
└─ ConsoleApp/             # консольный HTTP-сервис
   ├─ Settings.cs
   ├─ Models/
   │   └─ User.cs          # пример модели: таблица users
   ├─ HttpServer.cs        # HttpListener + маршруты /migrate/*
   └─ Program.cs           # точка входа, поднятие сервера
```

---

## 5. Требования

- .NET 6/7 (или выше).
- PostgreSQL (проверялось на локальном инстансе).
- NuGet-пакеты:  
  - `Npgsql` (в проекте `MigrationLib`).

---

## 6. Настройка PostgreSQL

1. Установить PostgreSQL (если ещё не установлен).
2. Создать базу (пример):

   ```sql
   CREATE DATABASE testdb;
   ```

3. При желании создать отдельного пользователя:

   ```sql
   CREATE USER myuser WITH PASSWORD 'mypassword';
   GRANT ALL PRIVILEGES ON DATABASE testdb TO myuser;
   ```

4. В `ConsoleApp/Program.cs` прописать строку подключения:

   ```csharp
   string connectionString =
       "Host=localhost;Port=5432;Database=testdb;Username=postgres;Password=1234";
   ```

   Здесь нужно подставить свои реальные данные.

---

## 7. Запуск проекта

1. Собрать solution (Build).
2. Запустить проект `ConsoleApp` (консольное приложение).
3. В консоли появится информация:

   - адрес сервера, например: `http://localhost:1337/`;
   - список доступных маршрутов;
   - доступные консольные команды: `/start`, `/restart`, `/off`, `/stop`, `/help`.

4. Открыть браузер или Postman и вызывать HTTP-методы:

   - `GET http://localhost:1337/migrate/status`
   - `GET http://localhost:1337/migrate/create`
   - `GET http://localhost:1337/migrate/apply`
   - `GET http://localhost:1337/migrate/rollback`
   - `GET http://localhost:1337/migrate/log`

---

## 8. HTTP API

Все ответы — JSON.

### 8.1. `GET /migrate/create`

Создаёт новую миграцию, но **не применяет** её к базе.

Пример ответа:

```json
{
  "migration": "Migration_20251115123456",
  "status": "created"
}
```

### 8.2. `GET /migrate/apply`

Применяет **последнюю** миграцию, если она ещё не применена.

```json
{
  "migration": "Migration_20251115123456",
  "status": "applied"
}
```

Если миграций нет или `up_sql` пустой, возвращается JSON с `"error"`.

### 8.3. `GET /migrate/rollback`

Откатывает **последнюю применённую** миграцию (использует `down_sql`).

```json
{
  "migration": "Migration_20251115123456",
  "status": "rolled_back"
}
```

Если нет применённой миграции — вернётся ошибка.

### 8.4. `GET /migrate/status`

Показывает расхождение между моделями и последним сохранённым снапшотом, а также список миграций.

Пример:

```json
{
  "schemaDiff": {
    "missingInDb": [ "users" ],
    "extraInDb": []
  },
  "migrations": [
    {
      "id": 1,
      "migrationName": "Migration_20251115123456",
      "appliedAt": "2025-11-15T10:20:30"
    }
  ]
}
```

### 8.5. `GET /migrate/log`

Возвращает лог всех миграций (упрощённый вид):

```json
{
  "log": [
    {
      "id": 1,
      "migrationName": "Migration_20251115123456",
      "appliedAt": "2025-11-15T10:20:30"
    },
    {
      "id": 2,
      "migrationName": "Migration_20251115130500",
      "appliedAt": null
    }
  ]
}
```

---

## 9. Пример использования (типичный сценарий)

### 9.1. Первая миграция

1. Описываем модель:

   ```csharp
   [Table("users")]
   public class User
   {
       [PrimaryKey]
       public int Id { get; set; }

       [Column]
       public string Name { get; set; } = "";
   }
   ```

2. Собираем проект.
3. Вызов:

   - `GET /migrate/create`
   - `GET /migrate/apply`

В базе появляется таблица `users`. В таблице `_migrations` — запись с `up_sql`/`down_sql` и снапшотом.

---

### 9.2. Изменение модели: добавление колонки

Меняем модель:

```csharp
[Table("users")]
public class User
{
    [PrimaryKey]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = "";

    [Column]
    public int Age { get; set; }   // новая колонка
}
```

Дальше:

1. Build.
2. `GET /migrate/create` — генерируется миграция с `ALTER TABLE users ADD COLUMN age INTEGER;`.
3. `GET /migrate/apply` — в таблицу `users` добавляется колонка `age`.

---

### 9.3. Смена типа и первичного ключа

Например:

```csharp
[Table("users")]
public class User
{
    [Column]
    public int Id { get; set; }          // больше не PK

    [PrimaryKey]
    public string Name { get; set; } = "";   // PK по Name

    [Column]
    public string Age { get; set; } = "";    // смена типа с int на string
}
```

После сборки:

1. `GET /migrate/create` — генерируются:
   - `ALTER TABLE users DROP CONSTRAINT users_pkey;`
   - `ALTER TABLE users ADD PRIMARY KEY (name);`
   - `ALTER TABLE users ALTER COLUMN age TYPE TEXT USING age::TEXT;`
2. `GET /migrate/apply` — изменения применяются.
3. `GET /migrate/rollback` — всё откатывается обратно.

---

## 10. Типичные ошибки и их смысл

- `"Нет применённой миграции для отката"`  
  Вызывается `/migrate/rollback`, когда нет ни одной миграции с `applied_at != null`.

- `"UpSql пустой"`  
  Создана миграция без изменений (модель не менялась), в `up_sql` пустая строка.

- `"42P01: отношение "users" не существует"`  
  В БД руками дропнули таблицу `users`, но в `_migrations` она числится как существующая.  
  В таком случае проще всего на учебном проекте:
  ```sql
  DROP TABLE IF EXISTS users;
  DROP TABLE IF EXISTS _migrations;
  ```
  и начать миграции с нуля.

---

## 11. Вывод

В результате работы реализована простая библиотека для миграций PostgreSQL на C#, которая:

- описывает схему через C#-модели и атрибуты,
- автоматически генерирует SQL-миграции с поддержкой отката,
- хранит историю миграций в специальной таблице,
- управляется через HTTP-API на `HttpListener` с JSON-ответами.

Такой подход можно использовать как учебный пример того, как работают миграции “под капотом” без ORM.
