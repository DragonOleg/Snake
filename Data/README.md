# Data — локальная база PostgreSQL

Здесь лежит **кластер PostgreSQL 18** проекта Snake: каталог `pgdata/`.
Кластер локальный (не Windows-служба), чтобы не конфликтовать с системным сервером на порту 5432,
и слушает только `127.0.0.1`.

| Параметр | Значение |
|---|---|
| Хост / порт | `127.0.0.1:5433` |
| База | `snake` |
| Пользователь / пароль | `snake` / `snake_dev_pw` |
| Строка подключения | `Snake.Console/appsettings.json` → `ConnectionStrings:Snake` |
| Таблица результатов | `game_results` |

## Управление

```powershell
Data\db.cmd start      # запустить кластер
Data\db.cmd status     # проверить и посчитать партии
Data\db.cmd psql       # psql внутрь базы
Data\db.cmd stop       # остановить
```

`db.cmd` — тонкая обёртка над `db.ps1` с `-ExecutionPolicy Bypass`, потому что на этой машине
политика выполнения — `Restricted` и прямой запуск `db.ps1` блокируется.
Если политика позволяет, можно вызывать и напрямую: `powershell -File .\Data\db.ps1 status`.

Каталог `pgdata/` в git не попадает (см. `.gitignore`).

## Схема

Таблица `game_results` создаётся автоматически при запуске игры:
`DbInitializer` вызывает `EnsureCreated()`, затем `GameResultRepository.EnsureSchema()`
выполняет идемпотентный DDL (`CREATE TABLE IF NOT EXISTS` + индексы).
Миграций нет, поэтому, меняя схему, правь **и** модель в `Snake.Persistence/Data/SnakeDbContext.cs`, **и** DDL
в `GameResultRepository`.

| Колонка | Тип | Смысл |
|---|---|---|
| `id` | bigint identity | PK |
| `finished_at_utc` | timestamptz | когда закончилась партия (UTC) |
| `score` | int | очки |
| `length` | int | длина змейки |
| `level` | int | уровень |
| `eaten` | int | сколько еды съедено |
| `duration_ms` | int | длительность партии |
| `board_width` / `board_height` | int | размер поля |
| `outcome` | text | `GameOver` или `Won` |
| `status_message` | text | причина («Удар о стену» и т.п.) |

## Если кластер нужно создать заново

```powershell
$bin = 'C:\Program Files\PostgreSQL\18\bin'
Set-Content -Path .\Data\.pwpw -Value 'snake_dev_pw' -NoNewline -Encoding ascii
& "$bin\initdb.exe" -D .\Data\pgdata -U snake -A scram-sha-256 --pwfile=.\Data\.pwpw -E UTF8 --locale=C
Remove-Item .\Data\.pwpw
Add-Content -Path .\Data\pgdata\postgresql.conf -Value "`nport = 5433`nlisten_addresses = '127.0.0.1'`n"
Data\db.cmd start
$env:PGPASSWORD='snake_dev_pw'
& "$bin\createdb.exe" -h 127.0.0.1 -p 5433 -U snake snake
Remove-Item Env:\PGPASSWORD
```

Пароль `snake_dev_pw` — только для локальной разработки; для чего-то большего вынесите его
в переменную окружения и не храните в `appsettings.json`.
