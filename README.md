# Методический отдел университета

**Repository**: https://github.com/chisCake/university-methodological-department

## Обзор проекта

Автоматизированная информационная система **«Методический отдел университета»** — настольное приложение для учёта, хранения и анализа учебно-методических данных: факультеты, кафедры, секции, сотрудники, специальности, дисциплины и элементы учебных планов.

Система обеспечивает централизованное ведение справочников, аналитические запросы, экспорт отчётов (Excel, Word), разграничение доступа через Supabase Auth и RLS, а также серверные триггеры целостности и аудит изменений в PostgreSQL.

Подробное описание предметной области, ТЗ и реализации — в [пояснительной записке](docs/ВЗРПП.курсовой.md).

### Основные компоненты

1. **UniversityMethodologicalDepartment.App** — WinUI 3: UI, ViewModels, навигация, auth, отчёты и аналитика
2. **UniversityMethodologicalDepartment.Bus** — домен, EF Core, репозитории, `DataService` / `CachedDataService` и др.
3. **UniversityMethodologicalDepartment.Tests** — xUnit-тесты против локальной БД
4. **supabase/** — миграции, seed, Edge Functions, конфигурация Supabase CLI

### Технологический стек

- **Клиент**: WinUI 3, .NET 10, C#, MVVM (Community Toolkit)
- **Данные**: Entity Framework Core, Npgsql, PostgreSQL (Supabase)
- **Безопасность**: Supabase Auth, Row Level Security, триггеры и `audit_log`
- **Отчёты**: DocumentFormat.OpenXml (Excel, Word), LiveCharts
- **Инфраструктура**: Docker, Supabase CLI, Node.js / pnpm

## Быстрый старт

1. **Установка и локальный запуск** — раздел [Запуск (локально, Docker)](#запуск-локально-docker) ниже
2. **Конфигурация** — [Конфигурация](#конфигурация) (копирование `*.example.*`)
3. **Курсовая документация** — [docs/ВЗРПП.курсовой.md](docs/ВЗРПП.курсовой.md)

## Навигация по документации

### Основные документы

- [Пояснительная записка (курсовой проект)](docs/ВЗРПП.курсовой.md) — предметная область, ТЗ, БД, UML, безопасность, руководство пользователя
- [Методические указания](docs/ВЗРПП.метода.md) — требования к оформлению и содержанию работы

### Документация по слоям приложения

#### Клиент (WinUI — `.App`)

- UI, ViewModels, страницы списков/деталей, аналитика, отчёты, настройки — см. [диаграммы классов UI/ViewModel](docs/Diagrams/classes-layers.md)
- [Диаграмма компонентов](docs/Diagrams/components.md)

#### Бизнес-логика и данные (`.Bus`)

- Сущности, репозитории, кэш, доступ к БД — [слои данных](docs/Diagrams/classes-layers.md), [доменные классы](docs/Diagrams/classes-domain.md)
- [Паттерны проектирования](docs/Diagrams/patterns.md)

#### База данных (`supabase/`)

- [Концептуальная модель](docs/Diagrams/conceptual.md)
- [Логическая модель](docs/Diagrams/db-logical.md)
- [Физическая модель](docs/Diagrams/db-physical.md)
- Миграции: `supabase/migrations/`, seed: `supabase/seed.sql`

### Диаграммы

| Документ | Содержание |
|----------|------------|
| [Use Case — сотрудник (обзор)](docs/Diagrams/use-case-employee-overview.md) | Просмотр, CRUD, запросы, диаграммы, отчёты |
| [Use Case — сотрудник (управление)](docs/Diagrams/use-case-employee-management.md) | Детализация сценариев редактирования |
| [Use Case — администратор](docs/Diagrams/use-case-admin.md) | Пользователи, журнал аудита |
| [Компоненты](docs/Diagrams/components.md) | Сборка App/Bus и зависимости |
| [Развёртывание](docs/Diagrams/deployment.md) | Клиент, Supabase, PostgreSQL |
| [Классы (дизайн)](docs/Diagrams/classes-design.md) | Структура прикладного слоя |

### Руководства

| Раздел README | Назначение |
|---------------|------------|
| [Структура репозитория](#структура-репозитория) | Папки и проекты решения |
| [Конфигурация](#конфигурация) | `appsettings`, `.runsettings` |
| [Запуск (локально, Docker)](#запуск-локально-docker) | Supabase, seed, сборка |
| [AGENTS.md](AGENTS.md) | Соглашения для разработки (AI) |
| [Известные баги](docs/KNOWN_ISSUES.md) | Неисправленные недочёты |

## Основные возможности

### Для сотрудника (без входа — только чтение; с входом — изменение данных)

- Справочники: факультеты, кафедры, секции, сотрудники, специальности, дисциплины, учебные планы
- Глобальный поиск по записям
- Аналитические запросы (дисциплины нескольких кафедр, несколько семестров, нагрузка по кафедрам и др.)
- Диаграммы (лабораторные часы по кафедрам в семестре)
- Экспорт отчётов в Excel и Word

### Для администратора

- Управление пользователями (Supabase Auth)
- Просмотр журнала аудита (`audit_log`)

## Архитектура вкратце

```
┌─────────────────────┐     HTTPS      ┌──────────────────┐
│  WinUI 3 (.App)     │───────────────>│  Supabase Auth   │
│  + .Bus (EF/Npgsql) │                │  Edge Functions  │
└──────────┬──────────┘                └────────┬─────────┘
           │ SQL (RLS)                          │
           └──────────────────┬─────────────────┘
                              ▼
                    ┌──────────────────┐
                    │    PostgreSQL    │
                    │  migrations/seed │
                    │ triggers / audit │
                    └──────────────────┘
```

Подробнее: [диаграмма развёртывания](docs/Diagrams/deployment.md), [компоненты](docs/Diagrams/components.md).

## Структура репозитория

| Путь | Назначение |
|------|------------|
| `UniversityMethodologicalDepartment.App/` | WinUI 3 — UI (XAML), ViewModels, сервисы приложения, навигация, тема, auth |
| `UniversityMethodologicalDepartment.Bus/` | Домен и доступ к данным: сущности EF, репозитории, `DataService` / `CachedDataService` |
| `UniversityMethodologicalDepartment.Tests/` | xUnit-тесты против локальной БД (Docker / Supabase) |
| `supabase/` | Схема и миграции PostgreSQL, seed, Edge Functions, `config.toml` для Supabase CLI |
| `scripts/` | Вспомогательные скрипты: scaffold EF (`scaffold-db.ps1`), seed пользователей Auth (`seed-auth-user.js`) |
| `docs/` | Курсовая и методичка: текст, диаграммы, материалы по проекту |

Корень: `UniversityMethodologicalDepartment.slnx`, `package.json` / `pnpm-lock.yaml` (Supabase CLI, `db:reset`, seed auth).

## Конфигурация

Файлы с локальными значениями в git не хранятся. Скопировать шаблоны (убрать суффикс `.example`):

| Шаблон | Куда |
|--------|------|
| `UniversityMethodologicalDepartment.App/appsettings.example.json` | `appsettings.json` |
| `UniversityMethodologicalDepartment.App/appsettings.dev.example.json` | `appsettings.dev.json` |
| `UniversityMethodologicalDepartment.Bus/appsettings.example.json` | `appsettings.json` (только для `dotnet ef`, см. ниже) |
| `UniversityMethodologicalDepartment.Tests.runsettings.example` | `UniversityMethodologicalDepartment.Tests.runsettings` |

**`appsettings` в `.Bus`:** в сборку приложения не попадает. При запуске WinUI конфигурация читается только из каталога **`.App`** (`CopyToOutputDirectory`). Файл в `.Bus` нужен отдельно для **EF Core CLI** (`scripts/scaffold-db.ps1`): там строка под пользователя `postgres`, а в App — под `app_readonly` (RLS).

## Запуск (локально, Docker)

Нужны: [Docker Desktop](https://www.docker.com/products/docker-desktop/), [Supabase CLI](https://supabase.com/docs/guides/cli), Node.js + [pnpm](https://pnpm.io/), .NET 10 SDK, Windows (WinUI 3).

```powershell
# 1. Зависимости Node (Supabase CLI, seed-auth)
pnpm install

# 2. Локальный Supabase в Docker (API :54321, Postgres :54322, Studio :54323)
supabase start

# 3. Конфиг приложения и тестов (см. таблицу выше)
Copy-Item UniversityMethodologicalDepartment.App\appsettings.example.json UniversityMethodologicalDepartment.App\appsettings.json
Copy-Item UniversityMethodologicalDepartment.App\appsettings.dev.example.json UniversityMethodologicalDepartment.App\appsettings.dev.json
Copy-Item UniversityMethodologicalDepartment.Bus\appsettings.example.json UniversityMethodologicalDepartment.Bus\appsettings.json
Copy-Item UniversityMethodologicalDepartment.Tests.runsettings.example UniversityMethodologicalDepartment.Tests.runsettings

# 4. Миграции + seed БД + пользователи Auth
pnpm run db:reset:seed

# 5. Edge Functions (отдельный терминал; нужно для управления пользователями в админке)
supabase functions serve

# 6. Сборка и запуск
dotnet build UniversityMethodologicalDepartment.slnx
# Запуск — из Visual Studio / F5 по проекту App
```

`pnpm run db:reset:seed` = `supabase db reset` + `node scripts/seed-auth-user.js`.

### Что поднимает Docker / seed

- **`supabase start`** — контейнеры Supabase (PostgreSQL, Auth, API, Studio и др.) по `supabase/config.toml`.
- **`supabase db reset`** — накатывает `supabase/migrations/*.sql`, затем `supabase/seed.sql`:
  - справочники БарГУ: факультеты, кафедры, секции, сотрудники (в т.ч. ИСиТ, ACNT, EAP, PMD);
  - специальности, дисциплины, элементы учебных планов;
  - роль БД `app_readonly` (пароль `app_readonly_local`) для приложения и тестов.
- **`supabase functions serve`** — локальный запуск Edge Functions (например `admin-users` для админ-раздела); процесс держать запущенным в отдельном терминале.
- **`pnpm run seed:auth`** — пользователи в Supabase Auth (не в `seed.sql`):

| Email | Пароль | Роль |
|-------|--------|------|
| `test@example.com` | `12345678` | обычный пользователь (`app_metadata.role=user`) |
| `admin@example.com` | `12345678` | администратор (`app_metadata.role=admin`) |

Ключи Supabase и строки подключения в example-файлах рассчитаны на этот локальный стенд (`127.0.0.1`, publishable anon key из Docker).

### Тесты

Скопировать `UniversityMethodologicalDepartment.Tests.runsettings.example` → `UniversityMethodologicalDepartment.Tests.runsettings`. Перед тестами: `supabase start` и актуальная БД (`pnpm run db:reset:seed` при необходимости).

## Контакты

**Автор**: Олешкевич Кирилл

- **Email**: kirilloleshkevich7@gmail.com
- **GitHub**: [@chisCake](https://github.com/chisCake)
- **Репозиторий**: [university-methodological-department](https://github.com/chisCake/university-methodological-department)
