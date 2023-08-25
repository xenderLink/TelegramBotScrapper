Простое микросервисное приложение с скрэппером и телеграм-ботом.
Создайте бота в https://telegram.me/BotFather и получите токен. В строчке https://github.com/xenderLink/TelegramBotScrapper/blob/e49e0a6371a161c12465eb0e2ab68483eb5022a9/TelegramBotScrapper/Bot/Bot.cs#L22 вместо "token" подставьте полученный токен от BotFather.

Для создания docker-образа в локальном репозитории:
```
docker build -t bot-image .
```
После того, как соберётся образ, для запуска контейнера:
```
docker run --name scrapper -d bot-image
```
Остановить контейнер:
```
docker stop scrapper
```
Удалить контейнер и образ:
```
docker rm scrapper && docker rmi bot-image
```
