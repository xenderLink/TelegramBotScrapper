using Telegram.Bot;
using Update = Telegram.Bot.Types.Update;

namespace TelegramBotScrapper.Helpers;

public interface IVacancySender
{
    public Task SendVacancies(ITelegramBotClient client, Update update);
}