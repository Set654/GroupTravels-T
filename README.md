Т-Путешествия - Веб-приложение для организации совместных путешествий, распределения расходов и расчёта долгов между участниками.

Стек технологий:
C# / .NET 10
ASP.NET Core Web API
Entity Framework Core
PostgreSQL
JWT Authentication
React
Python FastAPI

Архитектура Backend:
TripService.Api - контроллеры, HTTP-контракты и конфигурация приложения.
TripService.Application - сценарии приложения, сервисы и интерфейсы.
TripService.Domain - сущности и бизнес-алгоритмы.
TripService.Infrastructure - работа с БД, репозитории и инфраструктурные сервисы.

Настройка локального запуска
Для запуска Backend необходимо установить .NET 10 SDK и PostgreSQL.
Все команды выполняются из корневой папки репозитория.
 1. Восстановление зависимостей
powershell
dotnet restore TTravel.slnx
 2. Настройка подключения к PostgreSQL
Создайте локальную базу данных PostgreSQL и укажите строку подключения через User Secrets:
powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=GroupTravels;Username=postgres;Password=YOUR\_PASSWORD" --project TripService.Api
Замените YOUR\_PASSWORD на свой пароль от PostgreSQL. При необходимости измените остальные параметры подключения.
 3. Настройка JWT
powershell
dotnet user-secrets set "Jwt:Issuer" "TTravel" --project TripService.Api
dotnet user-secrets set "Jwt:Audience" "TTravelApi" --project TripService.Api
dotnet user-secrets set "Jwt:ExpirationMinutes" "60" --project TripService.Api

Создание случайного ключа подписи:
powershell
$keyBytes = New-Object byte\[] 32
$rng = \[System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($keyBytes)
$rng.Dispose()
$jwtKey = \[Convert]::ToBase64String($keyBytes)
dotnet user-secrets set "Jwt:SigningKeyBase64" $jwtKey --project TripService.Api

4. Применение миграций
Если инструмент dotnet-ef не установлен:
powershell
dotnet tool install --global dotnet-ef --version 10.0.0

Применение существующих миграций к локальной базе данных:
powershell
dotnet ef database update --project TripService.Infrastructure --startup-project TripService.Api

5. Запуск приложения
powershell
dotnet run --project TripService.Api --launch-profile https
Локальный адрес API:
`https://localhost:7247`

Проверка работы сервера:

`GET /api/health/live`

OpenAPI-спецификация в режиме Development:
 `https://localhost:7247/openapi/v1.json`
 
Реализованные API-маршруты
| Метод | Маршрут | Назначение |
| GET | /api/health/live | Проверка работы сервера |
| POST | /api/trips | Создание поездки |
| GET | /api/trips | Получение списка поездок |
| GET | /api/trips/{tripId} | Получение поездки |
| GET | /api/trips/{tripId}/members | Получение участников |
| POST | /api/trips/{tripId}/members/virtual | Добавление виртуального участника |
| POST | /api/trips/{tripId}/invite | Приглашение пользователя |
| GET | /api/trips/invitations | Получение входящих приглашений |
| PUT | /api/trips/{tripId}/invite/respond | Ответ на приглашение |

Все маршруты, кроме проверки работы сервера, требуют JWT.
