# AGENTS.md — инструкция агенту по проекту Snake

**Версия:** 2026-11-21. Составлено по фактическому коду и проверенной сборке (`dotnet build` — 0 ошибок).
**Главный принцип:** игра — это детерминированное ядро + сменные представления. Новое «красивое изображение» (Unity 2D, Unity 3D, Web) = **новый проект-представление**, а бизнес-логика при этом не меняется.

> Если код расходится с документом: раздел 2 описывает «как есть», раздел 4 — «как должно быть». Новый код пишется по разделу 4. Расхождение упомяни в ответе, но не «подстраивай» документ под старый код.

---

## 0. Быстрые факты

| Факт | Значение |
|---|---|
| TFM | `netcoreapp3.1` (SDK 3.1.426 установлен) |
| Сборка | `dotnet build Snake.sln` — 0 ошибок (есть предупреждения NU1504 / NU1903 / NETSDK1138) |
| Проектов | 5 (`Domain`, `Core`, `Application`, `Persistence`, `Console`) |
| Тестовых проектов | **нет** |
| `global.json` / `Directory.Build.props` | **нет** |
| БД | PostgreSQL 18, локальный кластер в `Data/pgdata`, `127.0.0.1:5433`, база `snake` |
| Git | `main`, история `TASK-1 … TASK-5` |

**CQRS в проекте НЕТ.** Нет `ICommand`, `IQuery`, `Handler`, `Result<T>`, диспетчера. MediatR/AutoMapper/FluentValidation подключены пакетами, но игрой не используются — это остатки шаблона. Не выдумывай CQRS-структуру и не создавай `Commands/`, `Queries/`, `Handlers/`.

---

## 1. Что такое эта игра

Ядро — `Snake.Application.Game.GameEngine`: пошаговая логика на целочисленной сетке.
- Детерминирована при заданном `Random`.
- Один шаг = один вызов `GameEngine.Tick()`.
- Состояние: змейка (`SnakeBody`), еда (`Point?`), препятствия (`Board`), счёт, уровень, скорость.
- Не знает ни про консоль, ни про время, ни про ввод — но **сейчас частично знает** (см. раздел 3).

Представление — всё, что превращает состояние в картинку и даёт ввод: консоль сегодня, Unity 2D/3D — в будущем.

---

## 2. Что реально лежит сейчас (факты)

### 2.1 Проекты

| Проект | Зависит от | Роль сейчас | Что внутри |
|---|---|---|---|
| `Snake.Domain` | — | модель | `Point`, `Board : BaseBoard`, `BaseSnakeBody`, `GameSettings : BaseGameSettings`, `Enum/{Direction, GameState, TickResult, GameCommand}`. Мусор шаблона: `Entity`, `BaseEntity`, `JwtToken`, `Exceptions/*` |
| `Snake.Core` | `Domain` | утилиты | `Enum/DirectionExtensions.cs` (`Delta`/`Opposite`/`IsOpposite`) — **реально нужен игре**; остальное шаблон: `Sort/*`, `Search/*`, `Urls/*`, `Xml/*`, `Param/*`, `Pager/*`, `Repositories/*`, `Texts/*`. Тянет `Microsoft.AspNetCore.Http` |
| `Snake.Application` | `Domain`, `Core` | логика **+ консоль** | `Game/GameEngine`, `Game/SnakeBody`, `Game/SimpleBot`, **`Game/GameApp`** (игровой цикл + запись результата), `Game/DemoRunner`, `Game/SelfTest`, **`Renderer/ConsoleRenderer`**, **`Keys/ConsoleInput`**, `Abstractions/*` (порты результатов); шаблон: MediatR/AutoMapper/`ISnakeDbContext` (EF) |
| `Snake.Persistence` | `Application` | БД | `SnakeDbContext` (+ `DbSet<GameResultEntity>`), `Repositories/GameResultRepository` (запись/чтение результатов), `DbInitializer` (схема). Остальное шаблон: `DbConnector` (Dapper, строка `"As"`), generic `CommandRepository`/`QueryRepository` |
| `Snake.Console` | `Application`, `Persistence` | точка входа | `Program.cs` (конфиг, логгер, режимы `--selftest` / `--demo` / `--stats` / `--help`, composition root), `ScoreboardView` (экран прогресса), `FileLogger*` |

### 2.2 Фактический граф зависимостей (стрелка = «зависит от»)

```
Snake.Domain ◄── Snake.Core ◄── Snake.Application ◄── Snake.Persistence
                                      ▲                    ▲
                                      └── Snake.Console ───┘
```

### 2.3 Ключевые типы и где они живут

| Тип | Файл | Заметки |
|---|---|---|
| `Point` | `Snake.Domain/Point.cs` | `readonly struct`, **Y растёт вниз** (консольная сетка) |
| `Board` / `BaseBoard` | `Snake.Domain/` | препятствия в `HashSet<Point>`; `IsInside`, `IsObstacle`, `AddObstacle`, `AllCells` |
| `GameSettings` | `Snake.Domain/Base/BaseGameSettings.cs` | `BoardWidth`, `BoardHeight`, `InitialLength`, `BaseFrameDelayMs`, `MinFrameDelayMs`, `SpeedStepMs`, `FoodPerLevel`, `ScorePerFood`, `ObstaclesPerLevel`, `MaxObstacles`, `Validate()` |
| `GameEngine` | `Snake.Application/Game/GameEngine.cs` | `Start`, `Tick`, `TryTurn`, `TogglePause`, `IsMoveSafe`; свойства `Board`, `Snake`, `Head`, `Food`, `Score`, `Level`, `Length`, `FrameDelayMs`, `CellsPerSecond`, `State`, `StatusMessage`, `Direction` |
| `SnakeBody` | `Snake.Application/Game/SnakeBody.cs` | `: BaseSnakeBody`, `Segments[0]` — голова |
| `GameApp` | `Snake.Application/Game/GameApp.cs` | **игровой цикл**: `ConsoleInput.Poll()` + `Tick()` по `Stopwatch` + `ConsoleRenderer.Draw()`; после `Tick()` вызывает `SaveFinishedRun()` → `IGameResultSink` (один раз на партию) |
| `ConsoleRenderer` | `Snake.Application/Renderer/ConsoleRenderer.cs` | псевдографика (`╭╮╰╯│─`) + 24-битные ANSI-цвета (`\u001b[38;2;r;g;bm`); градиент тела, пульс еды, первый кадр очищает экран; `RenderText()` — тот же кадр без цвета для демо |
| `AnsiConsole` | `Snake.Console/AnsiConsole.cs` | включает `ENABLE_VIRTUAL_TERMINAL_PROCESSING` в Windows; возвращает `false` — рендер идёт без цвета |
| `ConsoleInput` | `Snake.Application/Keys/ConsoleInput.cs` | статический, `Console.ReadKey(true)`, разбор escape-последовательностей |
| `SimpleBot` | `Snake.Application/Game/SimpleBot.cs` | жадный бот, нужен `--demo` |
| `GameCommand` | `Snake.Domain/Enum/GameCommand.cs` | `None, Up, Down, Left, Right, Pause, Restart, Quit` |
| `GameRunResult` | `Snake.Application/Abstractions/GameRunResult.cs` | итог партии: `FinishedAtUtc`, `Score`, `Length`, `Level`, `Eaten`, `DurationMs`, `BoardWidth/Height`, `Outcome`, `StatusMessage` |
| `IGameResultSink` / `IGameResultReader` | `Snake.Application/Abstractions/` | порты записи и чтения результатов |
| `GameProgress` | `Snake.Application/Abstractions/GameProgress.cs` | сводка: `Games`, `BestScore`, `AverageScore`, `BestLength`, `BestLevel`, `TotalEaten`, `Last` |
| `GameResultRepository` | `Snake.Persistence/Repositories/GameResultRepository.cs` | EF Core + Npgsql; `EnsureSchema`, `Save`, `GetProgress`, `GetBest`, `GetRecent` |

### 2.4 Данные в PostgreSQL

| Что | Где |
|---|---|
| Кластер | `Data/pgdata` (локальный, **не** Windows-служба), `127.0.0.1:5433` |
| База / пользователь | `snake` / `snake`, пароль `snake_dev_pw` — только для локальной разработки |
| Строка подключения | `Snake.Console/appsettings.json` → `ConnectionStrings:Snake` |
| Управление | `Data\db.cmd start` / `status` / `psql` / `stop` (обёртка над `db.ps1`) |
| Подробности | `Data/README.md` |
| Таблица | `game_results` (`id`, `finished_at_utc` timestamptz, `score`, `length`, `level`, `eaten`, `duration_ms`, `board_width`, `board_height`, `outcome`, `status_message`) |
| Индексы | `ix_game_results_score (score DESC)`, `ix_game_results_finished_at (finished_at_utc DESC)` |

- Схема создаётся при старте игры: `DbInitializer` → `EnsureCreated()` + идемпотентный DDL в `GameResultRepository.EnsureSchema()`.
- **Миграций нет.** Меняя схему, правь **и** модель `SnakeDbContext`, **и** DDL в `EnsureSchema()`.
- Если БД недоступна, игра работает, но результаты не пишутся (в консоли будет `[БД] PostgreSQL недоступна…`).

---

## 3. Блокеры: почему Unity 2D/3D сейчас не подключить

| # | Факт | Где | Почему мешает |
|---|---|---|---|
| 1 | Игровой цикл лежит в ядре | `Application/Game/GameApp.cs` (`Run`, `Finish`) | Unity не переиспользует цикл: он читает консоль, спит `Thread.Sleep`, пишет в `Console` |
| 2 | Рендер и ввод лежат в ядре | `Application/Renderer/ConsoleRenderer.cs`, `Application/Keys/ConsoleInput.cs` | слой логики зависит от консоли; Unity был бы вынужден тянуть `System.Console` |
| 3 | Нет порта отрисовки и снимка состояния | — | есть только порты результатов; `IGameView`/`GameSnapshot`/`GameSession` ещё не созданы — презентеру пришлось бы лезть во внутренности `GameEngine` |
| 4 | Composition root не выделен | `Console/Program.cs` | `GameEngine` / `ConsoleRenderer` / `GameApp` создаются вручную; DI используется только для БД и логов |
| 5 | `Domain` прибит к Linux | `Snake.Domain.csproj:5-6` | `RuntimeIdentifier=linux-x64` + `RuntimeFrameworkVersion=3.1.3` — проект не переиспользуется на Windows и в Unity |
| 6 | Ядро тянет лишние пакеты и старый TFM | `Application.csproj:8-13`, `Core.csproj:8` | EF Core, MediatR, AutoMapper, FluentValidation, `Microsoft.AspNetCore.Http`; `netcoreapp3.1` не совместим с Unity (.NET Standard 2.1) |
| 7 | Принудительные сеттеры `internal` | `GameEngine.cs` (`ForceFood` / `ForceSnake` / `ForceState`) | доступны только внутри `Snake.Application` — тесты и боты снаружи не могут подготовить сценарий |
| 8 | Нет тестового проекта | — | проверка только через `--selftest` (CLI-режим, а не тест-фреймворк) |

---

## 4. Целевое устройство

### 4.1 Правило зависимостей (жёсткое)

```
Snake.Domain ◄── Snake.Core ◄── Snake.Application
                                      ▲
            ┌─────────────┬───────────┼───────────┬─────────────┐
        Console       Unity2D     Unity3D        Web          ...
```

- Ядро (`Domain`, `Core`, `Application`) **не знает** про UI, консоль, Unity и время процесса.
- Ядро **не ссылается** на EF Core, Npgsql, `Snake.Persistence`; о хранилище оно знает только через порты из `Application/Abstractions`.
- Ядро — **только BCL**: без `Console`, `Thread.Sleep`, `Stopwatch`, без Unity-типов, без EF/MediatR.
- Презентеры зависят от `Application` (+ `Persistence`, если показывают/пишут результаты); ядро **никогда** не зависит от презентеров.
- Презентеры **не знают друг о друге** и не ссылаются друг на друга.
- `Persistence` — необязательный слой; его подключает composition root презентера/сервиса.

### 4.2 Порт вывода и снимок состояния (ещё не создан)

```csharp
// Snake.Application/Abstractions/GameSnapshot.cs
public sealed class GameSnapshot
{
    public int BoardWidth { get; }
    public int BoardHeight { get; }
    public GameState State { get; }
    public string StatusMessage { get; }
    public Direction Direction { get; }
    public Point Head { get; }
    public IReadOnlyList<Point> Body { get; }        // [0] — голова
    public Point? Food { get; }
    public IReadOnlyList<Point> Obstacles { get; }
    public int Score { get; }
    public int Level { get; }
    public int Length { get; }
    public int FrameDelayMs { get; }                 // как часто презентеру вызывать Advance
}
```

```csharp
// Snake.Application/Abstractions/IGameView.cs
public interface IGameView
{
    void Render(GameSnapshot snapshot);
}
```

Правила:
- Презентер получает **только** `GameSnapshot`. Ссылку на `GameEngine` ему не дают.
- В `GameSnapshot` нет типов Unity, `ConsoleColor`, спрайтов, `Transform`.
- `Render` не блокирует поток (в Unity — только обновление объектов сцены, без `Thread.Sleep`).
- Если для картинки не хватает данных — **добавь поле в `GameSnapshot`**, не открывай движок наружу.

### 4.3 `GameSession` — единственная точка входа для представлений (ещё не создан)

```csharp
// Snake.Application/GameSession.cs
public sealed class GameSession
{
    public GameSession(GameSettings settings, Random random);
    public GameSnapshot Snapshot { get; }
    public TickResult Start();
    public bool Enqueue(GameCommand command);   // поворот / пауза / рестарт / выход
    public TickResult Advance();                // ровно один тик
    public bool IsOver { get; }
}
```

- **Время — забота презентера.** `Advance()` вызывается, когда презентер решил, что пришёл кадр. Ядро никогда не спит и не измеряет время.
- Консоль: `Stopwatch` + `Thread.Sleep`. Unity: накопление `Time.deltaTime` в `Update()`. Web: таймер.
- `GameSession` не потокобезопасен: один презентер = один поток.

### 4.4 Карта файлов (цель)

```
Snake.Domain/
  Point.cs, Board.cs, Base/BaseBoard.cs
  GameSettings.cs, Base/BaseGameSettings.cs
  Base/BaseSnakeBody.cs
  Enum/{Direction,GameState,TickResult,GameCommand}.cs
Snake.Core/
  Enum/DirectionExtensions.cs          (Delta / Opposite / IsOpposite)
  ...чистые утилиты без Http и БД
Snake.Application/
  Game/GameEngine.cs                   логика
  Game/SnakeBody.cs
  Game/SimpleBot.cs
  GameSession.cs                       фасад для презентеров (создать)
  Abstractions/GameSnapshot.cs         (создать)
  Abstractions/IGameView.cs            (создать)
  Abstractions/GameRunResult.cs        есть
  Abstractions/IGameResultSink.cs      есть
  Abstractions/IGameResultReader.cs    есть
  Abstractions/GameProgress.cs         есть
Snake.Persistence/
  Data/SnakeDbContext.cs
  Data/Entities/GameResultEntity.cs
  Repositories/GameResultRepository.cs
  DbInitializer/DbInitializer.cs
Snake.Console/
  Program.cs                           режимы, конфиг, composition root
  ConsoleInput.cs                      (перенести из Application)
  ConsoleRenderer.cs                   (перенести из Application)
  ConsoleGameLoop.cs                   (перенести GameApp из Application)
  ScoreboardView.cs
  AnsiConsole.cs                       включение VT / 24-битных цветов
  FileLogger*.cs
Data/
  pgdata/                              кластер PostgreSQL (в git не попадает)
  db.cmd                               запуск db.ps1 без изменения политики
  db.ps1                              управление кластером
  README.md
Snake.Unity2D/   (Unity-проект, вне Snake.sln)
  Assets/Scripts/Snake2DGameView.cs
  Assets/Scripts/Snake2DInput.cs
Snake.Unity3D/   (Unity-проект, вне Snake.sln)
  Assets/Scripts/Snake3DGameView.cs
  Assets/Scripts/Snake3DInput.cs
```

### 4.5 Порты результатов (уже реализованы)

```csharp
// Snake.Application/Abstractions/
public sealed class GameRunResult
{
    public DateTime FinishedAtUtc { get; set; }
    public int Score { get; set; }
    public int Length { get; set; }
    public int Level { get; set; }
    public int Eaten { get; set; }
    public int DurationMs { get; set; }
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    public GameState Outcome { get; set; }      // GameOver | Won
    public string StatusMessage { get; set; }
}

public interface IGameResultSink
{
    void Save(GameRunResult result);
}

public interface IGameResultReader
{
    GameProgress GetProgress();
    IReadOnlyList<GameRunResult> GetBest(int count);
    IReadOnlyList<GameRunResult> GetRecent(int count);
}

public sealed class GameProgress
{
    public int Games, BestScore, BestLength, BestLevel, TotalEaten;
    public double AverageScore;
    public DateTime? FirstPlayedUtc;
    public GameRunResult Last;
    public bool HasGames { get; }
}
```

Правила:
- В ядре — **только** интерфейсы и DTO на BCL/Domain-типах.
- Реализация — `Snake.Persistence/Repositories/GameResultRepository.cs` (EF Core + Npgsql), контекст создаётся на операцию.
- Презентер подставляет реализацию в composition root; ядро про PostgreSQL не знает.
- Запись выполняется **в момент окончания партии** (`GameOver` / `Won`), ровно один раз (`GameApp.SaveFinishedRun`).
- Ошибка БД **не роняет игру**: `GameApp` ловит исключение, `GameResultRepository` пишет его в лог.
- `GameApp` принимает `initialHighScore` — HUD показывает рекорд из БД.

---

## 5. Как добавить новое представление

### 5.0 Фаза 0 — обязательная подготовка (делается один раз)

Без неё Unity не подключить. Порядок:

1. Перенести `Application/Renderer/ConsoleRenderer.cs` → `Snake.Console/ConsoleRenderer.cs`.
2. Перенести `Application/Keys/ConsoleInput.cs` → `Snake.Console/ConsoleInput.cs`.
3. Перенести `Application/Game/GameApp.cs` → `Snake.Console/ConsoleGameLoop.cs` (переименовать класс).
4. Перенести `Application/Game/DemoRunner.cs` и `Application/Game/SelfTest.cs` → `Snake.Console/` (или в отдельный `*.Tests`, см. п. 8).
5. Создать `Application/Abstractions/GameSnapshot.cs`, `Application/Abstractions/IGameView.cs`, `Application/GameSession.cs`; `GameEngine` отдаёт снимок.
6. Убедиться: в `Domain`/`Core`/`Application` нет `System.Console`, `Thread.Sleep`, `Stopwatch`, Unity-типов.
7. Перевести `Domain`, `Core`, `Application` на `netstandard2.1`; из `Snake.Domain.csproj` убрать `RuntimeIdentifier` и `RuntimeFrameworkVersion`.
8. Вынести из `Application` EF Core / MediatR / AutoMapper / FluentValidation / `ISnakeDbContext` в отдельный Infrastructure-проект (или в `Persistence`); из `Core` убрать `Microsoft.AspNetCore.Http` и `Urls/Base/BaseUrlManager.cs`.
9. Собирать `GameSession` в одном месте (composition root презентера).

**Разово для консоли:** `Program.cs` создаёт `GameSession` и подставляет `IGameResultSink`/`IGameResultReader`, `ConsoleGameLoop` реализует цикл через `IGameView`.
**Проверка после фазы 0:** `dotnet build Snake.sln`, `--selftest` и `--demo` дают тот же результат, что до рефакторинга.

### 5.1 Консоль (существует)

`Snake.Console` — эталонный презентер: игровой цикл + `ConsoleRenderer` + `ConsoleInput` + `ScoreboardView`.
Консольный рендер рисует **полный кадр** без `Console.Clear()` (чтобы не мерцало).

### 5.2 Unity 2D

1. Создать Unity-проект `Snake.Unity2D` (2D-шаблон, URP или Built-in).
2. Подключить ядро: `netstandard2.1`-сборки `Snake.Domain/Core/Application` в `Assets/Plugins/Snake/` + `.asmdef`, либо скопировать исходники в `Assets/Scripts/Snake/`.
3. `Snake2DGameView : MonoBehaviour, IGameView` — рисует по `GameSnapshot`:
   - голова и тело — отдельные спрайты, направление головы по `Direction`;
   - еда — пульсирующий спрайт; препятствия — тайлы;
   - HUD: очки, уровень, длина, скорость.
4. `Snake2DInput` — в `Update()` читает `Input.GetKeyDown` и вызывает `session.Enqueue(GameCommand.Up/…/Pause/Restart)`.
5. Цикл в `Update()`: копить `Time.deltaTime`; когда накопленное ≥ `Snapshot.FrameDelayMs / 1000f` → `session.Advance()` → `view.Render(session.Snapshot)`.
6. **Y-инверсия:** в ядре `Y` растёт **вниз**, в Unity — вверх. Преобразование: `worldY = -cellY` (и/или масштаб `cellSize`). Делает только презентер.
7. Перезапуск: `session.Enqueue(GameCommand.Restart)`; выход — `Quit` (в редакторе `Application.Quit()` только в билде).
8. Рекорды: `IGameResultSink` передаётся в `GameSession`/презентер, реализация берётся из `Snake.Persistence` — **Unity не должна тянуть EF Core в рантайм**, поэтому либо тонкий HTTP-клиент к тому же хранилищу, либо локальный JSON-адаптер порта.

### 5.3 Unity 3D

1. Создать `Snake.Unity3D` (3D-шаблон).
2. Ядро подключается так же, как в 5.2 — **тот же самый** `GameSession`, без изменений.
3. `Snake3DGameView : MonoBehaviour, IGameView` — инстансы префабов: голова, сегменты, еда (сфера с анимацией), препятствия (кубы).
4. Пул объектов: переиспользовать GameObject'ы сегментов, не создавать/уничтожать каждый кадр (иначе GC и просадки).
5. Камера: ортографическая сверху или изометрия; сетка маппится `cellX → x`, `cellY → -z` (или `-y` для 2.5D).
6. Логика, счёт, победа/поражение — только из ядра.

### 5.4 Unity-специфика (важно)

- **TFM:** `netstandard2.1` — иначе Unity не подключит сборки.
- **Unity-проекты не добавлять в `Snake.sln`**: `dotnet build` их не соберёт и сломает CI.
- **Никаких `Thread.Sleep`, `Console`, `Stopwatch` в ядре** — в Play Mode они блокируют главный поток.
- **Один `GameSession` на сцену**, всё в главном потоке.
- **Скорость** берётся из `Snapshot.FrameDelayMs`, не хардкодится в презентере.
- **2D и 3D используют одни и те же `Domain` + `Application`**; различается только `IGameView` и ввод.
- **PostgreSQL прямо из Unity не использовать** — EF Core/Npgsql в рантайме Unity не место; результаты — через порт и отдельный адаптер.

### 5.5 Definition of Done для нового представления

- [ ] Игрок двигается, ест, умирает, ставит паузу, перезапускает.
- [ ] Отображаются очки, уровень, длина, скорость, статус (пауза / конец / победа).
- [ ] `Domain` и `GameEngine` **не изменены**; если пришлось менять — это ошибка дизайна, сообщи о ней.
- [ ] Новый проект не ссылается на другие презентеры и не добавляет пакетов в ядро.
- [ ] `dotnet build Snake.sln` зелёный, `--selftest` проходит.

---

## 6. Жёсткие запреты

1. **Не** добавлять в `Domain`/`Core`/`Application` ссылки на `System.Console`, Unity, EF Core, Npgsql, MediatR, AutoMapper, FluentValidation, ASP.NET Core. Порты к инфраструктуре (интерфейсы + BCL/Domain-DTO) в `Application/Abstractions` — **можно**; реализация — только в `Persistence`.
2. **Не** класть отрисовку, ввод или таймер в `Application` — только в проект-презентер.
3. **Не** протаскивать в `GameSnapshot` и в ядро платформенные типы (`Transform`, `Sprite`, `ConsoleColor`, `GameObject`).
4. **Не** давать презентеру ссылку на `GameEngine` — только `GameSession` + `GameSnapshot`.
5. **Не** добавлять в `GameEngine` поля и ветки вида «если это Unity / если это консоль».
6. **Не** создавать новые проекты без явного запроса.
7. **Не** «оптимизировать» архитектуру без запроса: не сливать `Direction` и `GameCommand`, не переносить правила игры в презентер, не заменять `Tick()` на событийную модель.
8. **Не** добавлять CQRS-структуру (`Commands/`, `Queries/`, `Handlers/`, `Result<T>`) — её в проекте нет и она здесь не нужна.
9. **Не** тащить `Persistence` в Unity/Web-презентер напрямую; результаты — через порт.
10. **Не** обращаться к БД из ядра напрямую — только через `IGameResultSink` / `IGameResultReader`.
11. **Не** менять схему только в модели или только в DDL — правь оба места (миграций нет).
12. Любая «обратная» ссылка (презентер → ядро → презентер) — признак ошибки дизайна: сообщи, не обходи.

---

## 7. Соглашения об именовании

| Что | Шаблон | Пример |
|---|---|---|
| Проект-представление | `Snake.<Платформа>` | `Snake.Console`, `Snake.Unity2D`, `Snake.Unity3D` |
| Отрисовка | `<Платформа>GameView : IGameView` | `ConsoleRenderer`, `Snake2DGameView` |
| Ввод | `<Платформа>Input` | `ConsoleInput`, `Snake2DInput` |
| Игровой цикл | `<Платформа>GameLoop` | `ConsoleGameLoop` |
| Ядро | `GameEngine`, `GameSession`, `GameSnapshot`, `IGameView` | — |
| Результаты | `<Что>Result`, `I<Что>Sink`, `I<Что>Reader` | `GameRunResult`, `IGameResultSink` |
| Enum'ы | в `Snake.Domain/Enum` | `Direction`, `TickResult` |
| Async-методы | суффикс `Async` + `CancellationToken ct = default` | `SaveScoreAsync` |

В именах публичных типов ядра **не должно** быть слов `Console`, `Unity`, `Sprite`, `Transform`, `Camera`, `Window`.

---

## 8. Проверка результата

```powershell
Data\db.cmd start                                       # поднять PostgreSQL (127.0.0.1:5433)
dotnet build Snake.sln                                  # 0 ошибок
dotnet run --project Snake.Console -- --stats           # прогресс / лучшие / последние из БД
dotnet run --project Snake.Console -- --selftest        # 25 проверок логики
dotnet run --project Snake.Console -- --demo 300 42     # бот играет 300 тиков
dotnet run --project Snake.Console                      # играть
```

- `--stats` печатает сводку; при первом запуске схема создаётся автоматически.
- При интерактивном запуске сначала печатается экран прогресса и ждётся любая клавиша; первый кадр игры вызывает `Console.Clear()`, поэтому «хвостов» от сводки не остаётся. Если партий ещё нет — паузы нет.
- Поле считается по фактическому окну консоли (окно **не** меняем): `width = min(120, (WindowWidth - 3) / 2)`, `height = min(50, WindowHeight - 9)`. Запас в одну колонку и одну строку обязателен: строка ровно в ширину окна вызывает перенос, а запись в последнюю строку — прокрутку, и кадр «съезжает».
- Размер окна можно восстановить из лога: при старте пишется строка `Консоль: {Cols}x{Rows}, поле: {Width}x{Height}`.
- Графика: клетка = **2 символа** (`width = (cols - 3) / 2`). Голова `█`, тело `▓` с градиентом, еда `●` (меняет оттенок каждый тик), препятствие `▒`, рамка `╭╮╰╯│─`. Цвета 24-битные; если `AnsiConsole.TryEnable()` вернул `false` — тот же кадр рисуется без escape-кодов. Каждая строка кадра ровно `FrameWidth` видимых символов (проверено на `--demo`).
- Если PostgreSQL не запущен, игра продолжает работать: выводится `[БД] PostgreSQL недоступна…`, результаты не сохраняются.
- `--selftest` и `--demo` работают и при перенаправленном выводе: консольные вызовы курсора выполняются только в интерактивном режиме.
- Unity: Play Mode, 60 fps, ввод, рестарт, пустая консоль ошибок.
- Тесты: доменная логика проверяется в отдельном `*.Tests`-проекте **без** консоли и БД (`GameSession` создаётся напрямую).

---

## 9. Формат ответа агента

1. Слой/слои, которые затрагивает изменение.
2. Файлы полными путями, с пометкой: создан / изменён / удалён.
3. Граф зависимостей — если появился новый тип, интерфейс или проект.
4. Что проверено: команды и их результат.
5. Явная проверка: «правила разделов 4.1 и 6 не нарушены».

---

## 10. Техдолг шаблона

Не развивать. При касании этих файлов — предложить удаление или вынос, а не копировать подход.

| Что | Где | Действие |
|---|---|---|
| `Entity`, `BaseEntity`, `JwtToken`, `Exceptions/*` | `Snake.Domain` | игра не использует — удалить при касании |
| `Sort/*`, `Search/*`, `Urls/*`, `Xml/*`, `Param/*`, `Pager/*`, `Texts/*`, `Repositories/*` | `Snake.Core` | вынести из ядра; `BaseUrlManager` тянет ASP.NET Core |
| MediatR-бихейворы, `IMapWith`/`AssemblyMappingProfile`, `ApiWorker`, `UserTestJson`, `FileViewModel` | `Snake.Application` | не подключены к игре — удалить или вынести в Infrastructure |
| `ISnakeDbContext` | `Snake.Application/Interfaces` | EF в ядре — перенести в `Persistence` |
| `DbConnector`, generic `CommandRepository`/`QueryRepository`, `EntityExtensions` | `Snake.Persistence` | не используются; `DbConnector` читает строку `"As"` вместо `"Snake"` |
| Дубли `PackageReference` `Microsoft.EntityFrameworkCore` | `Snake.Application.csproj:8,14` | предупреждение NU1504 — удалить дубль |
| `AutoMapper 11.0.1` | `Snake.Application.csproj` | известные уязвимости (NU1903) |
| `RuntimeIdentifier=linux-x64`, `RuntimeFrameworkVersion=3.1.3` | `Snake.Domain.csproj` | ломает переиспользование; убрать вместе с переходом на `netstandard2.1` |
| `Microsoft.EntityFrameworkCore.Tools` | удалён из `Snake.Persistence.csproj` | был источником рассинхрона версий; вместо него явный `Microsoft.EntityFrameworkCore.Relational` |
| Нет `global.json` | корень | SDK закреплён неявно; при добавлении тестов — зафиксировать |
