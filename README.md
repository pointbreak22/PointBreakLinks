# PointbreakLinks

![CI](https://github.com/pointbreak22/PointBreakLinks/actions/workflows/ci.yml/badge.svg)

Маркетплейс для покупки/продажи размещений ссылок на сторонних площадках (SEO-линкбилдинг).
Покупатель создаёт проект, находит площадку в каталоге и оформляет заказ; продавец (вебмастер)
принимает заказ в работу, размещает ссылку и подтверждает публикацию. Есть внутренний кошелёк,
модерация новых площадок, чат по заказу, отзывы/рейтинг, аналитика и админ-панель.

Портирован с référence-проекта на Vue/Laravel (FOXLinks) на Clean Architecture + CQRS/.NET и
Angular, с рядом новых модулей, которых не было в оригинале (кошелёк, модерация, real-time
уведомления, верификация владения площадкой и другие — см. `PROJECT_MAP.md`).

## Стек

- **Backend**: .NET 10, Clean Architecture (Domain → Application → Infrastructure → WebAPI),
  MediatR (CQRS), EF Core + PostgreSQL, SignalR (real-time уведомления), JWT + refresh-токены,
  отдельный Identity-модуль (bounded context) в схеме `identity`.
- **Frontend**: Angular 21 (standalone components, signals), Vitest для юнит-тестов.
- **Тесты**: xUnit + Moq (`Application.Tests`) — command- и query-обработчики.

## Быстрый старт

### API (`Api/PointbreakLinksApi/`)

1. Поднять PostgreSQL, указать строку подключения в `appsettings.json` (или user-secrets).
2. Задать `Jwt:Secret`: `dotnet user-secrets set Jwt:Secret "..." --project WebAPI`.
3. Применить обе независимые истории миграций (бизнес-контекст первым):
   ```bash
   dotnet ef database update --project Infrastructure --startup-project WebAPI --context ApplicationDbContext
   dotnet ef database update --project Identity.Infrastructure --startup-project WebAPI --context IdentityDbContext
   ```
4. `dotnet run --project WebAPI` → Scalar-документация на `/scalar/v1` (по умолчанию `https://localhost:7001`).
5. Тесты (БД не нужна — все зависимости замоканы): `dotnet test Application.Tests`.

### Client (`Client/pointbreak-links-client/`)

```bash
npm install
npm start   # обязательно порт 4200 — см. AllowedOrigins в appsettings.json
```

## Документация

Полная карта архитектуры, принятые решения и их обоснование, известные ограничения — в
[`PROJECT_MAP.md`](PROJECT_MAP.md).
