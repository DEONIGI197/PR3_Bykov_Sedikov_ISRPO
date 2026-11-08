# Практическая работа №3 — API магазина одежды

Участники: Быков Денис и Седиков Артем (`crysis064`).

## Что сделано

API на ASP.NET Core 8 с базой SQLite. В базе есть товары, размеры и остатки, пользователи, заказы и состав заказов. Пароли хранятся в виде хеша. После входа API выдаёт JWT. Регистрация всегда создаёт обычного покупателя. Изменение статуса, остатков и доступности товара разрешено менеджеру.

## Распределение задач

| Участник | Задачи |
| --- | --- |
| Денис | Модели, база данных, товары, регистрация и вход |
| Артем | Заказы, проверка запросов в Postman |

Для совместной работы используются ветки `feature/catalog-auth` и `feature/orders`. Изменения объединяются в `main` после проверки.

## Запуск

Открыть `ClothingStore.sln` в Visual Studio 2022 и запустить профиль `http`. При первом запуске создаются база `store.db` и два примера футболок. Swagger открывается по адресу `http://localhost:5167/swagger`.

Для учётной записи менеджера задать переменные среды `Manager__Login` и `Manager__Password` перед запуском. Например, в PowerShell:

```powershell
$env:Manager__Login = "manager"
$env:Manager__Password = "свой_пароль"
dotnet run --project ClothingStoreApi --launch-profile http
```

В Visual Studio можно создать локальный `ClothingStoreApi/appsettings.Development.json` с разделом `Manager`, содержащим `Login` и `Password`. Этот файл исключён из Git. Если база уже создана, менеджер добавится при следующем запуске, если такого логина ещё нет. Для размещения на сервере нужно задать `Jwt__Key` своим длинным случайным ключом и выбрать другой пароль менеджера через переменную среды.

## Запросы

| Метод | Адрес | Доступ |
| --- | --- | --- |
| POST | `/api/auth/register` | Все |
| POST | `/api/auth/login` | Все |
| GET | `/api/products` | Все |
| GET | `/api/products?search=Белая` | Все |
| POST | `/api/orders` | Авторизованный пользователь |
| GET | `/api/orders?status=New` | Авторизованный пользователь |
| GET | `/api/orders?buyerId=1` | Авторизованный пользователь |
| PATCH | `/api/orders/1/status` | Менеджер |
| PATCH | `/api/products/1/sizes/1/stock` | Менеджер |
| PATCH | `/api/products/1/sale` | Менеджер |

В коллекции `ClothingStore.postman_collection.json` есть примеры всех запросов. Перед входом менеджера заполнить переменную `managerPassword`. Токены и идентификаторы коллекция заполняет после выполнения соответствующих запросов. Для снятия с продажи передать `{"isForSale":false}`, для возврата — `{"isForSale":true}`.

Статусы заказов: `New`, `Processing`, `Shipped`, `Completed`, `Cancelled`.
