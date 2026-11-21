# AGENTS.md — инструкция агенту по проекту Snake

**Версия:** 2026-11-21. Составлено по фактическому коду и проверенной сборке (`dotnet build` — успешно).
**Главный принцип:** игра — это детерминированное ядро + сменные представления. Новое «красивое изображение» (Unity 2D, Unity 3D, Web) = **новый проект-представление**, а бизнес-логика при этом не меняется.

> Если код расходится с документом: раздел 2 описывает «как есть», раздел 4 — «как должно быть». Новый код пишется по разделу 4. Расхождение упомяни в ответе, но не «подстраивай» документ под старый код.

---

## 0. Быстрые факты

| Факт | Значение |
|---|---|
| TFM | `netcoreapp3.1` (SDK 3.1.426 установлен) |
| Сборка | `dotnet build Snake.sln` — 0 ошибок, 11 предупреждений |
| Проектов | 5 (`Domain`, `Core`, `Application`, `Persistence`, `Console`) |
| Тестовых проектов | **нет** |
| `global.json` / `Directory.Build.props` | **нет** |
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
| `Snake.Application` | `Domain`, `Core` | логика **+ консоль** | `Game/GameEngine`, `Game/SnakeBody`, `Game/SimpleBot`, **`Game/GameApp`** (игровой цикл), `Game/DemoRunner`, `Game/SelfTest`, **`Renderer/ConsoleRenderer`**, **`Keys/ConsoleInput`**; шаблон: MediatR/AutoMapper/`ISnakeDbContext` (EF) |
| `Snake.Persistence` | `Application` | БД | `SnakeDbContext` (Npgsql), `DbConnector` (Dapper, строка `"As"`), generic `CommandRepository`/`QueryRepository`, `DbInitializer` (пустой). **Игрой не используется** — регистрация в `Program.cs` закомментирована |
| `Snake.Console` | `Application`, `Persistence` | точка входа | `Program.cs` (конфиг, логгер, режимы `--selftest` / `--demo` / `--help`, сборка игры вручную), `FileLogger*` |

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
| `GameApp` | `Snake.Application/Game/GameApp.cs` | **игровой цикл**: `ConsoleInput.Poll()` + `Tick()` по `Stopwatch` + `ConsoleRenderer.Draw()` + `Console.*` в `Finish()` |
| `ConsoleRenderer` | `Snake.Application/Renderer/ConsoleRenderer.cs` | `Console.SetCursorPosition/ForegroundColor`; `RenderText()` для неинтерактивного вывода |
| `ConsoleInput` | `Snake.Application/Keys/ConsoleInput.cs` | статический, `Console.ReadKey(true)`, разбор escape-последовательностей |
| `SimpleBot` | `Snake.Application/Game/SimpleBot.cs` | жадный бот, нужен `--demo` |
| `GameCommand` | `Snake.Domain/Enum/GameCommand.cs` | `None, Up, Down, Left, Right, Pause, Restart, Quit` |

---

## 3. Блокеры: почему Unity 2D/3D сейчас не подключить

| # | Факт | Где | Почему мешает |
|---|---|---|---|
| 1 | Игровой цикл лежит в ядре | `Application/Game/GameApp.cs:10,15,30,59,78` | Unity не переиспользует цикл: он читает консоль, спит `Thread.Sleep`, пишет в `Console` |
| 2 | Рендер и ввод лежат в ядре | `Application/Renderer/ConsoleRenderer.cs`, `Application/Keys/ConsoleInput.cs` | слой логики зависит от консоли; Unity был бы вынужден тянуть `System.Console` |
| 3 | Нет контракта представления | — | нет ни интерфейса отрисовки, ни снимка состояния; презентеру придётся лезть во внутренности `GameEngine` |
| 4 | Сборка игры руками в точке входа | `Console/Program.cs:94-97` | `new GameEngine` / `new ConsoleRenderer` / `new GameApp`; DI-хост создаётся, но для игры не используется |
| 5 | `Domain` прибит к Linux | `Snake.Domain.csproj:5-6` | `RuntimeIdentifier=linux-x64` + `RuntimeFrameworkVersion=3.1.3` — проект не переиспользуется на Windows и в Unity |
| 6 | Ядро тянет лишние пакеты и старый TFM | `Application.csproj:8-13`, `Core.csproj:8` | EF Core, MediatR, AutoMapper, FluentValidation, `Microsoft.AspNetCore.Http`; `netcoreapp3.1` не совместим с Unity (.NET Standard 2.1) |
| 7 | Принудительные сеттеры `internal` | `GameEngine.cs:277-292` | `ForceFood` / `ForceSnake` / `ForceState` доступны только внутри `Snake.Application` — тесты и боты снаружи не могут подготовить сценарий |
| 8 | Проверка только «руками» | `Application/Game/SelfTest.cs`, `Console/Program.cs:33` | тестового проекта нет; `--selftest` падает при перенаправленном выводе: `Console.CursorVisible = false` бросает `IOException` «Неверный дескриптор» (проверено) |

---

## 4. Целевое устройство

### 4.1 Правило зависимостей (жёсткое)

```
Snake.Domain ◄── Snake.Core ◄── Snake.Application
                                      ▲
            ┌─────────────┬───────────┼───────────┬─────────────┐
        Console       Unity2D     Unity3D        Web          ...
```

- Ядро (`Domain`, `Core`, `Application`) **не знает** про UI, консоль, Unity, время процесса и БД.
- Ядро — **только BCL**: без `Console`, `Thread.Sleep`, `Stopwatch`, без Unity-типов, без EF/MediatR.
- Презентеры зависят от `Application`; ядро **никогда** не зависит от презентеров.
- Презентеры **не знают друг о друге** и не ссылаются друг на друга.
- `Persistence` — необязательный слой; ядро от него не зависит. Его подключает composition root презентера/сервиса.

### 4.2 Порт вывода и снимок состояния

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

### 4.3 `GameSession` — единственная точка входа для представлений

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
  GameSession.cs                       фасад для презентеров
  Abstractions/GameSnapshot.cs
  Abstractions/IGameView.cs
Snake.Console/
  Program.cs                           режимы, конфиг, composition root
  ConsoleInput.cs
  ConsoleRenderer.cs                   реализация отрисовки
  ConsoleGameLoop.cs                   реализация игрового цикла
  FileLogger*.cs
Snake.Unity2D/   (Unity-проект, вне Snake.sln)
  Assets/Scripts/Snake2DGameView.cs
  Assets/Scripts/Snake2DInput.cs
Snake.Unity3D/   (Unity-проект, вне Snake.sln)
  Assets/Scripts/Snake3DGameView.cs
  Assets/Scripts/Snake3DInput.cs
```

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

**Разово для консоли:** `Program.cs` берёт `GameSession` из DI или создаёт в одном месте, `ConsoleGameLoop` реализует цикл через `IGameView`.
**Проверка после фазы 0:** `dotnet build Snake.sln`, `--selftest` и `--demo` дают тот же результат, что до рефакторинга.

### 5.1 Консоль (существует)

`Snake.Console` — эталонный презентер: `ConsoleGameLoop` + `ConsoleRenderer : IGameView` + `ConsoleInput`.
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

### 5.5 Definition of Done для нового представления

- [ ] Игрок двигается, ест, умирает, ставит паузу, перезапускает.
- [ ] Отображаются очки, уровень, длина, скорость, статус (пауза / конец / победа).
- [ ] `Domain` и `GameEngine` **не изменены**; если пришлось менять — это ошибка дизайна, сообщи о ней.
- [ ] Новый проект не ссылается на другие презентеры и не добавляет пакетов в ядро.
- [ ] `dotnet build Snake.sln` зелёный, `--selftest` проходит.

---

## 6. Жёсткие запреты

1. **Не** добавлять в `Domain`/`Core`/`Application` ссылки на `System.Console`, Unity, EF Core, MediatR, AutoMapper, FluentValidation, ASP.NET Core.
2. **Не** класть отрисовку, ввод или таймер в `Application` — только в проект-презентер.
3. **Не** протаскивать в `GameSnapshot` и в ядро платформенные типы (`Transform`, `Sprite`, `ConsoleColor`, `GameObject`).
4. **Не** давать презентеру ссылку на `GameEngine` — только `GameSession` + `GameSnapshot`.
5. **Не** добавлять в `GameEngine` поля и ветки вида «если это Unity / если это консоль».
6. **Не** создавать новые проекты без явного запроса.
7. **Не** «оптимизировать» архитектуру без запроса: не сливать `Direction` и `GameCommand`, не переносить правила игры в презентер, не заменять `Tick()` на событийную модель.
8. **Не** добавлять CQRS-структуру (`Commands/`, `Queries/`, `Handlers/`, `Result<T>`) — её в проекте нет и она здесь не нужна.
9. **Не** тащить `Persistence` в Unity/Web-презентер, если он не сохраняет рекорды.
10. Любая «обратная» ссылка (презентер → ядро → презентер) — признак ошибки дизайна: сообщи, не обходи.

---

## 7. Соглашения об именовании

| Что | Шаблон | Пример |
|---|---|---|
| Проект-представление | `Snake.<Платформа>` | `Snake.Console`, `Snake.Unity2D`, `Snake.Unity3D` |
| Отрисовка | `<Платформа>GameView : IGameView` | `ConsoleRenderer`, `Snake2DGameView` |
| Ввод | `<Платформа>Input` | `ConsoleInput`, `Snake2DInput` |
| Игровой цикл | `<Платформа>GameLoop` | `ConsoleGameLoop` |
| Ядро | `GameEngine`, `GameSession`, `GameSnapshot`, `IGameView` | — |
| Enum'ы | в `Snake.Domain/Enum` | `Direction`, `TickResult` |
| Async-методы | суффикс `Async` + `CancellationToken ct = default` | `SaveScoreAsync` |

В именах публичных типов ядра **не должно** быть слов `Console`, `Unity`, `Sprite`, `Transform`, `Camera`, `Window`.

---

## 8. Проверка результата

```powershell
dotnet build Snake.sln                                  # 0 ошибок
dotnet run --project Snake.Console -- --selftest        # самопроверка логики
dotnet run --project Snake.Console -- --demo 300 42     # бот играет 300 тиков
dotnet run --project Snake.Console                      # играть
```

- `--selftest` и `--demo` **работают только в интерактивном терминале**: при перенаправленном выводе `Program.cs:33` (`Console.CursorVisible = false`) бросает `IOException` «Неверный дескриптор». Это известный баг — в CI его обходить не «костылём в ядре», а выносом консольных вызовов в презентер.
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
| `SnakeDbContext`, `DbConnector`, generic-репозитории, `DbInitializer` | `Snake.Persistence` | не используются; `DbConnector` читает строку `"As"` вместо `"Snake"` |
| Дубли `PackageReference` `Microsoft.EntityFrameworkCore` | `Snake.Application.csproj:8,14` | предупреждение NU1504 — удалить дубль |
| `AutoMapper 11.0.1` | `Snake.Application.csproj` | известные уязвимости (NU1903) |
| `RuntimeIdentifier=linux-x64`, `RuntimeFrameworkVersion=3.1.3` | `Snake.Domain.csproj` | ломает переиспользование; убрать вместе с переходом на `netstandard2.1` |
| `_isRunning` не используется | `Snake.Console/Program.cs:24` | предупреждение CS0414 |
