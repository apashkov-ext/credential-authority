# Архитектура исходной версии

## Образец

Обязательный образец структуры и стиля —
[chat-ops, main, d5853a4](https://github.com/apashkov-ext/chat-ops/tree/d5853a41b7022850cab9b8ca5b1dda3783342146).
Фиксированная ревизия позволяет воспроизвести архитектурные договорённости.
Функции Telegram и GitLab в новое приложение не переносятся.

Платформа нового приложения — **.NET 10**, хотя образец использует .NET 8.
SQLite применяется для выполнения требований сохранности; операции с БД
реализуются через `Microsoft.Data.Sqlite`, без добавления EF Core.

## Слои и зависимости

| Проект | Ответственность | Ссылки на проекты |
| --- | --- | --- |
| `CredentialAuthority.Api` | HTTP, фоновые воркеры, сборка приложения | App, Infra |
| `CredentialAuthority.App` | Сценарии, доменные типы, порты | Нет |
| `CredentialAuthority.Infra` | SQLite, схема, транзакции, адаптеры портов | App |

Отдельные проекты Domain, Core, Contracts или Application не создаются:
домен находится в `App/Core`, как в образце. HTTP DTO не заменяют доменные
типы. App не зависит от HTTP, Infra, SQLite и файловой системы.
DI-регистрация в App через `IServiceCollection` допустима и соответствует
образцу; доменные типы в `App/Core` не зависят от DI и конфигурации хоста.

## Целевая структура

```text
code-review-task/
  AGENTS.md
  README.md
  .gitignore
  NuGet.Config
  docs/
    architecture.md
    acceptance.md
  src/
    CredentialAuthority.sln
    global.json
    Directory.Build.props
    Directory.Packages.props
    CredentialAuthority.Api/
      CredentialAuthority.Api.csproj
      Program.cs
      Module.cs
      Properties/launchSettings.json
      CredentialAuthority.Api.http
      appsettings.json
      Features/
        Module.cs
        CreateRequest/Module.cs
        GetRequest/Module.cs
        GetCredential/Module.cs
      Integrations/
        Module.cs
      Serialization/
        UtcDateTimeOffsetConverter.cs
      Workers/
        Module.cs
        DatabaseInitializationWorker/
          DatabaseInitializationWorker.cs
          Module.cs
        IssuanceWorker/
          IssuanceWorker.cs
          Module.cs
    CredentialAuthority.App/
      CredentialAuthority.App.csproj
      Core/
        Models/
      Features/
        CreateRequest/
        GetRequest/
        GetCredential/
        IssueCredential/
      SharedPorts/
    CredentialAuthority.Infra/
      CredentialAuthority.Infra.csproj
      Features/
        CreateRequest/
        GetCredential/
        IssueCredential/
      Integrations/
        Sqlite/Module.cs
      SharedAdapters/
    Tests/
      Directory.Build.props
      Directory.Packages.props
      CredentialAuthority.Api.Tests/
      CredentialAuthority.App.Tests/
      CredentialAuthority.Infra.Tests/
```

В папках сценариев находятся их реализации и `Module.cs`, а не только
регистрации. Пустые каталоги и фиктивные классы ради заполнения схемы
не добавляются. `IssueCredential` — внутренний сценарий без HTTP-ручки.

## Сценарии, порты и результаты

- Сценарии называются `*UseCase`, имеют интерфейс `I*UseCase` и метод
  `Execute(..., CancellationToken ct = default)`, как в образце.
- Порты конкретного сценария лежат рядом с ним в `App/Features/<Feature>`;
  действительно общие порты — в `App/SharedPorts`.
- Адаптеры лежат в соответствующем `Infra/Features/<Feature>` или
  `Infra/SharedAdapters`. Generic repository и общий сервис всех операций
  не вводятся.
- Предметные исходы представлены отдельными record-типами и результатом
  `OneOf` с алиасом в файле `*Result.cs`. Ожидаемый конфликт владельца
  не маскируется техническим исключением или строкой ошибки.
- API преобразует результаты сценария в HTTP-ответы через явный `Match`.
- Доменные типы владельца, заявки, удостоверения и их переходы находятся
  в `App/Core/Models` и представлены records. Инварианты имени владельца
  инкапсулированы в `OwnerName`. Базовый `ValueObject` и SharedKernel отсутствуют.
- Стиль C#: file-scoped namespace, явные конструкторы и зависимости,
  `sealed` для конкретных реализаций, `internal` для непубличных адаптеров.

## Модули и сборка

- `Module.cs` содержит `static class Module` и extension-методы регистрации.
- App предоставляет `Add<Feature>FeatureApp`, Infra —
  `Add<Feature>FeatureInfra` для соответствующего сценария.
- API-модуль сценария объединяет регистрацию App и Infra с транспортом.
  `Api/Features/Module.cs` собирает эти регистрации в `AddFeatures`.
- `ConfigureApi` настраивает HTTP: Problem Details, JSON и обработку некорректных
  запросов. `AddIntegrations` регистрирует только подключение SQLite.
- Общие адаптеры подключаются централизованно:
  `Program.cs → Api/Module.cs:AddAdapters → Infra/SharedAdapters/Module.cs:AddSharedAdapters`.
  Модули Features и Workers не вызывают `AddSharedAdapters`.
  Регистрация общего `TimeProvider` идемпотентна, поскольку часы нужны
  как HTTP-сценариям, так и воркеру.
- Все воркеры находятся в `Api/Workers/<WorkerName>/`. Рядом лежит
  `Module.cs`, регистрирующий сам воркер и его собственные сценарии/адаптеры/часы.
  Это правило распространяется и на одноразовые воркеры. Общие адаптеры
  остаются в `AddAdapters`, подключение БД — в `AddIntegrations`.
- `Workers/Module.cs` собирает регистрации в `AddWorkers`: сначала
  `DatabaseInitializationWorker`, затем `IssuanceWorker`. Запуск hosted
  services последовательный (`ServicesStartConcurrently = false`).
- `Program.cs` остаётся короткой точкой сборки. Предметные проверки,
  SQL и обработка заявок в него не переносятся.
- `IssuanceWorker` вызывает сценарии App и отвечает за период 10 секунд,
  последовательный запуск обходов, DI scope и корректную остановку.
  Решение о выдаче принадлежит App/Core.
- `DatabaseInitializationWorker` выполняет подготовку схемы один раз в
  `ExecuteAsync`. Его `StartAsync` ожидает завершения этой задачи: HTTP-сервер
  и выдача не стартуют раньше готовности БД. Ошибка инициализации прерывает
  запуск. Прямой вызов инициализации из Program отсутствует.

## Настройки сборки и пакеты

- `.sln`, `global.json` и общие `.props` находятся в `src`, как в образце.
- `src/Directory.Build.props` задаёт `net10.0`, nullable, implicit usings,
  версию языка и общие ссылки на необходимые Microsoft.Extensions-пакеты.
  Для Web-проекта эти ссылки не дублируются: их предоставляет shared framework.
- `src/Directory.Packages.props` включает
  `ManagePackageVersionsCentrally=true` и содержит версии рабочих пакетов.
  В `.csproj` у `PackageReference` нет атрибутов `Version`/`VersionOverride`.
- В `src/Tests` имеются собственные `Directory.Build.props` и
  `Directory.Packages.props`, явно импортирующие родительские файлы.
  Test SDK, xUnit, runner, coverlet, Moq и Moq.AutoMock объявляются
  централизованно для тестов, как в образце.
- Версии `net10.0` и рабочих пакетов не дублируются в отдельных проектах.
- SDK 10 фиксируется в `src/global.json`. В `global.json` указан `10.0.100`
  с `latestFeature`, разрешающим стабильные SDK 10.0 без привязки
  к локальному патчу SDK разработчика.
- `OneOf` подключён непосредственно в App; дополнительный проект для него
  не создаётся. Не переносятся неиспользуемые Telegram, Refit, GitLab,
  feature-management-пакеты и настройки чужого приложения.
- Общая структура повторяется; значения UserSecretsId, имена сборок,
  пути к изображениям и версии самого ChatOps не копируются.

## Данные, время и восстановление

SQLite — единственное авторитетное хранилище заявок и удостоверений.
Схема создаётся автоматически одноразовым `DatabaseInitializationWorker`
на пустом локальном хранилище до начала приёма HTTP-запросов.
Путь к БД задаётся конфигурацией; рабочие файлы не включаются в Git.

Инвариант уникальности обеспечивается атомарной операцией в хранилище,
а не только предварительным SELECT. Сравнение владельца в домене и БД
должно одинаково учитывать регистр латинских и русских букв.
Отклонённые заявки сохраняются, но не удерживают резерв владельца.

Данные документа хранятся в той же строке, что и заявка. Выпуск документа
и переход заявки фиксируются одним условным UPDATE в неявной транзакции
SQLite. Условие `status = Pending` защищает от повторного завершения.
Частичный сбой не оставляет документ без завершённой заявки и не разрешает
второй выпуск. После перезапуска обход выбирает сохранённые `Pending`.

Время поступает через `TimeProvider`; тесты управляют им явно.
Граничные сценарии не требуют реальных задержек. Фоновый период — 10 секунд,
а не искусственная задержка внутри сценария выпуска.

## Проверка реализации

App.Tests проверяет доменные правила и сценарии с портами; Infra.Tests —
настоящую SQLite, транзакции, параллельное создание и сохранность;
Api.Tests — HTTP-контракт, сериализацию и запуск фоновой обработки.
Подмена портов использует Moq/Moq.AutoMock по стилю образца.
Инфраструктурные проверки не заменяются моками SQL-адаптеров.

Критерии: [acceptance.md](acceptance.md).

## Запуск из IDE

Общий `Api/Properties/launchSettings.json` с `commandName: Project`
используется Rider и `dotnet run`. Профиль не содержит абсолютных путей,
локальных переменных и зависимости от IIS или Docker. Локальная конфигурация
Rider `.idea` не является частью контракта запуска. HTTP-порт хранится в
`appsettings.json`; относительный путь SQLite разрешается от content root
API-проекта. Запросы для ручной проверки находятся в `Api/CredentialAuthority.Api.http`.
