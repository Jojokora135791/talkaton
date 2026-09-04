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

| Что       | Где                                     |
| --------- | --------------------------------------- |
| Календарь | http://localhost:8080                   |
| API       | http://localhost:5080                   |
| Swagger   | http://localhost:5080/swagger           |
| Health    | http://localhost:5080/health            |
| База      | SQLite в томе `sqlite-data`, `/data/talkaton.db` |

Миграции применяются на старте api — руками ничего делать не нужно.

На экране входа достаточно ввести имя: пароля нет, а имя работает как ключ
к своему рабочему пространству. При первом входе заводятся четыре календаря
с макета, списки участников и демо-неделя встреч — включая повторяющийся
«Штаб Платформы Данных» с артефактами и участниками. То же имя в следующий раз
вернёт ровно те же данные; другое имя — отдельное пространство, поэтому
многопользовательский сценарий проверяется в двух вкладках.

Сбросить всё: `docker compose down -v`.

## Разработка без Docker

Бэкенду нужен .NET SDK 8, фронту — Node 22.

```bash
# бэкенд на http://localhost:5080, база — файл backend/src/Talkaton.Api/talkaton.db
dotnet run --project backend/src/Talkaton.Api

# фронтенд на http://localhost:4200, /api проксируется на 5080
cd frontend && npm ci && npm start
```

## Запуск из VS Code

В репозитории лежат общие `.vscode/launch.json` и `.vscode/tasks.json`.
Откройте корень репозитория (не подпапку) и нажмите F5 — в списке конфигураций три сценария:

| Конфигурация                                    | Что нужно установить      | Что делает                                                                   |
| ----------------------------------------------- | ------------------------- | ---------------------------------------------------------------------------- |
| **Стенд: бэкенд + фронтенд**              | .NET SDK 8, Node 22, C# Dev Kit | Собирает и запускает API на 5080, поднимает`ng serve` на 4200 и открывает браузер. Точки останова работают и в C#, и в TypeScript. |
| **Фронтенд: ng serve + браузер**       | Node 22                   | Только фронт — если API уже поднят руками или в Docker.                                                                                             |
| **Браузер к стенду в Docker (8080)** | Docker                    | Поднимает`docker compose` и открывает готовый стенд. Ни .NET, ни Node не нужны.                                                          |

Если Chrome не установлен, замените в `launch.json` `"type": "chrome"` на `"msedge"`.

Локальный API и стенд в Docker слушают один и тот же порт 5080, поэтому одновременно
они не поднимутся: перед запуском бэкенда из VS Code выполните задачу
**docker: остановить стенд**.

Через `Ctrl+Shift+P → Tasks: Run Task` доступны сборка, тесты бэкенда и фронта,
линт, проверка расхождения миграций с моделью и управление стендом в Docker.

Отдельная конфигурация **Swagger** просто открывает http://localhost:5080/swagger
у уже запущенного API.

### Если .NET SDK ещё не установлен

Установщик с https://dotnet.microsoft.com/download/dotnet/8.0 (SDK 8.0.x → Windows x64)
либо, без прав администратора, скрипт в профиль пользователя:

```powershell
Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
./dotnet-install.ps1 -Channel 8.0 -InstallDir "$env:USERPROFILE\.dotnet"
dotnet tool install --global dotnet-ef --version 8.0.10

# PATH скрипт сам не правит
$user = [Environment]::GetEnvironmentVariable('PATH','User')
[Environment]::SetEnvironmentVariable('PATH', "$user;$env:USERPROFILE\.dotnet", 'User')
```

После этого **VS Code нужно перезапустить** — расширение C# ищет `dotnet` в PATH при старте.
Задачи сборки и тестов дописывают этот путь сами и работают сразу.

Пока SDK нет, тестировать можно конфигурацией **Браузер к стенду в Docker**.

### Если рядом стоит SDK 10

`dotnet test` берёт тестовый хост от того SDK, который первым нашёлся в PATH. Под SDK 10
хост поднимается на рантайме .NET 10, и EF Core 8 падает на разборе параметров запроса
(`Contains` по массиву) — тест `ParticipantListApiTests` не проходит, хотя код исправен.

Поэтому задачи VS Code ставят `%USERPROFILE%\.dotnet` **перед** системным путём.
Из терминала запускайте тесты так же:

```powershell
$env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
dotnet test backend/Talkaton.sln
```

Проверить, какой SDK берётся: `(Get-Command dotnet).Source` и `dotnet --list-sdks`.

## Развёртывание на сервере

Стенд рассчитан на то, что снаружи его закрывает чужой реверс-прокси с TLS, а сам
он слушает только петлевой интерфейс. Порты, окружение и политика перезапуска
задаются переменными; на сервере их кладут в `.env` рядом с `docker-compose.yml`
(файл в `.gitignore`, в репозиторий не попадает):

```bash
WEB_BIND=127.0.0.1          # интерфейс публикации фронта
WEB_PORT=9080               # порт, в который смотрит внешний nginx
API_BIND=127.0.0.1          # API наружу не нужен: только для отладки с самой машины
API_PORT=9081
ASPNETCORE_ENVIRONMENT=Production   # выключает Swagger и debug-логи
RESTART_POLICY=unless-stopped       # стенд переживает перезагрузку сервера
```

Без `.env` работают значения по умолчанию — `0.0.0.0`, 8080 и 5080, `Development`,
без автоперезапуска, — то есть локальный стенд ведёт себя ровно как раньше.

Проверить, что подставилось, до запуска: `docker compose config`.

Проксировать наружу нужно **только** `WEB_PORT`: nginx внутри контейнера `web`
сам разбирает `/api/` и отправляет его в API по внутренней сети. Во внешнем nginx
адрес пишите как `http://127.0.0.1:9080`, а не `localhost` — Docker при привязке
к loopback слушает только IPv4, а `localhost` резолвится ещё и в `::1`, и часть
запросов будет получать `connection refused`.

Миграции применяются на старте и в `Production` — это `Database:MigrateOnStartup`
в `appsettings.json`, от окружения оно не зависит.

⚠️ Закрыть порт через `ufw` нельзя: Docker публикует порты правилами в цепочке
`DOCKER`, которая обходит `INPUT`, где живёт ufw. Работает только привязка
к `127.0.0.1`. В ufw открывают лишь 22, 80 и 443.

⚠️ Аутентификации в приложении нет до этапа 6: вход по имени, дальше GUID
в заголовке. Публичный стенд открыт любому, кто угадает имя.

### Автоматическая выкатка

Задание `deploy` в CI срабатывает на push в `main` и только после зелёных
`backend`, `frontend` и `compose` — сломанный коммит на стенд не попадёт.
Оно подключается по SSH к пользователю `deploy` и ничего ему не передаёт:
что запускать, решает сервер. Ключ в `~deploy/.ssh/authorized_keys` ограничен
принудительной командой, оболочки у него нет:

```
command="/usr/local/bin/talkaton-deploy.sh",no-port-forwarding,no-agent-forwarding,no-X11-forwarding,no-pty ssh-ed25519 AAAA... github-deploy
```

Сам скрипт лежит **вне** рабочей копии намеренно: он делает `git reset --hard`
в `/opt/talkaton`, а bash дочитывает скрипт по ходу выполнения — файл под
контролем git переписался бы прямо во время работы.

```bash
#!/usr/bin/env bash
set -euo pipefail
BRANCH="${TALKATON_BRANCH:-main}"
cd /opt/talkaton
git fetch --prune origin
git checkout -B "$BRANCH" "origin/$BRANCH"
git reset --hard "origin/$BRANCH"
docker compose up -d --build --wait
docker image prune -f
```

Секретов нужно два — **Settings → Secrets and variables → Actions**:

| Секрет | Откуда взять |
| ------ | ------------ |
| `DEPLOY_SSH_KEY` | приватный ключ пары, публичная половина которой лежит в `authorized_keys` |
| `DEPLOY_KNOWN_HOSTS` | вывод `ssh-keyscan -p 44 -H talkaton.duckdns.org` целиком |

Порт и пользователь заданы в самом задании: они не секрет.

Откат сломанной выкатки — на сервере `git reset --hard <коммит>` и
`docker compose up -d --build --wait`, в репозитории `git revert`.

## Проверки

```bash
dotnet test backend/Talkaton.sln          # тесты бэкенда
cd frontend && npm run lint && npx ng test --watch=false && npm run build
```

Те же шаги плюс приёмка `docker compose up` гоняются в GitHub Actions
на каждый PR — см. [.github/workflows/ci.yml](.github/workflows/ci.yml).
Приёмка этапа 2 вынесена в [.github/scripts/smoke-stage-2.sh](.github/scripts/smoke-stage-2.sh):
входит по имени и проверяет, что демо-неделя и витринная встреча на месте.

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
├── frontend/                              Angular 22, тёмная тема
│   ├── src/styles/tokens.scss             токены темы, снятые с макета
│   ├── src/app/core/api/                  единственное место с URL-ами бэкенда
│   ├── src/app/core/session/              вход по имени, заголовок X-Talkaton-User
│   ├── src/app/core/time/                 даты, RRULE в человеческом виде
│   ├── src/app/layout/                    шапка с вкладками Толка
│   ├── src/app/features/login/            экран входа по имени
│   ├── src/app/features/calendar/         сетки дня/недели/месяца/года, панели, редактор
│   ├── src/app/features/reminders/        планировщик напоминаний и тост
│   └── nginx.conf                         прод-раздача + проксирование /api
├── backend/
│   ├── src/Talkaton.Domain/               сущности, RRULE, развёртка вхождений
│   ├── src/Talkaton.Infrastructure/       EF Core, миграции, наполнение при входе
│   ├── src/Talkaton.Api/                  minimal API, Swagger, CORS
│   └── tests/
├── docs/adr/                              решения, которые дорого переигрывать
└── docker-compose.yml
```

## API

Всё под `/api`, Swagger — на `/swagger`. Кроме входа, каждый запрос несёт
заголовок `X-Talkaton-User` с идентификатором из `POST /api/session`.

| Метод    | Путь                          | Зачем                                        |
| -------- | ----------------------------- | -------------------------------------------- |
| `POST`   | `/session`                    | вход по имени, заводит пространство при первом |
| `GET`    | `/session`                    | проверка входа при перезагрузке страницы     |
| `GET`    | `/calendars`                  | блок «Мои календари»                         |
| `PATCH`  | `/calendars/{id}`             | галочка видимости, переименование            |
| `GET`    | `/events?from=&to=&calendarIds=` | вхождения встреч за период                |
| `POST`   | `/events`                     | создание встречи                             |
| `PATCH`  | `/events/{id}`                | правка, перенос drag&drop, resize            |
| `DELETE` | `/events/{id}`                | удаление серии или одного вхождения          |
| `POST`   | `/events/{id}/rsvp`           | идёт / не идёт / возможно                    |
| `GET`    | `/events/{id}/artifacts`      | блок «Артефакты встречи»                     |
| `GET`    | `/users?query=`               | поиск людей для приглашения                  |
| `GET`    | `/participant-lists`          | блок «Списки участников»                     |

У `PATCH` и `DELETE` встречи есть параметр `scope`: `series` (по умолчанию)
правит всю серию, `occurrence` вместе с `occurrenceStart` — одно вхождение.
Подробности — в [ADR 0004](docs/adr/0004-iskliucheniya-vhozhdeniy.md).

## Договорённости

- **Время — только в UTC**, таймзона пользователя хранится отдельным полем.
  Переделывать это позже дороже всего.
- **Повторяемость — по RFC 5545 (RRULE)**, а не самописным форматом: иначе
  синхронизация с Google и ICS превратится в боль. Вхождения разворачиваются
  на чтении, в базу попадают только исключения.
- **Цвета — только через `var(--tk-*)`.** Ни одного HEX в компонентах:
  на этапе склейки с Толком меняются только токены.
- **Фронт ходит в бэкенд только через `TalkatonApi`**, а вход живёт в
  `SessionService`. На этапе 6 SSO Контура заменяет один файл.
- **Весь код, общающийся с Толком, живёт за интерфейсом `ITalkGateway`** (этап 3).
  Это главная страховка проекта.

## Ветвление

`main` — стабильное, фичи — в `feat/*`, вливаются через PR с зелёным CI.
