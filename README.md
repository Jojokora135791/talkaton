# Толкатрон

Календарь как пятая вкладка Контур.Толка — рядом с Чатами, Артефактами, Досками
и Контактами. Ради трёх вещей:

1. **Уведомление перед началом следующей встречи** с кнопкой «Присоединиться»,
   ведущей прямо в комнату Толка.
2. **Постановка встреч через LLM** — из ИИ-протокола прошедшей встречи достаётся
   договорённость «созвонимся в четверг по биллингу» и предлагается готовый черновик.
3. **Интеграция с внешними календарями** — Google Calendar и ICS-фид.

Полный план работ — в [PLAN.md](PLAN.md), макет — в `template_1.png`.

---

## Как поднять локально

Нужен только Docker.

```bash
docker compose up --build
```

Дальше:

| Что | Где |
|---|---|
| Календарь | http://localhost:8080 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |
| Postgres | `localhost:5432`, база/логин/пароль — `talkaton` |

Миграции и seed применяются на старте api — руками ничего делать не нужно.
В левой панели должны появиться четыре календаря из макета, внизу центральной
колонки — строка `API: healthy · Postgres: up`.

Сбросить базу: `docker compose down -v`.

## Разработка без Docker

Бэкенду нужен .NET SDK 8, фронту — Node 22.

```bash
# Postgres — из compose, остальное локально
docker compose up -d postgres

# бэкенд на http://localhost:5080
dotnet run --project backend/src/Talkaton.Api

# фронтенд на http://localhost:4200, /api проксируется на 5080
cd frontend && npm ci && npm start
```

## Проверки

```bash
dotnet test backend/Talkaton.sln          # тесты бэкенда
cd frontend && npm run lint && npx ng test --watch=false && npm run build
```

Те же шаги плюс приёмка `docker compose up` гоняются в GitHub Actions
на каждый PR — см. [.github/workflows/ci.yml](.github/workflows/ci.yml).

### Миграции

```bash
dotnet ef migrations add ИмяМиграции \
  --project backend/src/Talkaton.Infrastructure \
  --startup-project backend/src/Talkaton.Infrastructure \
  --output-dir Persistence/Migrations
```

CI отдельным шагом проверяет, что модель не разъехалась с миграциями.

---

## Структура

```
talkaton/
├── frontend/                        Angular 22, тёмная тема
│   ├── src/styles/tokens.scss       токены темы, снятые с макета
│   ├── src/app/layout/              шапка с вкладками Толка
│   ├── src/app/features/calendar/   страница календаря, три колонки
│   └── nginx.conf                   прод-раздача + проксирование /api
├── backend/
│   ├── src/Talkaton.Domain/         сущности
│   ├── src/Talkaton.Infrastructure/ EF Core, миграции, seed
│   ├── src/Talkaton.Api/            minimal API, Swagger, CORS
│   └── tests/
├── docs/                            ADR, схема API, скрипт демо
└── docker-compose.yml
```

## Договорённости

- **Время — только в UTC**, таймзона пользователя хранится отдельным полем.
  Переделывать это позже дороже всего.
- **Повторяемость — по RFC 5545 (RRULE)**, а не самописным форматом: иначе
  синхронизация с Google и ICS превратится в боль.
- **Цвета — только через `var(--tk-*)`.** Ни одного HEX в компонентах:
  на этапе склейки с Толком меняются только токены.
- **Весь код, общающийся с Толком, живёт за интерфейсом `ITalkGateway`** (этап 3).
  Это главная страховка проекта.

## Ветвление

`main` — стабильное, фичи — в `feat/*`, вливаются через PR с зелёным CI.
