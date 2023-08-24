using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotScrapper.Helpers;

public abstract class VacancySender : IVacancySender
{
    protected readonly string[] cities = { "Челябинск", "Екатеринбург", "Москва", "Санкт-Петербург" };
    protected abstract string greetingMessage { get; set; }                     // сообщение по 

    protected int oldBotMsgId = 0;

    protected IReadOnlyList<(string, string)> Chlb, Ekb, Msk, Spb;              // Вакансии и гиперссылки
    protected int cIdx, eIdx, mIdx, sIdx;                                       // Индексы по городам

    protected InlineKeyboardMarkup citiesKeyboard, navKeyboard, backToKeyboard;
    protected abstract InlineKeyboardButton[] citiesButton { get; set; }        // кнопка для связи "города-сервис"

    public abstract Task SendVacancies(ITelegramBotClient client, Update update);

    public VacancySender()
    {
        var servicesButton =  new InlineKeyboardButton[] { "К списку сервисов" };

        citiesKeyboard = new (new []
        {
            new InlineKeyboardButton[] { new InlineKeyboardButton(cities[0]) { CallbackData = cities[0] },
                                         new InlineKeyboardButton(cities[1]) { CallbackData = cities[1] }
                                       },
                        
            new InlineKeyboardButton[] { new InlineKeyboardButton(cities[2]) { CallbackData = cities[2] },
                                         new InlineKeyboardButton(cities[3]) { CallbackData = cities[3] }
                                       },
            servicesButton
        });
        
        navKeyboard = new (new []
        {
            new InlineKeyboardButton[] { "Далее" },
            citiesButton,
            servicesButton
        });
        
        backToKeyboard = new (new []
        {
            citiesButton,
            servicesButton
        });        
    }

    protected void FirstChunkVacancies(IReadOnlyList<(string, string)> vacancies, ref int index, StringBuilder stringBuilder)
    {
        if (vacancies.Count <= 10)
        {
            for (index = 0; index < vacancies.Count; index++)
                 stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
        }
        else
        {
            for (index = 0; index < 10; index++)
                 stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
        }
    }

    protected void NextChunkVacancies(IReadOnlyList<(string, string)> vacancies, ref int index, StringBuilder stringBuilder, ref int remainElements)
    {
        remainElements = vacancies.Count - index;

        if (remainElements > 10)
        {
            for (int j = index + 10; index < j; index++)
                 stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
        
        }
        else
        {
            for (; index < vacancies.Count; index++)
                 stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
            
            index = 0;
        }
    }

    protected async Task NoVacancies(ITelegramBotClient botClient, Update update)
    {
        var botMsg = await botClient.SendTextMessageAsync(
                                     chatId: update.CallbackQuery.Message.Chat.Id,
                                     text: $"В этом городе нет подходящих вакансий.",
                                     replyMarkup: backToKeyboard);
                        
        oldBotMsgId = botMsg.MessageId;
    }


    protected async Task SendCities(ITelegramBotClient botClient, Update update)
    {
        var botMsg = await botClient.SendTextMessageAsync(
                                     chatId: update.CallbackQuery.Message.Chat.Id,
                                     text: greetingMessage,
                                     replyMarkup: citiesKeyboard);
        
        await MessageDeleter.DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, oldBotMsgId);

        oldBotMsgId = botMsg.MessageId;
    }
}