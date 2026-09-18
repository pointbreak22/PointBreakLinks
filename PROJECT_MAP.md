# PointBreakLinks — карта проекта

Маркетплейс по продаже ссылок/площадок (по образцу link-building бирж вроде GoGetLinks/Miralinks).
Продавцы (вебмастеры) выставляют площадки на продажу, покупатели (оптимизаторы) собирают их в
проекты, покупают размещения, задают анкоры/ссылки и отслеживают публикацию.

Переносится с **Laravel + Vue/Nuxt** (`FOXLinks_Laravel_Vue`) на **.NET 10 + Angular 21**.
Архитектура API и клиента взята по образцу `MessengerAzure` (Clean Architecture + CQRS/MediatR
на бэкенде, feature-based структура на Angular).

## Референсные проекты

| Проект | Роль | Путь |
| --- | --- | --- |
| `FOXLinks_Laravel_Vue` | Источник домена и бизнес-логики (что переносим) | `e:/Projects/Orders/FOXLinks_Laravel_Vue` |
| `MessengerAzure` | Источник архитектурного паттерна (как переносим) | `e:/Projects/My Projects/MessengerAzure` |

## Архитектурные решения (зафиксировано в этой сессии)

| Решение | Выбор | Почему |
| --- | --- | --- |
| СУБД | PostgreSQL (Npgsql.EntityFrameworkCore.PostgreSQL) | Как в MessengerAzure; бесплатна, легко поднимается локально/в Docker |
| Аутентификация | Свой JWT (access + httpOnly refresh-cookie) | Как в FOXLinks (`AuthController`), без внешнего IdP — свой контроль над ролями |
| Real-time | SignalR, хаб `/hubs/notifications` заведён сразу, но пока ничего не пушит | Понадобится для статусов заказов и чата между покупателем/продавцом |
| ORM | EF Core, миграции лежат в `Infrastructure/Persistence/Migrations` | — |
| Docs UI | Scalar (`/scalar/v1`), как в MessengerAzure, а не Swagger UI | — |
| Identity | Отдельные проекты `Identity.Domain`/`Identity.Application`/`Identity.Infrastructure` + отдельная Postgres-схема `identity` в той же физической БД | По прямой просьбе пользователя — "чтобы если данные сайта сотрутся, пользователи остались". Схема, а не отдельная база: FK-constraints между бизнес-таблицами и `identity.users` продолжают работать (Postgres FK переживают `ALTER TABLE ... SET SCHEMA`), в отличие от варианта с реально отдельной БД, где FK пришлось бы убирать. См. отдельный раздел ниже |

Эти решения легко пересмотреть — просто скажи, что поменять.

## Identity — отдельный модуль (по просьбе пользователя, эта сессия)

**Зачем**: пользователь захотел вынести регистрацию/авторизацию в отдельный проект вроде
"IdentityServer" — отдельные Domain/Application/Infrastructure для Auth, и чтобы учётные записи
не пропадали при очистке/пересборке бизнес-данных. Перед реализацией обсуждались два варианта —
отдельная Postgres-схема (FK продолжают работать) vs по-настоящему отдельная база (FK пришлось бы
убирать почти по всему приложению, `User.Id` referenced буквально everywhere: `Project.UserId`,
`Site.SellerId`, `PurchasedSite.BuyerId`, `Message.SenderId/RecipientId`, `Notification.UserId`,
`BalanceTransaction.UserId`) — выбран первый, пользователь прямо попросил попробовать.

**Структура** — три новых проекта в `.slnx`, зеркалящие стиль существующих Domain/Application/
Infrastructure, но полностью независимые от бизнес-кода (Identity.Domain не ссылается вообще ни
на что бизнесовое — ни на `Domain`, ни на `Application`; так и было задумано, чтобы Identity
реально мог жить отдельно):

```
Identity.Domain/         — User (Id/Name/Email/PasswordHash/IsBanned), Role, RefreshToken,
                            PasswordResetToken, RoleNames (переехал сюда из Domain/Constants —
                            роль это концепция identity, не бизнеса), свои BaseEntity/exceptions
                            (NotFoundException/ConflictException/AuthenticationException —
                            продублированы, а не переиспользованы из бизнес Domain, чтобы не
                            тянуть зависимость)
Identity.Application/    — CQRS/Auth: Register, Login, RefreshToken, Logout, GetCurrentUser,
                            ChangePassword, ForgotPassword, ResetPassword — перенесены как есть
                            из Application/CQRS/Auth, только namespace. IPasswordHasher,
                            IJwtTokenGenerator, TokenIssuer, PasswordPolicy — тоже сюда.
                            UserDto больше НЕ содержит Balance (это бизнес/Wallet-концепция —
                            см. ниже, как баланс попадает в ответ)
Identity.Infrastructure/ — IdentityDbContext (HasDefaultSchema("identity"), СВОЯ отдельная
                            история миграций — identity.__EFMigrationsHistory, а не общая с
                            бизнес-контекстом), EfUserRepository/EfRefreshTokenRepository/
                            EfPasswordResetTokenRepository, BcryptPasswordHasher,
                            JwtTokenGenerator, JwtSettings, DependencyInjection
                            (AddIdentityInfrastructure) — коннекшн-строка читается из
                            "IdentityConnection" с фолбэком на "DefaultConnection", так что
                            указать реально отдельную БД позже — это правка одной строки в
                            appsettings, без единой правки кода
```

**Бизнес-сторона сохраняет свою собственную, отдельно замапленную копию `User`** (`Domain/
Entities/User.cs`: Id/Name/Email/IsBanned/Roles/Projects — без `PasswordHash`, без `Balance`),
указывающую на ТУ ЖЕ таблицу `identity.users`. Ключевой приём —
`builder.ToTable("users", "identity", t => t.ExcludeFromMigrations())` в бизнесовом
`UserConfiguration`/`RoleConfiguration`: это не даёт бизнес-контексту генерировать
DDL (CREATE/ALTER/DROP) для этой таблицы (её схему полностью и единолично владеет Identity), но
не мешает читать/писать в неё в рантайме — `AdminController`'s ban/role-management как работал
через `Application/CQRS/Admin`, так и работает, без единой правки логики, просто через другой,
более узкий `IUserRepository` (бизнесовый), у которого остались только методы, реально нужные
Admin'у (`GetAllPaginatedAsync`, `GetByIdWithRolesAndProjectsAsync`, `GetRoleByNameAsync`) — все
Auth-специфичные методы (`GetByEmailAsync`, `EmailExistsAsync`, `AddAsync`) переехали в
Identity-свой `IUserRepository` (другой интерфейс, другой namespace, тот же паттерн, что и с
`Domain.Exceptions` vs `Identity.Domain.Exceptions`).

**`Wallet` — новая сущность, `Balance` съехал с `User`.** Баланс — это концепция маркетплейса
(сколько пользователь может потратить/сколько заработал), а не identity — держать его на
Identity-owned таблице означало бы, что бизнес-стороне нужен write-доступ к колонке, которой
Identity формально владеет. `Wallet { UserId, Balance }` — создаётся лениво при первом обращении
(`IWalletRepository.GetOrCreateAsync`), поскольку регистрация теперь в Identity и ничего не знает
про Wallet. `RequestPublicationCommandHandler`/`AcceptOrderCommandHandler`/
`TopUpBalanceCommandHandler`/`GetMyBalanceQueryHandler` — все переключены с `IUserRepository` на
`IWalletRepository`. Раз `UserDto` (Identity) больше не содержит `Balance`, `AuthController` сам
компонует финальный `user`-JSON на HTTP-границе — единственное уместное место соединять оба
bounded context для одного ответа: после `mediator.Send(RegisterCommand/LoginCommand/...)`
(Identity) отдельно вызывает `mediator.Send(GetMyBalanceQuery)` (бизнес, тот же `IMediator`,
т.к. MediatR сканирует обе Application-сборки) и склеивает поля вручную. Формат ответа
(`user.balance`) не изменился — фронтенду ничего менять не пришлось.

**`DomainExceptionHandler` теперь матчит по имени типа исключения** (`exception.GetType().Name`),
а не по паттерн-матчингу конкретного типа — у business Domain и Identity.Domain теперь СВОИ,
разные классы `NotFoundException`/`ConflictException`/`AuthenticationException`, и обработчик
должен узнавать оба набора, не ссылаясь на Identity.Domain напрямую (WebAPI и так уже
ссылается на оба через контроллеры, но сам обработчик написан так, будто bounded context'ов
может быть сколько угодно).

**Перенос существующих данных — физический, не «drop и пересоздать».** Postgres FK-constraints
не привязаны к schema-qualified имени таблицы на уровне хранения — они трекают её по OID, так
что `ALTER TABLE users SET SCHEMA identity;` не требует ни удаления, ни пересоздания ни единого
внешнего ключа из `projects`/`sites`/`purchased_sites`/`messages`/`notifications`/
`balance_transactions`/etc. Миграция `Infrastructure`'s `SplitIdentitySchema` (сгенерирована
`dotnet ef migrations add`, затем вручную отредактирована — авто-сгенерированный вариант пытался
`DeleteData` 5 seed-строк ролей, посчитав их "больше не нужными" бизнес-стороне; это неверно, они
просто переезжают, а не удаляются) делает: `DROP TABLE password_reset_tokens/refresh_tokens`
(без сохранения данных — обычные refresh-токены и одноразовые reset-ссылки, ничего ценного),
`CREATE SCHEMA identity; ALTER TABLE users/roles/role_user SET SCHEMA identity;` (все 40+
существующих пользователей и их роли сохранились байт-в-байт), `CREATE TABLE wallets`,
бэкфилл `INSERT INTO wallets SELECT "Id", "Balance", ... FROM identity.users`, затем
`ALTER TABLE identity.users DROP COLUMN "Balance"`. `Identity.Infrastructure`'s `InitialCreate`
migration (тоже вручную обрезана после генерации) создаёт ТОЛЬКО `password_reset_tokens`/
`refresh_tokens` — `users`/`roles`/`role_user` уже физически на месте после бизнес-миграции,
переприменять их `CreateTable`/`InsertData` было бы ошибкой ("relation already exists").
Применяются строго по очереди: сначала бизнес-миграция (создаёт схему `identity` и переносит
туда таблицы), потом Identity-миграция (создаёт то, чего физически ещё не было).

**Проверено вживую после переноса** (curl, реальная БД): логин старым, созданным ДО разделения,
аккаунтом — баланс совпадает 1:1 с тем, что было до переноса; `POST /api/auth/register` создаёт
нового пользователя через Identity; `GET /api/admin/users`/смена роли/бан — читают и пишут через
бизнес-сторону в ту же `identity`-таблицу, и Identity's `LoginCommandHandler` сразу видит эффект
(забаненный только что через Admin пользователь не может залогиниться — доказывает, что оба
контекста реально смотрят в одни и те же физические данные, не в копии); полный цикл покупка →
списание с покупателя → принятие заказа → зачисление продавцу — суммы сходятся. Плюс браузер
(Playwright): регистрация, редирект на `/projects`, `/wallet` показывает баланс — фронтенд не
менялся ни строкой, работает как есть, потому что JSON-контракт (`user.balance` и все остальные
поля) не изменился ни на бит.

## Архитектура API (Clean Architecture + CQRS)

```
Api/PointbreakLinksApi/
├── Domain/            — бизнес-сущности, enum'ы, интерфейсы репозиториев. Без зависимостей
│   │                     (в т.ч. без зависимости на Identity.Domain — см. отдельный раздел
│   │                     "Identity — отдельный модуль" выше за User/Role/RefreshToken/Auth).
│   ├── Common/         BaseEntity (Id, CreatedAt, UpdatedAt)
│   ├── Entities/        User (тонкая, отдельно замапленная копия identity.users — Id/Name/
│   │                     Email/IsBanned/Roles/Projects, без PasswordHash/Balance), Role (та же
│   │                     история), Wallet (Balance съехал сюда с User), Country, Topic, Status,
│   │                     PaymentSetting, Site, Project, PurchasedSite, Link, DynamicStat,
│   │                     Message, Notification, BalanceTransaction, PurchasedSiteEvent
│   ├── Enums/            InsuranceType, BalanceTransactionType
│   ├── Exceptions/       NotFoundException, ForbiddenException, AuthenticationException,
│   │                      ConflictException — свои классы, отдельные от Identity.Domain.Exceptions
│   │                      (DomainExceptionHandler в WebAPI матчит оба набора по имени типа)
│   └── Repositories/     IUserRepository (только Admin-нужды: пагинация/роли/проекты — Auth-
│                          методы уехали в Identity.Domain.Repositories.IUserRepository),
│                          IWalletRepository, остальные заводятся вместе с фичей
│
├── Application/        — use-case'ы через MediatR (Commands/Queries), ссылается только на Domain.
│   └── CQRS/
│       ├── Sites/        ✅ продавец: CreateSite, UpdateSite, DeactivateSite, GetMySites,
│       │                    GetSiteById, GetTopics, GetCountries, AcceptOrder, ConfirmPublished,
│       │                    GetWebmasterSales, GetProjectSites, VerifySite (новое — см. ниже).
│       │                    ✅ покупатель: GetCatalog (`GET /all-sites`, теперь с реальными
│       │                    фильтрами topic/country/price/iks/dr — см. `SiteCatalogFilter`),
│       │                    RequestPublication (`POST /sites/{id}/request-publication`)
│       ├── Stats/        ✅ GetDynamicStats(pageKey) — виджеты дашборда, авто-обновление
│       │                    счётчика проектов при create/delete (см. IDynamicStatsRefresher)
│       ├── Messages/     ✅ GetMessages, SendMessage — чат по заказу (реальный, не в FOXLinks —
│       │                    см. "Известные ограничения")
│       ├── Projects/     ✅ CreateProject, UpdateProject, DeleteProject, GetMyProjects,
│       │                    GetProjectById
│       ├── Admin/        ✅ GetUsers, SetUserBanned, SetUserRole, GetDashboard — new module, not
│       │                    a port (см. AdminUserDto/AdminDashboardDto: у FOXLinks нет admin-бэкенда вообще)
│       ├── Moderation/   ✅ GetPendingSites, ApproveSite, RejectSite — тоже новый модуль: роль
│       │                    `moderator` существовала в seed-данных FOXLinks, но нигде не
│       │                    использовалась (ни бэкенда, ни страницы). Реальный пробел: Site
│       │                    создавался сразу с `IsActive = true`, то есть непроверенное
│       │                    объявление сразу было видно в каталоге покупателя — исправлено,
│       │                    теперь `IsActive = false` до одобрения (см. "Известные ограничения")
│       ├── Favorites/    ✅ AddFavorite, RemoveFavorite, GetMyFavorites, GetFavoriteSiteIds —
│       │                    новый модуль. FOXLinks' "Избранные площадки" — везде ссылки на
│       │                    /coming-soon, ничего не было реализовано
│       └── Reviews/      ✅ CreateReview, GetSiteReviews — новый модуль. Отзыв разрешён только
│                             после `PurchasedSite.IsPublished = true` (см. ConfirmPublished — у
│                             него не было фронтенд-триггера вообще, см. "Известные ограничения")
│                             и только один отзыв на заказ (уникальный индекс по PurchasedSiteId,
│                             не по паре покупатель+площадка)
│
├── Infrastructure/     — реализации: EF Core, репозитории бизнес-сущностей. Больше не содержит
│   │                     JWT/хэширование паролей — уехало в Identity.Infrastructure.
│   ├── Persistence/      ApplicationDbContext (DbSet<User>/<Role> — обе ExcludeFromMigrations,
│   │                      схема "identity", свой набор миграций в public-схеме и её
│   │                      __EFMigrationsHistory, независимый от Identity), Configurations/*
│   │                      (+ seed-данные: Status, Topic, Country — Role.HasData уехал в
│   │                      Identity.Infrastructure), Migrations/
│   ├── Repositories/     EfUserRepository (только Admin-нужды), EfWalletRepository,
│   │                      EfSiteRepository, EfPurchasedSiteRepository, EfTopicRepository,
│   │                      EfDynamicStatRepository, EfMessageRepository, EfProjectRepository,
│   │                      EfCountryRepository, EfFavoriteSiteRepository, EfSiteReviewRepository
│   ├── Services/         DynamicStatsRefresher (пересчитывает DynamicStat при изменениях — порт
│   │                      Laravel Observers, см. Application/Common/IDynamicStatsRefresher.cs),
│   │                      HttpSiteVerificationService (проверка владения площадкой —
│   │                      SSRF-защищённый HTTP-клиент с ручной обработкой редиректов и
│   │                      проверкой каждого хопа на приватные диапазоны IP)
│   └── DependencyInjection.cs
│
├── Identity.Domain/ , Identity.Application/ , Identity.Infrastructure/ — отдельный, независимый
│                     от бизнес-кода модуль авторизации (см. "Identity — отдельный модуль" выше
│                     за полное описание): User/Role/RefreshToken/PasswordResetToken, Register/
│                     Login/RefreshToken/Logout/GetCurrentUser/ChangePassword/ForgotPassword/
│                     ResetPassword, IdentityDbContext (схема "identity", своя история миграций)
│
└── WebAPI/             — HTTP-слой, ссылается и на бизнес Application/Infrastructure, и на все
    │                     три Identity.* проекта.
    ├── Controllers/      ApiControllerBase (GetCurrentUserId из claim "sub"), AuthController
    │                      (команды/запросы — из Identity.Application; баланс в ответ
    │                      подмешивается отдельным вызовом в бизнес Application — см. выше),
    │                      SitesController, StatsController, MessagesController, ProjectsController,
    │                      WalletController, AnalyticsController,
    │                      AdminController ([Authorize(Roles = "admin")] — RoleNames теперь из
    │                      Identity.Domain.Constants), ModerationController
    │                      ([Authorize(Roles = "moderator,admin")]),
    │                      FavoritesController, ReviewsController, NotificationsController
    ├── Middleware/        DomainExceptionHandler — Domain-исключения (NotFound/Conflict/
    │                       Authentication/Forbidden) → правильный HTTP-статус + ProblemDetails,
    │                       в одном месте вместо try/catch в каждом action. Матчит по имени типа
    │                       исключения (не по паттерну конкретного класса) — у business Domain и
    │                       Identity.Domain свои, разные классы с этими именами
    ├── Hubs/              NotificationHub — теперь реально пушит 4 события (см.
    │                       Application/Common/INotificationPusher.cs): новый заказ продавцу,
    │                       заказ принят покупателю, новое сообщение в чате получателю, площадка
    │                       одобрена/отклонена продавцу
    ├── Services/          CustomUserIdProvider (SignalR user-id из JWT sub — исправлен в этой
    │                       сессии, см. "Известные ограничения"), SignalRNotificationPusher
    │                       (реализация INotificationPusher поверх IHubContext<NotificationHub>;
    │                       живёт в WebAPI, а не в Infrastructure, потому что сам Hub — тип WebAPI)
    └── Program.cs         JWT Bearer, CORS (AllowCredentials — нужен для refresh-cookie),
                            MediatR (регистрирует ДВЕ сборки — бизнес Application и
                            Identity.Application), EF Core (AddInfrastructure +
                            AddIdentityInfrastructure — два независимых DbContext), Serilog,
                            Scalar, SignalR, DomainExceptionHandler
```

**Каждая `_NEXT.md`** в `Application/CQRS/<Модуль>/` — это конспект: какой контроллер/модели в
FOXLinks смотреть, какие CQRS-команды/запросы туда напрашиваются. Начинай с них, когда будем
переписывать конкретный модуль.

### Паттерн пагинации (используется в Sites, пригодится для Projects/Optimizator)

Репозитории берут `page`/`perPage` и сами делают `Skip`/`Take`/`Count` (EF Core остаётся
только в Infrastructure — Application ссылается только на Domain), возвращая
`(IReadOnlyList<T> Items, int Total)`. Query-хендлер оборачивает это в
`Application.Common.PagedResult<TDto>.Create(items, total, page, perPage)` — та же форма JSON,
что была у FOXLinks' `PaginationService` (`{ items, currentPage, lastPage, perPage, total }`),
так что Angular-сторона (`Pagination`-компонент, порт `components/pagination.vue`) не менялась.

### Модель домена (перенесена из миграций FOXLinks 1:1)

| Laravel таблица | .NET сущность | Заметки |
| --- | --- | --- |
| `identity.users` | `Identity.Domain.Entities.User` (полная, с `PasswordHash`) + `Domain.Entities.User` (тонкая копия, для бизнес-стороны) | Схема `identity`, не `public` — см. "Identity — отдельный модуль" выше. + связь many-to-many с `Role` через `role_user`. `IsBanned` (bool) реально проверяется при логине/рефреше (см. Admin-модуль). `Balance` съехал на `wallets` |
| `identity.roles` / `identity.role_user` | `Identity.Domain.Entities.Role` (владеет seed-данными, `RoleConfiguration.HasData`) + `Domain.Entities.Role` (тонкая копия) | Схема `identity` |
| `identity.refresh_tokens` | `Identity.Domain.Entities.RefreshToken` | Схема `identity`, только в Identity-модуле. Один активный токен на пользователя (как в FOXLinks — вход с нового устройства инвалидирует старый) |
| `wallets` | `Wallet` | Новая таблица (не из FOXLinks), схема `public` — `UserId` + `Balance`. Создаётся лениво при первом обращении к кошельку |
| `countries` | `Country` | — |
| `topics` | `Topic` | — |
| `statuses` | `Status` | ⚠️ Общая lookup-таблица для статуса модерации Site **и** статуса заказа PurchasedSite — так было в исходнике. Плюс 6-я строка `"active"`/"Активна", добавленная в этой сессии (не из FOXLinks) — статус одобренной модератором площадки, см. Moderation-модуль. Стоит пересмотреть на два отдельных enum'а при переносе Sites. |
| `payment_settings` | `PaymentSetting` | `insurance_type` строка → enum `InsuranceType` |
| `sites` | `Site` | Каталог площадок на продажу. `VerificationToken`/`IsVerified` добавлены в этой сессии — не из FOXLinks, см. Site-модуль ниже |
| `projects` | `Project` | Проект покупателя, агрегирует расходы |
| `purchased_sites` | `PurchasedSite` | Заказ: конкретное размещение на Site в рамках Project |
| `links` | `Link` | Анкор+URL внутри заказа |
| `dynamic_stats` | `DynamicStat` | Виджеты дашборда (webmaster/optimizator/project) |
| `messages` | `Message` | Чат, привязан к `PurchasedSiteId`, а не общий DM |
| — | `FavoriteSite` | Новая таблица (не из FOXLinks) — buyer↔site, уникальный индекс на паре |
| — | `SiteReview` | Новая таблица (не из FOXLinks) — 1:1 с `PurchasedSite` (уникальный индекс на `PurchasedSiteId`), не N:1 с покупателем+площадкой |

### Роли (из seed-миграции FOXLinks)

`admin`, `moderator`, `webmaster` (продавец), `optimizer` (покупатель), `universal`
(дефолтная роль при саморегистрации).

### Auth-флоу (перенесён 1:1 из `AuthController.php`)

1. `POST /api/auth/register` / `/login` → JSON `{ access_token, user }` + httpOnly cookie
   `refresh_token` (30 дней).
2. `POST /api/auth/refresh` — читает cookie, продлевает **тот же** refresh-токен (не ротирует
   значение, только срок), возвращает новый access-токен.
3. `POST /api/auth/logout` — удаляет refresh-токен из БД и cookie.
4. `GET /api/me` — текущий пользователь по access-токену.

Access-токен живёт в памяти на клиенте (сигнал, не localStorage) — см. `UserStore`.

Refresh-cookie выставляется с `SameSite=None; Secure` (не `Lax`, как было изначально) —
клиент и API живут на разных origin'ах (разные порты локально; скорее всего разные поддомены
в проде, как у `MessengerAzure`), а `Lax`-куки не отправляются на cross-origin fetch/XHR
вообще — только при top-level navigation. С `Lax` `/auth/refresh` молча получал 401 при каждом
обращении с фронта, и сессия ни разу не восстанавливалась после жёсткой перезагрузки.

## Архитектура клиента (Angular 21, standalone + signals)

```
Client/pointbreak-links-client/src/app/
├── core/
│   ├── auth/            auth.service.ts (register/login/refresh/logout/me, `sessionReady`
│   │                     promise — см. ниже), auth.guard.ts (authGuard, guestGuard, roleGuard —
│   │                     все async, ждут `sessionReady` перед проверкой)
│   ├── http/             api-endpoints.ts, jwt-auth.interceptor.ts (Bearer + retry-on-401),
│   │                      api-error.ts (вытаскивает `detail` из ProblemDetails-ответа API)
│   ├── theme/            ThemeService (light/dark, localStorage), PaletteService
│   │                      (route `data.palette` → `body[data-palette]`, см. ниже)
│   ├── notifications/    ToastService (глобальный сигнал-стор, один <app-toast/> в app.html)
│   ├── navigation/       nav-items.ts (конфиг пунктов меню Header с флагом `implemented`)
│   ├── layout/           SidebarService (свёрнут/развёрнут — глобальный флаг, как
│   │                      `useSidebar.js` в исходнике)
│   ├── models/            TS-интерфейсы 1:1 с Application/CQRS/*/DTOs/*.cs (camelCase —
│   │                      ASP.NET Core сериализует так по умолчанию)
│   └── signalr/          notification-hub.service.ts (обёртка над HubConnection) +
│                          notifications-bootstrap.service.ts (подключает хаб как только
│                          появляется сессия — `effect()` на `UserStore.isAuthenticated()` —
│                          и показывает 4 события через ToastService; инстанцируется один раз из
│                          `app.config.ts`'s инициализатора)
├── stores/               user.store.ts, sites.store.ts, stats.store.ts, projects.store.ts,
│                          favorites.store.ts — см. `stores/_NEXT.md`
├── services/             sites-api.service.ts, stats-api.service.ts, messages-api.service.ts,
│                          projects-api.service.ts, favorites-api.service.ts,
│                          reviews-api.service.ts — см. `services/_NEXT.md`
├── shared/
│   ├── layout/header/, layout/footer/, layout/sidebar/   ✅ общий шелл на каждой странице
│   ├── login-modal/                      ✅ вход-модалка, открывается из Header
│   ├── toast/                            ✅ глобальный тост
│   ├── pagination/                       ✅ порт `components/pagination.vue`
│   ├── smart-grid/                       ✅ порт `components/smart-grid.vue` (виджеты DynamicStat)
│   ├── message-chat-modal/               ✅ реальный чат (не фейковые данные исходника — см.
│   │                                       "Известные ограничения"); переиспользуется и
│   │                                       Вебмастером (my-sales), и Проектами (project-details)
│   └── directives/fade-in-on-scroll.ts   ✅ IntersectionObserver вместо ручного
│                                           scroll-листенера из исходника
├── pages/
│   ├── landing/                       ✅ публичный лендинг (index.vue)
│   ├── auth/login/, auth/register/    ✅
│   ├── coming-soon/                   ✅ порт temp-unavailable.vue — цель большинства ссылок,
│   │                                    которые пока никуда не ведут
│   ├── webmaster/                     ✅ продавец: webmaster.ts (переключатель вкладок) +
│   │                                    my-platforms/ (+ проверка владения площадкой — кнопка
│   │                                    «Проверить», инструкции с токеном, бейдж «Активна»/
│   │                                    рейтинг), my-sales/, add-edit-site-modal/,
│   │                                    order-task-modal/ (+ «Подтвердить публикацию» — см.
│   │                                    "Известные ограничения")
│   ├── projects/                      ✅ покупатель: project-list/, project-details/ (+ «Оставить
│   │                                    отзыв» через leave-review-modal/, когда заказ
│   │                                    опубликован и ещё не оценён), create-edit-project-modal/
│   │                                    — теперь стартовая страница после логина (заменила
│   │                                    старую заглушку `/app`)
│   ├── optimizator/                   ✅ покупатель: optimizator.ts (каталог + реальный
│   │                                    клиентский поиск — единственная живая фича из
│   │                                    исходных фильтров + реальная панель фильтров
│   │                                    тематика/страна/цена/ИКС/DR, звёздочка «в избранное»,
│   │                                    рейтинг и бейдж подтверждённого владения на карточке
│   │                                    площадки) + buy-miralinks-modal/ (3 вкладки:
│   │                                    Ссылки/Настройки/Итоги; вкладка «Итоги» считает
│   │                                    реальные значения, а не хардкод исходника — см.
│   │                                    "Известные ограничения")
│   ├── favorites/                     ✅ новая страница, не порт — см. ниже
│   ├── admin/                         ✅ admin-dashboard/ + admin-users/ — новый модуль, не порт
│   │                                    (FOXLinks' admin-*.vue — локальные mock-данные без
│   │                                    бэкенда; см. AdminUserDto/AdminDashboardDto). Дашборд:
│   │                                    5 реальных агрегатов (пользователи/проекты/активные
│   │                                    площадки/заказы/оборот), без графиков и без выдуманных
│   │                                    трендов. Пользователи: список, смена роли, бан/разбан.
│   │                                    Оба — только под `roleGuard('admin')`
│   ├── moderation/                     ✅ новый модуль, не порт — роль `moderator` существовала
│   │                                    в seed-данных FOXLinks, но нигде не использовалась.
│   │                                    Очередь площадок на проверке (URL/продавец/тематика/
│   │                                    цена/дата) + Одобрить/Отклонить. `roleGuard('moderator',
│   │                                    'admin')` — первое использование гварда с несколькими ролями
│   └── _NEXT.md                       ⏳ соответствие остальных страниц Nuxt-фронтенда
├── app.config.ts         provideHttpClient + interceptor, provideAppInitializer
│                          (тихий silent-refresh + PaletteService.init(), только в браузере —
│                          SSR-рендер не имеет доступа к httpOnly cookie)
└── app.routes.ts          '' → Landing (публично), /login, /register, /projects, /projects/:id,
                            /webmaster, /optimizator, /favorites, /moderation, /admin-dashboard,
                            /admin-users, /coming-soon (все девять — authGuard, /moderation и
                            /admin-* вдобавок roleGuard)
```

SSR (`@angular/ssr`) уже был в скаффолде — оставлен для публичных страниц (`''`/`login`/
`register`), но всё, что за авторизацией, теперь рендерится только на клиенте
(`RenderMode.Client` в `app.routes.server.ts`) — см. "Известные ограничения" за тем, почему это
не просто оптимизация, а исправление реального бага с редиректами.

### Система палитр (перенесена и централизована)

В FOXLinks у каждой страницы был свой CSS-файл в `assets/css/palettes/`, переопределяющий
единый набор CSS-переменных (`--primary`, `--bg-card`, `--gradient`, `--shadow`,
`--border-radius`...), которые использовали общие классы компонентов в `main.css`
(`.primary-btn`, `.stat-card`, `.table-row`, ...). Переключение — атрибут `data-palette` на
`<body>`, который каждая Vue-страница выставляла сама через `useHead()`.

Перенесено 1:1 в `Client/.../src/styles/`:
- `design-system.css` — общие классы компонентов (кнопки/карточки/таблицы/бейджи/модалки/
  формы/пагинация), порт `main.css`. Используются на любой странице через обычные CSS-классы.
- `palettes/*.css` — все 8 палитр (`index`, `register`, `webmaster`, `optimizator`, `project`,
  `admin`, `analytics`, `position`), включая тёмную тему (`.dark-theme`) там, где она была в
  исходнике.

**Улучшение относительно исходника**: переключение палитры теперь не разбросано по каждой
странице, а централизовано в `PaletteService` — он слушает `Router` и берёт `data.palette` из
конфига маршрута (`app.routes.ts`). Добавить новую страницу с палитрой = одна строка
`data: { palette: '...' }` в роуте, а не код в компоненте.

**Для страничной/одноразовой вёрстки** (не переиспользуемой на многих страницах — герои,
сетки карточек, шаги процесса) — используются Tailwind-утилиты прямо в шаблоне компонента, а
не новые CSS-классы. Токены палитры при этом доступны через arbitrary values/переменные
Tailwind v4: `rounded-(--border-radius)`, `style="background: var(--gradient)"` и т.п. — так
и сохраняется единый дизайн без раздувания стилями каждого компонента (см. `pages/landing/`
как пример).

## Что уже готово и проверено

- API собирается (`dotnet build`) и генерирует валидные EF Core-миграции на полной модели
  домена (`InitialCreate`, `AddSitesStatsMessagesModuleSeeds`).
- **`Application.Tests`** (xUnit + Moq, `dotnet test` из `Api/PointbreakLinksApi/`) — до этого
  вся проверка на протяжении всей сессии была ручной (curl + Playwright), без единого
  автотеста, что для кодовой базы с таким количеством тонких бизнес-правил (см. "Известные
  ограничения" → "исправлено" — почти все найденные баги были именно в этом слое) оставляло
  ноль защиты от регрессии при следующей правке. 21 тест на обработчики, где правило однажды
  уже ломалось молча или могло бы: `OpenDisputeCommandHandler` (спор только по заказу в статусе
  «В работе», нельзя открыть дважды), `CreateSiteCommandHandler` (новая площадка стартует
  `IsActive=false` — регрессия сюда уже случалась один раз, см. выше), `Approve`/
  `RejectSiteCommandHandler` (переход статуса, аудит-запись, уведомление ровно один раз на
  покупателя даже при нескольких совпавших сохранённых поисках), и симметричная пара
  `RequestPublicationCommandHandler`/`AcceptOrderCommandHandler` (списание у покупателя при
  создании заказа, зачисление продавцу только при принятии — именно та последовательность,
  которую комментарии в обоих файлах объясняют как обязательную). Один тест-мутацию прогнали
  вручную (временно вернули `IsActive=true` в `CreateSiteCommandHandler`) — тест немедленно
  упал, подтверждая, что проверки реальные, а не тавтологичные. Позже дополнено 16 тестами на
  Query-обработчики (37 всего): `GetAnalyticsQueryHandler` (форматирование месяца без
  off-by-one на границах года), `GetProjectSitesQueryHandler` (проверка владения проектом —
  тот самый баг, которого не было у FOXLinks), `GetPurchasedSiteEventsQueryHandler` (таймлайн
  заказа виден и покупателю, и продавцу — попытка buyer-lookup, затем seller-lookup, а не
  единственная проверка роли), `GetMessagesQueryHandler` (открытие чата помечает прочитанными
  только чужие непрочитанные сообщения и не трогает БД, если нечего помечать), и
  `GetSellerProfileQueryHandler` (забаненный продавец — 404, а не пустой профиль).
- **`WebAPI.IntegrationTests`** (xUnit + `WebApplicationFactory` + `Testcontainers.PostgreSql`,
  `dotnet test WebAPI.IntegrationTests` из `Api/PointbreakLinksApi/`, нужен только Docker —
  контейнер поднимается и мигрируется автоматически) — единственный слой, который реально
  собирает ВЕСЬ стек (DI, оба DbContext против настоящего Postgres, JWT, middleware) и который
  `Application.Tests`' моки принципиально не могут проверить. Сразу же нашёл два реальных бага:
  - **`SiteReverificationJob` (BackgroundService) валит весь хост, если стартует раньше
    миграций** — на пустой БД его собственный запрос падает на "relation does not exist", а
    `HostOptions.BackgroundServiceExceptionBehavior = StopHost` останавливает ВЕСЬ хост из-за
    этого одного фонового сервиса. В проде это не проявляется (миграции всегда применены до
    старта), но сам факт, что один непрофильный BackgroundService может убить хост целиком —
    стоит иметь в виду. Тестовая фабрика обходит это, применяя миграции через отдельные,
    вручную созданные экземпляры DbContext ДО того, как реальный хост (и его фоновые сервисы)
    вообще запускается.
  - **`Jwt:Secret` читался в `Program.cs` двумя РАЗНЫМИ способами** — `AddJwtBearer`'s
    `TokenValidationParameters` строился из `builder.Configuration.GetSection("Jwt").
    Get<JwtSettings>()`, вызванного ЖЁСТКО и НЕМЕДЛЕННО в этой строке кода, а
    `JwtTokenGenerator` (подписывающий токен) получает те же настройки через
    `IOptions<JwtSettings>` — ленивое связывание, разрешаемое при первом обращении, УЖЕ после
    того как финальная конфигурация собрана. В обычном запуске оба пути читают ОДИН И ТОТ ЖЕ
    единственный источник конфигурации и совпадают случайно; но `WebApplicationFactory`
    добавляет override поверх конфигурации ПОСЛЕ того как этот код в `Program.cs` уже
    выполнился — из-за чего подпись токена уходила с одним секретом, а валидация — с другим
    (взятым из `appsettings.json`/user-secrets), и `/api/me` стабильно падал 401 сразу после
    успешного `/register`. Исправлено: `AddJwtBearer()` без опций + `services.
    AddOptions<JwtBearerOptions>(...).Configure<IOptions<JwtSettings>>(...)` — теперь оба пути
    читают `JwtSettings` одинаково, лениво, через один и тот же `IOptions<JwtSettings>`. Не
    просто артефакт тестовой инфраструктуры: тот же разъезд смог бы произойти в проде, добавь
    кто-нибудь конфигурацию (Key Vault, доп. `appsettings.*.json` и т.п.) уже ПОСЛЕ этой строки.
- **GitHub Actions CI** (`.github/workflows/ci.yml`) — backend (`dotnet build`/`test` по
  `.slnx`) и frontend (`tsc --noEmit` → `ng lint` → `ng test` → `ng build --configuration
  production`) на каждый push/PR в `main`.
- **ESLint** (`@angular-eslint`, `ng lint`) — раньше в клиенте не было линтера вообще, только
  `tsc`. Первый прогон дал 74 ошибки; 18 (`label-has-associated-control` в
  `system-settings.html` и двух модалках) — реальные пропущенные `for`/`id` у полей форм,
  исправлены. Остальные 56 — два паттерна, осознанно и последовательно используемых по всему
  приложению, а не баги: `close = output<void>()` почти во всех модалках (стилистическое
  предупреждение о совпадении с нативным DOM-событием `close`, само по себе не проблема — ни
  один компонент не наследует `<dialog>`) и клик по `.modal-overlay` для закрытия по клику вне
  модалки (у каждой модалки уже есть отдельная, полностью доступная с клавиатуры кнопка «×» —
  делать сам оверлей фокусируемым не решает ничего реального). Обе категории отключены точечно
  в `eslint.config.js` с комментарием-обоснованием, а не тихо проигнорированы.
- Известная уязвимость `Microsoft.OpenApi` 2.0.0 (NU1903, high severity, висела в
  предупреждениях сборки с самого начала) закрыта — версия закреплена на патченной `2.12.2`.
  После апгрейда вручную проверены оба потребителя (`/openapi/v1.json` — 96 путей, `openapi:
  3.1.1`, и `/scalar/v1` — 200 OK), сборка теперь с нулём предупреждений.
- **`/health`** — проверяет обе независимые DbContext (бизнес-схема `public` + Identity-схема
  `identity`), раньше эндпоинта не было вообще. **HSTS** включён для не-`Development`
  окружений (в dev сломал бы обычный `http://localhost` из-за самоподписанного сертификата).
  Базовые security-заголовки (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`)
  теперь на каждом ответе — без CSP, так как это чистый JSON API для отдельного Angular-origin,
  не сервер, рендерящий свой HTML, так что ограничивать inline-script/style нечего.
- **Senior-level аудит бэкенда** (по явному запросу) нашёл и закрыл три реальных, а не
  косметических, пробела:
  - **`AsNoTracking()`** — ни один read-only запрос (каталог, дашборды, листинги) не был
    помечен, EF Core снимал снапшот каждой строки для change-tracking без всякой нужды.
    Добавлено во всех 24 репозиториях, но ТОЛЬКО там, где метод проверен по каждому вызывающему
    коду и никогда не мутируется после чтения — иначе `SaveChangesAsync` на untracked-сущности
    молча ничего бы не сохранил (тихая потеря данных, без исключения). Например,
    `ISiteRepository.GetByIdAsync`/`GetByIdForOwnerAsync` (используются
    Approve/Reject/Update/Deactivate/VerifySiteCommandHandler) остались tracked намеренно.
  - **`AsSplitQuery()`** — `IPurchasedSiteRepository`'s `IncludeAll()` джойнил три независимые
    one-to-many коллекции (`Site.Reviews`, `Links`, `Messages`) в одном запросе без
    разделения — классический cartesian explosion (заказ с 3 сообщениями и 2 ссылками
    возвращался как 6 дублированных строк для клиентской дедупликации).
  - **Единый слой валидации** — раньше проверки входных данных были россыпью ручных
    `if (...) throw new ConflictException(...)` по обработчикам, а `RegisterCommand` вообще не
    проверялся (пустое имя/некорректный email/пароль в 1 символ ушли бы в БД и упали бы там
    сырой ошибкой). Добавлен FluentValidation + `ValidationBehavior` (MediatR pipeline behavior,
    один на оба MediatR-модуля — Application и Identity.Application), валидаторы для
    Register/ChangePassword/ResetPassword/CreateReview/ReplyToReview/OpenDispute/
    RequestWithdrawal/AddSavedPayoutMethod/TopUpBalance. Валидация — только формат/диапазон
    (не требует БД); правила состояния (баланс, статус заказа, дубликаты) остались в
    обработчиках, чтобы не размывать границу CQRS повторным походом в БД из валидатора.
    `FluentValidation.ValidationException` ловится `DomainExceptionHandler` (по имени типа, как
    и остальные) → 400 с `ValidationProblemDetails.Errors` по полям.
  Проверено: полный набор тестов (68 Application.Tests + 6 WebAPI.IntegrationTests) и вручную
  через реальный бэкенд — создание/правка площадки (проверка, что мутация с tracked-сущностью
  реально сохраняется через переfetch отдельным запросом), листинг с `AsSplitQuery` (вложенные
  ссылки/сообщения не потерялись), и HTTP 400 с понятной структурой на невалидный email/пароль/
  сумму, при этом валидные запросы по-прежнему проходят.
- Клиент собирается (`ng build`, dev и prod) и проходит юнит-тест (`ng test`).
- Полный auth-цикл реализован сквозно: Domain → Application (MediatR) → Infrastructure (EF,
  JWT, bcrypt) → WebAPI (контроллер, cookie) → Angular (форма, guard, interceptor).
- Публичная часть фронтенда перенесена и визуально проверена в браузере (Playwright,
  скриншоты): лендинг (все 9 секций, fade-in-при-скролле, переключение светлой/тёмной темы),
  страница/модалка входа, страница регистрации.
- **Вебмастер (продавец) и Проекты (покупатель) — сквозные пути проверены вживую** (реальный
  Postgres + оба сервера, не только сборка):
  - Вебмастер: регистрация → `/webmaster` → добавление площадки → площадка в таблице со
    статусом «На модерации» → вкладка «Мои продажи» (пусто, заказов пока нет) → тёмная тема →
    сворачивание сайдбара.
  - Проекты: регистрация → редирект на `/projects` → создание проекта → отображение в
    списке → переход в `/projects/:id` → пустой список размещений → редактирование
    существующего проекта (форма корректно подгружает данные) → удаление (подтверждение,
    список обновляется).
  Эти прогоны вскрыли пять реальных багов (см. "Известные ограничения" → "исправлено") — без
  визуальной проверки в браузере, не только `dotnet build`/`ng build`, они бы остались.
- **Оптимизатор (покупатель) — сквозной путь проверен вживую** (реальный Postgres + оба
  сервера, два параллельных браузерных контекста — продавец и покупатель): продавец
  регистрируется и добавляет площадку → покупатель регистрируется, создаёт проект →
  `/optimizator` показывает каталог всех активных площадок (не только своих) → клиентский
  поиск по URL/тематике/описанию фильтрует таблицу → модалка покупки (3 вкладки) → выбор
  проекта, добавление ссылки, настройки (срочность/экспертность/страхование/уникальность) →
  вкладка «Итоги» показывает реально посчитанные значения → отправка создаёт заказ → тост
  об успехе → заказ виден и в `/projects/:id` покупателя (статус «Заявка»), и в «Мои продажи»
  продавца (с ценой, статусом, именем покупателя, рабочей кнопкой чата). Этот прогон вскрыл
  ещё один реальный баг (см. "Известные ограничения" → "исправлено").
- **Admin (управление пользователями) — сквозной путь проверен вживую.** Это новый модуль, не
  порт: у FOXLinks нет ни одного admin-контроллера на бэкенде, а `admin-users.vue` — локальный
  mock-массив без единого реального запроса. Спроектирован по минимуму того, что реально нужно:
  список пользователей (роль, число проектов, дата регистрации, статус), смена роли, бан/разбан
  — без фейковой колонки баланса и несуществующего статуса «на модерации» у пользователя.
  Проверено: продвижение пользователя до admin напрямую в БД → `/admin-users` показывает
  реальный список (23 пользователя, пагинация) → смена роли другого пользователя → бан → у
  забаненного пользователя попытка входа отклоняется с понятным сообщением «Аккаунт
  заблокирован.» → разбан → вход снова работает → обычный (не admin) пользователь при заходе на
  `/admin-users` перенаправляется на `/projects` (`roleGuard`). Этот прогон вскрыл два реальных
  бага (см. "Известные ограничения" → "исправлено").
- **Admin Dashboard — сквозной путь проверен вживую.** Тоже новый модуль: `admin-dashboard.vue`
  хардкодит все 4 карточки статистики и оба графика Chart.js на выдуманных числах. Заменено на
  одну агрегацию по реальным таблицам — всего пользователей/проектов/активных площадок/заказов и
  общий оборот (сумма `FinalPrice` по всем заказам) — без графиков (нет накопленной истории,
  чтобы её строить) и без придуманных трендов «+42 за неделю». Специально не считает «комиссию
  системы» отдельной цифрой: `PurchasedSite` хранит только итоговую цену с уже встроенной 10%-й
  комиссией, без сохранённой отдельно базовой цены на момент заказа — разбивать её обратно
  означало бы выдавать догадку за реальное число. Проверено вживую (23 пользователя, 6
  проектов, 7 активных площадок, 2 заказа, оборот 4 840 ₽ — совпадает с суммой двух реальных
  заказов из проверки Optimizator) и `roleGuard` (обычный пользователь при заходе на
  `/admin-dashboard` перенаправляется на `/projects`). Этот прогон вскрыл ещё один реальный баг
  (см. "Известные ограничения" → "исправлено").
- **Модерация площадок — сквозной путь проверен вживую.** По просьбе пользователя ("давай пока
  все без кошелька делать" — из трёх предложенных идей: кошелёк, модерация, реал-тайм
  уведомления, рейтинги — кошелёк отложен). Новый модуль: роль `moderator` существовала в
  seed-данных FOXLinks с самого начала, но нигде не использовалась — ни бэкенда, ни страницы.
  Заодно вскрылся реальный, ранее незамеченный баг: `CreateSiteCommandHandler` ставил
  `IsActive = true` сразу при создании, то есть непроверенное объявление продавца немедленно
  становилось видно и покупаемо в каталоге покупателя, а статус «На модерации» в списке продавца
  оставался неизменным навсегда, потому что ничего и никогда не переводило площадку дальше по
  статусам. Исправлено: новая площадка теперь стартует с `IsActive = false`; добавлен статус
  `"active"`/"Активна" (модерация одобряет → `IsActive = true` + статус "Активна", отклоняет →
  статус "Отклонена"). Проверено сквозным сценарием: продавец добавляет площадку → площадка НЕ
  видна в каталоге покупателя (0 строк по поиску) → модератор (продвинут до роли `moderator`
  напрямую в БД) видит её в очереди `/moderation`, видит там же и старые площадки из более ранних
  прогонов сессии (тоже застрявшие в статусе "На модерации" из-за того же бага) → одобряет →
  площадка сразу появляется в каталоге покупателя И статус у продавца меняется на "Активна" →
  вторая площадка отклонена → статус "Отклонена". Плюс `roleGuard('moderator', 'admin')`
  (обычный пользователь при заходе на `/moderation` перенаправляется на `/projects`), пункт
  «Модерация» в шапке виден и чистому модератору, и админу.
  Заодно исправлен смежный баг: успешный вход администратора всегда вёл на `/projects` (в
  `login.ts` и в `header.ts`'s `onLoginSuccess()` был хардкод, плюс устаревший комментарий
  "admin-dashboard.vue isn't built yet" — уже неверный с прошлой сессии). Вынесено в
  `UserStore.postLoginRoute` — единое место для решения, куда вести пользователя после входа.
- **Реал-тайм уведомления через SignalR — проверены вживую, все 4 события.** `NotificationHub`
  был заведён с самого начала архитектуры, но ничего в него не пушило до сих пор. Добавлен
  `INotificationPusher` (Application) / `SignalRNotificationPusher` (WebAPI, поверх
  `IHubContext<NotificationHub>`) — fire-and-forget, ошибка пуша (отключённый клиент) логируется
  и не валит команду, которая его вызвала. Подключено к 4 существующим командам: новый заказ →
  продавцу (`RequestPublicationCommandHandler`), заказ принят → покупателю
  (`AcceptOrderCommandHandler`), новое сообщение → получателю (`SendMessageCommandHandler`),
  площадка одобрена/отклонена → продавцу (`ApproveSiteCommandHandler`/`RejectSiteCommandHandler`).
  На клиенте — `NotificationsBootstrapService` (новый) подключает уже существовавший, но никогда
  не использовавшийся `NotificationHubService` при появлении сессии и показывает пуши через
  `ToastService`. При первой сквозной проверке (два открытых вкладки — продавец и покупатель,
  без перезагрузки страницы) уведомление вообще не долетало — вскрылся реальный баг в
  `CustomUserIdProvider` (см. "Известные ограничения" → "исправлено"), без которого вся фича
  молча не работала бы никогда, несмотря на то что код компилировался и не бросал исключений.
  После исправления проверены вживую все 4 события с реальными живыми тостами на второй,
  уже открытой вкладке.
- **Избранное / реальные фильтры каталога / отзывы и рейтинг / верификация владения площадкой —
  все четыре проверены вживую в одном сквозном прогоне.** Ответ на "что ещё добавить, кроме
  кошелька" — реализовано всё предложенное сразу.
  - **Избранное** (`FavoriteSite`, buyer↔site) — звёздочка в каталоге Optimizator, страница
    `/favorites`. FOXLinks' "Избранные площадки" — везде ссылки на /coming-soon.
  - **Фильтры каталога** — реальные topic/country/price/iks/dr вместо декоративной панели
    исходника (`applyFilters` там просто крутила фейковый спиннер через `setTimeout`).
  - **Отзывы и рейтинг** (`SiteReview`, 1:1 с `PurchasedSite`) — отзыв разрешён только когда
    заказ реально подтверждён опубликованным. Ровно на этом всплыл реальный, отдельный от
    Reviews баг: `ConfirmPublishedCommand`/эндпоинт `POST /sites/{id}/confirm-published`
    существовали с предыдущей сессии, но **ни одна кнопка в клиенте никогда их не вызывала** —
    `PurchasedSite.IsPublished` не мог стать `true` вообще ни для одного заказа, что делало
    новую фичу отзывов недостижимой сразу после написания. Исправлено: `order-task-modal.html`
    теперь показывает «Подтвердить публикацию» вместо Принять/Отклонить, когда заказ уже в
    статусе "в работе" и ещё не опубликован.
  - **Верификация владения площадкой** (`Site.VerificationToken`/`IsVerified`) — продавец
    публикует токен на своём сайте (мета-тег или просто текст на странице), `POST
    /sites/{id}/verify` проверяет. Специально спроектировано с защитой от SSRF, так как URL
    площадки — это ввод продавца, а не доверенная константа: перед каждым запросом (включая
    редиректы — они обрабатываются вручную, не автоматически) резолвится DNS и отклоняются
    приватные/loopback/link-local диапазоны (в т.ч. `169.254.169.254` — типичный endpoint
    облачных метаданных), таймаут 5 секунд, ответ читается не более 256 КБ.
  Сквозной прогон: продавец добавляет площадку → площадка одобрена модератором → покупатель
  добавляет в избранное (звезда закрашивается, площадка появляется в `/favorites`) → фильтр по
  минимальной цене корректно отсеивает площадку → покупатель покупает размещение → продавец
  принимает заказ → продавец подтверждает публикацию → покупатель оставляет отзыв (4★) из
  `/projects/:id` → рейтинг «★ 4 (1)» сразу виден и в каталоге Optimizator, и в «Мои площадки»
  продавца. Отдельно проверена верификация: попытка проверить площадку с несуществующим доменом
  корректно и безопасно завершается неудачей (DNS не резолвится → «код не найден»), без падений
  и зависаний.
- **Восстановление/смена пароля, история статусов заказа, экспорт в CSV, инбокс уведомлений —
  все четыре проверены вживую.** Ответ на "что ещё можно добавить для этого проекта" —
  реализовано всё предложенное сразу.
  - **Восстановление/смена пароля** (`PasswordResetToken`) — `POST /auth/forgot-password`
    генерирует сырой токен (`RandomNumberGenerator.GetBytes(32)`), хранит его SHA-256-хеш (не
    bcrypt — bcrypt-у нужен случайный конкретно для проверки, а не поиска, а сброс пароля требует
    детерминированного поиска по точному значению токена; это осознанное отличие от `RefreshToken`,
    который хранит токен как есть), срок жизни 1 час, одноразовый. Email-провайдера в проекте нет
    (см. "Известные ограничения"), поэтому ссылка сброса логируется через `ILogger.LogWarning`
    вместо реальной отправки — вся механика (генерация/хеш/срок/одноразовость) настоящая и
    протестирована сквозным сценарием, не пропущено ничего, кроме канала доставки. `POST
    /auth/reset-password` принимает сырой токен, `POST /auth/change-password` (авторизованный)
    требует текущий пароль. Новая страница `/profile` (форма смены пароля + данные аккаунта),
    `/forgot-password`, `/reset-password?token=...`, ссылка «Забыли пароль?» на `/login`, пункт
    «Профиль» в выпадающем меню шапки. Проверено полностью через API (правильный/неправильный
    текущий пароль, повторное использование токена отклоняется, логин новым паролем работает,
    логин старым — не работает) и через браузер (Playwright): весь путь forgot → лог с
    ссылкой → reset → редирект на login → вход новым паролем → `/profile` → смена пароля.
  - **История статусов заказа** (`PurchasedSiteEvent`) — не строгий лог переходов `StatusId`
    (одно реальное событие, подтверждение публикации, вообще не меняет `StatusId`, так что
    строгий лог его бы пропустил), а гибкая строка `Description` с `CreatedAt` в качестве
    времени события. Пишется в `RequestPublicationCommandHandler` ("Заявка на размещение
    создана"), `AcceptOrderCommandHandler` ("Заказ принят продавцом в работу"),
    `ConfirmPublishedCommandHandler` ("Продавец подтвердил публикацию"),
    `CreateReviewCommandHandler` ("Покупатель оставил отзыв"). `GET
    /purchased-sites/{id}/events` проверяет, что вызывающий — покупатель ИЛИ продавец этого
    заказа (пробует оба lookup'а, `NotFoundException` если ни один не подошёл — сторонний
    пользователь получает 404, не 403, чтобы не подтверждать существование заказа). На клиенте —
    переиспользуемый `<app-purchased-site-timeline>`, вставлен и в `order-task-modal` (продавец),
    и разворачивающейся строкой в `project-details` (покупатель, тот же паттерн, что и
    инструкции по верификации в `my-platforms`). Проверено сквозным сценарием (создание →
    принятие → подтверждение публикации → отзыв) с обеих сторон и проверкой, что посторонний
    пользователь получает 404.
  - **Экспорт в CSV** — `GET /webmaster/sales/export` и `GET /projects/{id}/sites/export`,
    переиспользуют существующие paginated-запросы с `perPage = int.MaxValue`. UTF-8 BOM в начале
    файла (иначе Excel показывает кириллицу как абракадабру при прямом открытии, не через мастер
    импорта), корректное экранирование полей с запятыми/кавычками (`TaskDescription` может
    содержать что угодно) по RFC 4180. Найден и исправлен реальный баг при первой же проверке:
    `decimal.ToString("F2")` без `CultureInfo.InvariantCulture` брал текущую культуру сервера
    (`ru-RU`), которая использует запятую как десятичный разделитель — при экспорте CSV это
    давало `1500,00` внутри поля, разделённого запятыми (файл оставался технически валиден
    благодаря экранированию в кавычки, но был бы нечитаем при импорте без него). Исправлено
    явным `CultureInfo.InvariantCulture`. На клиенте — кнопки «Экспорт в CSV» на `/webmaster`
    (вкладка «Мои продажи») и `/projects/:id`; ответ приходит как `Blob` (`responseType:
    'blob'`) и скачивается через одноразовый `<a download>` (`core/http/download-blob.ts`) —
    обычная ссылка `<a href>` не смогла бы пронести Bearer-токен для авторизованного GET.
    Проверено и через `curl` (BOM, экранирование, инвариантная культура), и через реальное
    скачивание файла в браузере (Playwright, оба маршрута).
  - **Инбокс уведомлений** (`Notification`) — постоянное хранилище рядом с уже существовавшим
    live-пушем через `INotificationPusher`/SignalR: та же самая команда, что раньше только
    пушила тост, теперь ещё и пишет строку в `Notification` с тем же текстом, что видит тост
    (тексты продублированы вручную между `NotificationsBootstrapService.ts` и 4 обработчиками
    команд — синхронизировать при изменении формулировок в любом из двух мест). Подключено к тем
    же 4 командам: `RequestPublicationCommandHandler`, `AcceptOrderCommandHandler`,
    `SendMessageCommandHandler`, `ApproveSiteCommandHandler`/`RejectSiteCommandHandler`. `GET
    /notifications` (пагинация), `GET /notifications/unread-count`, `POST
    /notifications/mark-all-read`. На клиенте колокольчик в шапке (раньше — декоративная
    заглушка) стал реальным: бейдж с числом непрочитанных, выпадающий список, «Прочитать все».
    `NotificationsStore.refreshUnreadCount()` вызывается и при подключении SignalR-сессии, и
    после каждого живого пуша — счётчик всегда перечитывается с сервера, а не инкрементируется
    локально, поэтому остаётся верным, даже если вкладка пропустила более ранний пуш. Проверено
    вживую: два прогона через `curl` (persisted-запись, счётчик, пагинация, mark-all-read) и
    сквозной браузерный сценарий с живым SignalR-пушем (заказ создаётся внешним запросом, пока
    вкладка продавца открыта и не перезагружается — бейдж обновляется на живую, без reload).
- **`DynamicStat` засеян и все 12 позиций теперь считаются по-настоящему.** Ответ на "что ещё
  можно сделать" — из четырёх вариантов (засеять DynamicStat, кошелёк, аналитика/позиции, "что-то
  другое") выбран самый быстрый и наименее рискованный. `DynamicStatConfiguration.HasData` теперь
  сеет все 12 строк 1:1 с FOXLinks' `DynamicStatsSeeder.php` (заголовки, суффикс "₽" только там,
  где сеятель его ставит). Важное отличие от исходного плана в "Что дальше": FOXLinks'
  собственные Observers (`ProjectObserver`/`SiteObserver`/`PurchasedSiteObserver`, прочитаны
  перед реализацией) реализуют только позиции 1 (все три страницы) и 2 (только webmaster) —
  позиции 3-4 сидятся нулём и НИКОГДА не обновляются никаким кодом ни в исходнике, ни в этом
  порте до сих пор; `Project.TotalLinks`/`LinksPosted`/`FrozenPosted`/`SpentMoney` (которые
  предыдущая версия этого раздела предлагала переиспользовать для project-позиций 2-4) — те же
  самые вечно-нулевые поля: они выставлены в `ProjectDto`, но ни один контроллер/обработчик ни в
  FOXLinks, ни здесь никогда их не пишет. Подставить их означало бы просто заменить одну
  decorative-заглушку («Статистика пока отсутствует») на другую (реальное поле, которое навсегда
  останется нулём) — вместо этого все восемь новых позиций считаются на лету из
  `PurchasedSite`/`Site` (уже живых, реально изменяющихся таблиц), с той же схемой триггеров
  "пересчитать в обработчике команды, который меняет исходные данные", что и у уже готовых
  позиций 1-2:
  - **webmaster** (в скоупе конкретного продавца, как и позиции 1-2): «Общий доход» — сумма
    `FinalPrice` по заказам этого продавца, любой статус (та же логика без фильтра по статусу,
    что и `AdminDashboardDto.TotalRevenue` — для единообразия); «Средняя цена» — средняя `Price`
    по его активным площадкам. Обе — тот же триггер, что и активные площадки/продажи.
  - **optimizator** (общесистемно, как и позиция 1 «Доступно площадок»): «Активные заказы» —
    число заказов в статусах "application"/"work" по всей платформе; «Средняя цена» — средняя
    цена по всем активным площадкам системы; «Экономия» — сумма `max(0, Site.Price -
    PurchasedSite.FinalPrice)` по всем заказам. Честная, не выдуманная формула — но сейчас почти
    всегда близка к нулю или невелика, поскольку `FinalPrice` считается клиентом с множителями
    страховки/срочности, которые обычно ПОВЫШАЮТ цену, а не понижают её; это не имитация числа, а
    реальная формула, которой сейчас просто не с чем сработать эффектно при текущей модели
    ценообразования.
  - **project** (общесистемно, как и позиция 1 «Всего проектов» — `ProjectObserver` считает ВСЕ
    проекты платформы, не только проекты вызывающего пользователя, и это унаследовано, а не
    доделано впервые здесь): «Размещено ссылок» — число заказов с `IsPublished = true`; «Ссылки в
    работе» — число заказов в статусе "work", ещё не подтверждённых опубликованными; «Потрачено»
    — сумма `FinalPrice` по всем заказам платформы.
  Как и позиции 1-2, эти строки — единственная запись на `(page_key, position)`, разделяемая
  между всеми пользователями страницы (у webmaster-позиций это то же "последний записавший
  побеждает" по конкретному продавцу, что уже было в FOXLinks для позиций 1-2 — не новая
  проблема, не начал её чинить). Проверено вживую через `curl` (все 12 строк до/после создания
  площадки → заказа → принятия → подтверждения публикации → нового проекта, значения меняются
  ожидаемо на каждом шаге) и в браузере (Playwright, три страницы: `/webmaster`, `/projects`,
  `/optimizator` — везде реальные карточки вместо «Статистика пока отсутствует»).
- **Живое обновление открытого чата.** `SendMessageCommandHandler` уже слал SignalR-пуш
  ("NewMessage") для тоста/инбокса уведомлений, но если у получателя в этот момент был открыт сам
  `MessageChatModal` этого заказа, новое сообщение туда не попадало — нужно было закрыть и снова
  открыть модалку. Исправлено без нового hub-метода: `INotificationPusher.NotifyNewMessageAsync`
  теперь принимает готовый `MessageDto`, `SignalRNotificationPusher` шлёт его поля один в один с
  тем, что клиент уже ожидает от REST (`id`/`purchasedSiteId`/`senderId`/`recipientId`/`text`/
  `createdAt`/`isRead`) плюс `senderName` — то же событие "NewMessage" читают и
  `NotificationsBootstrapService` (тост/бейдж, как раньше), и теперь `MessageChatModal`
  (фильтрует по `purchasedSiteId === order().id`, добавляет сообщение в список напрямую, без
  повторного запроса). `NotificationHubService` получил `off()` — SignalR копит обработчики на
  одном и том же соединении, а модалка открывается/закрывается много раз за сессию, так что
  подписка обязана сниматься в `ngOnDestroy`, иначе после нескольких открытий сообщение
  задваивалось бы. Проверено вживую: два окна (продавец и покупатель), у продавца модалка чата
  открыта заранее, покупатель отправляет сообщение через форму — оно появляется в уже открытой
  модалке продавца без перезагрузки и без повторного открытия, одновременно с уже существующим
  тостом "Новое сообщение от...".
- **Аналитика / Мониторинг позиций / остаток Admin-панели — все четыре страницы построены.**
  Перед реализацией прочитаны все четыре исходных файла FOXLinks целиком (`analytics.vue`,
  `position.vue`, `admin-api.vue`, `system-settings.vue`), включая грep по бэкенду на предмет
  реального API за каждым элементом UI — результат разделил их на две принципиально разные
  категории:
  - **Аналитика — построена по-настоящему**, не макет. У FOXLinks `analytics.vue` на 100%
    статичный (6 графиков Chart.js рисуют захардкоженные массивы, таблица проектов — вымышленные
    строки вроде `desingwood.ru`), но часть показателей (расходы/доходы по месяцам, разбивка
    заказов по статусам, таблица проектов) реально считается из `Project`/`PurchasedSite`/`Site`
    — новый модуль `Application/CQRS/Analytics` (`GetAnalyticsQuery`, аналог
    `IAdminDashboardRepository` по духу: обычная агрегация на чтение, не кэширующий счётчик под
    `IDynamicStatsRefresher`, потому что сводка "и как покупатель, и как продавец" не привязана
    к одной конкретной команде, которая её меняет). Показатели вроде CTR/видимости/позиции в
    выдаче — не включены: для них нет источника данных нигде в домене приложения (см. Position
    ниже) — не выдумывать то, что нечем посчитать. На клиенте — новый `shared/chart-canvas/`
    (тонкая обёртка над `chart.js`, добавлен как настоящая npm-зависимость, впервые в этом
    проекте) и страница `/analytics` (route `data.palette: 'analytics'`, уже перенесённая
    палитра). Ссылки "Аналитика" в сайдбаре (были `/coming-soon` в обеих ветках — админской и
    обычной) переключены на реальный маршрут. Проверено вживую и через `curl` (агрегация верна
    для покупателя и продавца по отдельности), и в браузере (оба реальных графика и таблица
    рендерятся с живыми числами).
  - **Мониторинг позиций / Управление API / Настройка системы — честный макет, не порт.**
    `position.vue` целиком требует подсистему отслеживания позиций в поисковой выдаче (ключевые
    запросы, снимки SERP, история) — такой сущности нет ни в бэкенде FOXLinks, ни где-либо ещё в
    домене этого приложения, и нет интеграции ни с одним внешним сервисом ранжирования; это не
    пробел переноса, это отдельная нереализованная подсистема. `admin-api.vue` описывает
    управление API-ключами для внешних интеграций, которого в FOXLinks никогда не существовало
    (нет модели `ApiKey`, нет публичного REST API, который эти ключи вообще защищали бы).
    `system-settings.vue` — форма 2FA/CSRF/лимитов/выбора алгоритма шифрования без единой
    таблицы настроек в бэкенде; в реальных системах такие параметры почти всегда живут в
    конфигурации сервера, а не в редактируемой через веб-форму записи БД. По прямому запросу
    пользователя ("всё четыре, включая выдуманное") все три страницы построены визуально, но
    без единой выдуманной цифры или значения: показатели — «—» вместо чисел FOXLinks
    (`5%`, `24,567`, `94%` и т.п.), таблицы — честные пустые состояния («Ключей пока нет»,
    «Нет отслеживаемых поисковых запросов»), тумблеры/поля формы — отключены и не предзаполнены,
    кнопки действий вызывают тост с объяснением, почему функция недоступна, вместо `alert()`.
    Единственные настоящие данные на `/position` — список собственных проектов пользователя в
    выпадающем списке (сами проекты реальны, просто отслеживания позиций для них нет). Перенесены
    в `design-system.css` использованные `.system-settings`/`.settings-group`/`.settings-row`/
    `.setting-item`/`.toggle-switch`/`.danger-zone` (были в глобальном `main.css` FOXLinks, не
    scoped-стилях страниц) и `.compact-select`/`.compact-btn` (были в scoped-стилях
    `position.vue`). Маршруты `/position` (`roleGuard` не нужен, для обычных пользователей),
    `/admin-api` и `/system-settings` (`roleGuard('admin')`, как остальная admin-панель) — все
    три флага `implemented` в `nav-items.ts` переключены на `true`, ссылки в сайдбаре и шапке
    больше не ведут на `/coming-soon`. Проверено вживую (Playwright, все три страницы + переходы
    по ссылкам из сайдбара/шапки администратора).
- **Кошелёк/баланс — реализован, включая реальное списание/зачисление.** Единственная фича из
  всех предложенных за сессию списков, которую пользователь сам откладывал трижды подряд, пока
  явно не попросил её сделать. Не порт: `balance-topup-modal.vue` в исходнике сиротский (нигде
  не импортируется), таблицы `balance`/`wallet` нет ни в одной миграции FOXLinks — проектировано
  с нуля. `User.Balance` (новое поле) + `BalanceTransaction` (новая таблица-леджер: тип
  TopUp/Purchase/Sale, сумма со знаком, описание, опциональная ссылка на заказ) — баланс не
  просто число, а сумма реальных проводок, так что «История операций» показывает настоящий
  аудит-лог, а не выдумку задним числом.
  - **Реально всё, кроме приёма платежа.** `RequestPublicationCommandHandler` теперь проверяет
    баланс покупателя ДО создания заказа (`ConflictException` с понятным текстом, если не
    хватает — тот же путь ошибки, что уже подхватывает `extractErrorMessage()` на клиенте, без
    отдельной обработки) и списывает `FinalPrice` в момент создания заказа.
    `AcceptOrderCommandHandler` зачисляет ту же сумму продавцу в момент принятия им заказа в
    работу (не в момент создания — платить продавцу за то, что он ещё не согласился делать,
    было бы неправильно). Списание и зачисление — одноразовые, без отмены: в приложении нет ни
    одного пути отклонить/отменить уже созданный заказ (см. "Известные ограничения" —
    `rejectOrder()` в клиенте как был UI-заглушкой без бэкенда, так и остался), так что нет и
    сценария, требующего возврата средств.
  - **Единственный честный пробел — платёжный провайдер.** `TopUpBalanceCommandHandler`
    зачисляет сумму мгновенно вместо реального списания денег — как и восстановление пароля
    (лог вместо письма), это единственная точка, которую нужно будет заменить на реальный
    провайдер (эквайринг/ЮKassa/Stripe), когда он появится; вся остальная механика (леджер,
    проверка баланса, списание, зачисление) уже настоящая и никак не изменится. Страница
    `/wallet` явно предупреждает об этом текстом под формой пополнения, а не молчит об
    ограничении.
  - Header's статичный плейсхолдер "0.00 ₽" заменён на реальный `userStore.currentUser()?.balance`
    (кликабелен, ведёт на `/wallet`) — обновляется на клиенте сразу после покупки (страница
    покупателя), принятия заказа (страница продавца) и пополнения, без ожидания следующего
    логина. В `buy-miralinks-modal`'s вкладке «Итоги» добавлена строка «Ваш баланс» — красным,
    если её не хватает на текущий заказ, ДО отправки, не только как ошибка после попытки.
  Проверено вживую и через `curl` (заказ с нулевым балансом отклоняется, пополнение проходит,
  заказ после пополнения проходит и списывает средства, продавец получает деньги при принятии,
  история операций содержит все три проводки в правильном порядке), и в браузере (Playwright:
  реальный баланс в шапке, страница `/wallet`, баланс в модалке покупки).

## Что дальше (по твоей команде)

Всё из ранее обсуждавшихся списков, включая кошелёк (см. ниже за тем, что в нём настоящее, а что
— честно обозначенный пробел), готово. См. `Client/.../src/app/pages/_NEXT.md` за полным
соответствием Vue → Angular.

Sites (продавец + покупатель) / Stats / Messages / Projects / Optimizator / Admin / Moderation /
реал-тайм уведомления / избранное / отзывы и рейтинг / верификация площадок / восстановление и
смена пароля / история статусов заказа / экспорт в CSV / инбокс уведомлений / живой чат /
Аналитика / Мониторинг позиций / Управление API / Настройка системы / Кошелёк уже готовы.

## Как запустить локально

**API** (`Api/PointbreakLinksApi/`):
1. Поднять PostgreSQL, поправить `ConnectionStrings:DefaultConnection` в `appsettings.json`
   (или `appsettings.Development.json` / user-secrets).
2. Задать реальный `Jwt:Secret` — через `dotnet user-secrets set Jwt:Secret "..." --project WebAPI`
   (уже сделано в этой сессии для локальной БД) или переменные окружения; не коммитить в appsettings.
3. Применить ОБЕ независимые истории миграций, бизнес-контекст первым (создаёт схему `identity`
   и переносит туда `users`/`roles`/`role_user` — Identity-миграция на это рассчитывает):
   - `dotnet ef database update --project Infrastructure --startup-project WebAPI --context ApplicationDbContext`
   - `dotnet ef database update --project Identity.Infrastructure --startup-project WebAPI --context IdentityDbContext`
4. `dotnet run --project WebAPI` → Scalar-доки на `/scalar/v1`, по умолчанию `https://localhost:7001`.
5. Автотесты (не требуют поднятой БД — все зависимости замоканы): `dotnet test Application.Tests`.

**Client** (`Client/pointbreak-links-client/`):
1. `npm install` (уже сделано в этой сессии)
2. `npm start` → **обязательно на порту 4200** (по умолчанию), не на произвольном — бэкенд
   разрешает CORS только для origin'ов из `AllowedOrigins` в `appsettings.json`
   (`http://localhost:4200` по умолчанию); другой порт молча ловит CORS-ошибку на каждый
   запрос к API.

## Известные ограничения / стоит пересмотреть позже

**Исправлено в этой сессии (найдено при живой проверке Вебмастера в браузере, не при сборке):**

- ~~SSR не восстанавливает сессию на сервере~~ — при заходе на защищённый маршрут напрямую
  (`page.goto`, не переход по SPA-роутеру) SSR **реально редиректил на `/login` через HTTP 302**
  (не просто рендерил разлогиненное состояние), а после гидратации клиент подтверждал сессию и
  уводил на дефолтный маршрут — то есть залогиненный пользователь, запросивший `/webmaster`,
  гарантированно попадал не туда, куда шёл. Исправлено: защищённые маршруты теперь
  `RenderMode.Client` (`app.routes.server.ts`), гварды стали `async` и ждут
  `AuthService.sessionReady` (`auth.guard.ts`).
- ~~Refresh-cookie не работала между разными origin'ами~~ — стояла `SameSite=Lax`, которая не
  отправляется на cross-origin fetch/XHR. Исправлено на `SameSite=None` (`AuthController.cs`).
- `.table-header` в дизайн-системе использовал `--gray-100` как фон — в светлой теме это тот же
  тёмно-синий, что и `--text-primary`, то есть заголовки таблиц были невидимы (только тёмная
  тема случайно давала контраст, `--bg-secondary`). Баг был в исходном `main.css`, не только в
  порте. Исправлено на `--bg-secondary` (работает в обеих темах).
- Дублирующийся URL площадки падал 500-й с сырым стектрейсом EF/Postgres вместо понятной
  ошибки. Добавлены `ConflictException` + `WebAPI/Middleware/DomainExceptionHandler.cs`
  (централизованная обработка Domain-исключений → ProblemDetails) и предварительная проверка
  уникальности в `CreateSiteCommandHandler`/`UpdateSiteCommandHandler`.
- `GetProjectSitesQuery`/`GetProjectSitesQueryHandler` (`GET /projects/{id}/sites`) добавляют
  проверку владения проектом перед выдачей купленных площадок — у FOXLinks
  `getByProjectQuery()` фильтровал только по `project_id`, без проверки, что проект
  принадлежит вызывающему пользователю (любой залогиненный мог посмотреть чужой проект,
  подобрав id).
- `Program.cs` не регистрировал `JsonStringEnumConverter` — `System.Text.Json` по умолчанию
  сериализует enum'ы как числа. Для ответов (`PaymentSettingDto.InsuranceType`) это молча
  работало бы неправильно (клиент получил бы `0`/`1`/`2` вместо `"None"`/...), но впервые
  реально сломало запрос при добавлении `RequestPublicationCommand` — тело с
  `"insuranceType": "None"` не биндилось в `Domain.Enums.InsuranceType`, весь `[FromBody]`
  падал с 400. Исправлено добавлением `.AddJsonOptions(o =>
  o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))`.
- Ролевой `<select>` на `/admin-users` всегда показывал первую опцию ("Администратор")
  независимо от реальной роли пользователя — `[value]="user.roles[0]?.name"` на самом `<select>`
  выполнялся раньше, чем его дочерние `<option>` (сгенерированные вложенным `@for`) успевали
  появиться в DOM, так что браузеру было нечего сопоставлять со значением. Исправлено переносом
  выбора на `[selected]` у каждого `<option>` вместо `[value]` на родителе.
- `jwtAuthInterceptor` перехватывал **любой** 401-ответ (включая от `/auth/login`) и пытался
  обновить токен через `/auth/refresh` перед тем как отдать ошибку компоненту — если пользователь
  ввёл неверный пароль или аккаунт забанен, `/auth/refresh` тоже закономерно отвечает 401 (сессии
  ещё нет), и именно ЭТА ошибка (не исходная) долетала до формы входа. В паре с тем, что
  `login.ts`/`login-modal.ts` вообще игнорировали текст ошибки и показывали жёстко зашитое
  «Неверный email или пароль.», это маскировало любое другое сообщение — в частности новое
  «Аккаунт заблокирован.» от Admin-модуля. Исправлено: `/auth/login` исключён из retry-логики
  интерцептора (как уже был исключён `/auth/refresh`), `AuthController`'s `Login`/`Refresh` теперь
  возвращают `detail` в стандартном ProblemDetails-формате вместо `{ message }`, а
  `login.ts`/`login-modal.ts` читают его через `extractErrorMessage()` вместо игнорирования.
- `.stats-grid` (класс-обёртка карточек статистики, используемый шаблоном `smart-grid.html`
  на webmaster/project/optimizator) **никогда не был определён** в `design-system.css` — только
  сами `.stat-card`/`.stat-number` были перенесены, а сам grid-контейнер — нет. Раньше это было
  незаметно, потому что на всех трёх страницах `<app-smart-grid>` всегда показывал «Статистика
  пока отсутствует» (см. ниже — строки `DynamicStat` никогда не были засеяны), то есть карточки
  реально ни разу не рендерились до Admin Dashboard, где они впервые появились с настоящими
  данными и оказались сложены в одну колонку на всю ширину вместо сетки. Исправлено добавлением
  `.stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); ... }`
  (перенесено из исходного `main.css`) — как только на webmaster/project/optimizator появятся
  реальные строки `DynamicStat`, их карточки тоже лягут в сетку правильно.
- `CreateSiteCommandHandler` ставил `IsActive = true` сразу при создании площадки — новое,
  никем не проверенное объявление продавца немедленно было видно и покупаемо в каталоге
  Optimizator, а бейдж статуса у продавца («На модерации») не менялся никогда, потому что ничего
  не двигало площадку дальше по статусам. У FOXLinks то же самое (`StoreSiteRequest` тоже ставит
  `is_active = true` при создании) — не обнаружено раньше, потому что ничто не читало
  `IsActive` иначе как "показывать в каталоге или нет", и до Moderation некому было его менять.
  Исправлено: `IsActive = false` при создании, новый статус `"active"` (см. Moderation-модуль) —
  площадка появляется в каталоге только после одобрения.
- Успешный вход администратора всегда вёл на `/projects`, а не на его собственный дашборд —
  `login.ts` и `header.ts`'s `onLoginSuccess()` оба хардкодили `/projects`, причём в `header.ts`
  висел уже устаревший комментарий "admin's real landing page isn't built yet" — неверный с
  прошлой сессии, когда Admin Dashboard появился. Исправлено: оба места теперь используют
  `UserStore.postLoginRoute` (computed: `/admin-dashboard` для admin, иначе `/projects`) — одна
  точка принятия решения вместо двух рассинхронизированных копий.
- `CustomUserIdProvider.GetUserId()` проверял только claim `"sub"` — но ASP.NET Core's JWT
  bearer по умолчанию перемаппливает зарегистрированный claim `"sub"` в
  `ClaimTypes.NameIdentifier` ещё до построения `ClaimsPrincipal` (та же причина, по которой
  `ApiControllerBase.GetCurrentUserId()` изначально проверяет оба варианта). Из-за этого
  `IUserIdProvider.GetUserId()` возвращал `null` для абсолютно каждого SignalR-подключения, и
  `Clients.User(id).SendAsync(...)` молча не долетал ни до кого — без единой ошибки компиляции
  или исключения в рантайме, просто тихо ничего не происходило. Обнаружено только при первой
  сквозной проверке реал-тайм уведомлений (два открытых окна, без перезагрузки страницы) —
  без такой проверки этот баг остался бы полностью незамеченным, несмотря на то что и бэкенд,
  и фронтенд компилировались и не падали. Исправлено добавлением того же fallback на
  `ClaimTypes.NameIdentifier`.
- `ConfirmPublishedCommand`/`POST /sites/{id}/confirm-published` существовали с предыдущей
  сессии (когда переносился модуль Sites), но ни одна кнопка в клиенте никогда их не вызывала —
  `order-task-modal.html` показывал только "Принять"/"Отклонить" независимо от текущего статуса
  заказа, так что `PurchasedSite.IsPublished` не мог стать `true` вообще никогда. Незаметно, пока
  ничего не зависело от `IsPublished`, но именно на этом сломалась бы новая фича отзывов (отзыв
  разрешён только для опубликованных заказов) — вскрылось при попытке пройти сценарий целиком, а
  не при написании кода. Исправлено: модалка теперь показывает «Подтвердить публикацию», когда
  заказ в статусе "в работе" и ещё не опубликован (`OrderTaskModal.isAcceptedNotPublished`),
  `SitesStore.confirmPublished()` вызывает эндпоинт и обновляет список продаж.
- CSV-экспорт (`SitesController.ExportSales`/`ExportProjectSites`) форматировал `FinalPrice`
  через `decimal.ToString("F2")` без явной культуры — на сервере с текущей культурой `ru-RU` это
  даёт `1500,00` (запятая как десятичный разделитель) внутри CSV-поля, разделённого запятыми.
  Файл оставался технически валиден (значение экранировалось в кавычки как единое поле), но был
  бы нечитаем при импорте в любой инструмент, ожидающий точку. Обнаружено сразу при первой
  проверке через `curl`, не при написании кода. Исправлено явным `CultureInfo.InvariantCulture`.

**Ещё не исправлено:**

- **Email-провайдера в проекте нет** — `ForgotPasswordCommandHandler` логирует ссылку сброса
  пароля через `ILogger.LogWarning` вместо реальной отправки письма (см. выше). Вся механика
  токена настоящая и протестирована; не хватает только канала доставки. Когда появится реальный
  email-провайдер, заменить один `logger.LogWarning(...)` на вызов мейлера — больше ничего в
  этом обработчике менять не придётся.
- **Платёжного провайдера в проекте нет** — `TopUpBalanceCommandHandler` зачисляет сумму
  пополнения кошелька мгновенно вместо реального списания денег (см. выше). Леджер
  (`BalanceTransaction`), проверка баланса перед покупкой и списание/зачисление по заказу —
  всё настоящее; не хватает только приёма платежа. Когда появится реальный провайдер, заменить
  инстант-зачисление в этом обработчике на его webhook/return-хендлер.
- Тексты уведомлений продублированы вручную в двух местах: серверные обработчики команд (строка
  `Notification.Message`, видна в постоянном инбоксе) и
  `NotificationsBootstrapService.ts` (текст живого тоста через SignalR) — при изменении
  формулировки одного нужно не забыть поправить и второе, единого источника истины для текста
  сейчас нет.

- `Status` — одна lookup-таблица на два разных смысла (модерация Site vs статус заказа),
  унаследовано от FOXLinks напрямую.
- `Messages` в FOXLinks — таблица/модель есть, но `message-chat-modal.vue` там на 100%
  захардкожен (фейковые сообщения, нет `send`-обработчика, имя собеседника никогда не
  подставляется). У нас реализован по-настоящему (`MessagesController`, `GetMessages`/
  `SendMessage`, `ReadAt` для статуса прочтения) — раз сущность и таблица уже были в скаффолде,
  сделать по-честному стоило не намного дороже, чем повторить бутафорию.
- Иконки — Font Awesome 6 Free подключён через cdnjs (`<link>` в `index.html`), как проще всего
  перенести `<i class="fas fa-...">` из исходника. Можно позже заменить на npm-пакет или SVG,
  если важна независимость от внешнего CDN.
