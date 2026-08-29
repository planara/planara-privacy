![build](https://github.com/planara/planara-privacy/actions/workflows/build.yml/badge.svg)
![release](https://github.com/planara/planara-privacy/actions/workflows/release.yml/badge.svg)
![publish-k3s](https://github.com/planara/planara-privacy/actions/workflows/publish-k3s.yml/badge.svg?branch=main)
![version](https://img.shields.io/github/v/tag/planara/planara-privacy?sort=semver)
[![Codecov](https://codecov.io/gh/planara/planara-privacy/branch/main/graph/badge.svg)](https://codecov.io/gh/planara/planara-privacy)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](http://makeapullrequest.com)

# Planara.Privacy

Сервис управления пользовательскими согласиями.

Отвечает за хранение версий документов согласий, выдачу и отзыв согласий пользователей, ведение истории изменений и обработку временных согласий в процессе регистрации.

Реализован как ASP.NET Core + GraphQL сервис с PostgreSQL, Kafka и outbox-публикацией событий.

## Возможности

* Управление версиями документов согласий
* Получение текущей действующей версии согласия
* Хранение истории согласий пользователей
* Выдача согласия текущим пользователем
* Отзыв действующего согласия
* Обработка временных согласий в процессе регистрации
* Идемпотентная обработка запросов на выдачу согласия
* Публикация событий выдачи согласия в Kafka
* Публикация событий отзыва согласия в Kafka
* Outbox pattern для надежной доставки событий
* JWT авторизация (`[privacyorize]`)
* Фильтрация, сортировка и пагинация GraphQL-запросов
* GraphQL API (HotChocolate)

## GraphQL API

### Queries

* `currentConsentVersion(type: ConsentType): ConsentVersionResponse`
  Возвращает текущую опубликованную и вступившую в силу версию согласия указанного типа

* `consentVersions: ConsentVersionResponseConnection`
  Возвращает опубликованные версии документов согласий с поддержкой фильтрации, сортировки и пагинации

* `myConsents: UserConsentResponseConnection`
  Возвращает историю согласий текущего пользователя с поддержкой фильтрации, сортировки и пагинации (требует авторизации)

### Mutations

* `grantConsent(request: GrantConsentRequestInput): ConsentMutationResponse`
  Выдает согласие текущему пользователю на указанную версию документа (требует авторизации)

* `revokeConsent(type: ConsentType): ConsentMutationResponse`
  Отзывает действующее согласие указанного типа у текущего пользователя (требует авторизации)

## Запуск

Перед запуском сервиса необходимо поднять необходимую инфраструктуру:

```bash
docker compose up -d
```

После запуска инфраструктуры можно запустить сервис:

```bash
dotnet run --project src/Planara.Privacy.csproj
```

GraphQL endpoint:

```text
/graphql
```

## Тестирование

Для запуска тестов требуется Docker, так как интеграционные тесты используют Testcontainers и PostgreSQL.

Запуск тестов:

```bash
dotnet test Planara.Privacy.sln
```

Запуск тестов с покрытием:

```bash
dotnet test Planara.Privacy.sln \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings
```
