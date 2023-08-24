using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using JsonCrud;
using Update = Telegram.Bot.Types.Update;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotScrapper.Helpers;

public sealed class HhRuVacancySender : VacancySender
{
    private HhRuJsonVacancy json;
    protected override string greetingMessage { get; set; } = "Выберите город, в котором вас интересует список вакансикй на hh.ru";

    // кнопка, которая возвращает название сервиса для связи "сервис-города"
    protected override InlineKeyboardButton[] citiesButton { get; set; } = 
        { new InlineKeyboardButton("К списку городов") { CallbackData = "hh.ru" } };  
                                                                                     
    public HhRuVacancySender() : base(){  json = new (); }

    public async override Task SendVacancies(ITelegramBotClient client, Update update)
    {
        switch (update.CallbackQuery.Data)
        {
            case "hh.ru":
                await SendCities(client, update);
                break;

            case "К списку городов":
                await SendCities(client, update);
                break;

            case "Челябинск":
            {
                Chlb = await json.GetVacancies(update.CallbackQuery.Data);
                
                if (Chlb.Count is 0)
                {
                    await NoVacancies(client, update);        
                }
                else if (Chlb.Count > 0)
                {
                    StringBuilder sb = new ();
                    
                    FirstChunkVacancies(vacancies: Chlb, index: ref cIdx, stringBuilder: sb);
                    await Send(client, update, stringBuilder: sb, remainElements: Chlb.Count, city: cities[0]); 
                }
            }
            break;

            case "Екатеринбург":
            {
                Ekb = await json.GetVacancies(update.CallbackQuery.Data);
                
                if (Ekb.Count is 0) 
                {
                    await NoVacancies(client, update);
                }
                else if (Ekb.Count > 0)
                {
                    StringBuilder sb = new ();

                    FirstChunkVacancies(vacancies: Ekb, index: ref eIdx, stringBuilder: sb);
                    await Send(client, update, stringBuilder: sb, remainElements: Ekb.Count, city: cities[1]); ; 
                }
            }
            break;

            case "Москва":
            {
                Msk = await json.GetVacancies(update.CallbackQuery.Data);
                
                if (Msk.Count is 0)
                {
                    await NoVacancies(client, update);
                }
                else if (Msk.Count > 0)
                {
                    StringBuilder sb = new ();
                    
                    FirstChunkVacancies(vacancies: Msk, index: ref mIdx, stringBuilder: sb);
                    await Send(client, update, stringBuilder: sb, remainElements: Msk.Count, city: cities[2]); 
                }
            }
            break;

            case "Санкт-Петербург":
            {
                Spb = await json.GetVacancies(update.CallbackQuery.Data);
                
                if (Spb.Count is 0)
                {
                    await NoVacancies(client, update);
                }
                else if (Spb.Count > 0)
                {
                    StringBuilder sb = new ();

                    FirstChunkVacancies(vacancies: Spb, index: ref sIdx, stringBuilder: sb);
                    await Send(client, update, stringBuilder: sb, remainElements: Spb.Count, city: cities[3]); 
                }
            }
            break;

            case "Далее":
            {
                // Челябинск
                if (update.CallbackQuery.Message.Text.Contains(cities[0])) 
                {
                    if (Chlb is null) 
                        await SendCities(client, update);

                    else
                    {
                        int remElem = 0;
                        StringBuilder sb = new ();
                        
                        NextChunkVacancies(vacancies: Chlb, index: ref cIdx, stringBuilder: sb, remainElements: ref remElem);
                        await Send(client, update, stringBuilder: sb, remainElements: remElem, city: cities[0]); 
                    }
                }
                // Екатеринбург
                else if (update.CallbackQuery.Message.Text.Contains(cities[1]))
                {
                    if (Ekb is null) 
                        await SendCities(client, update);

                    else
                    {
                        int remElem = 0;
                        StringBuilder sb = new ();
                        
                        NextChunkVacancies(vacancies: Ekb, index: ref eIdx, stringBuilder: sb, remainElements: ref remElem);
                        await Send(client, update, stringBuilder: sb, remainElements: remElem, city: cities[1]);
                    }
                }
                // Москва
                else if (update.CallbackQuery.Message.Text.Contains(cities[2])) 
                {
                    if (Msk is null) 
                        await SendCities(client, update);

                    else
                    {
                        int remElem = 0;
                        StringBuilder sb = new ();
        
                        NextChunkVacancies(vacancies: Msk, index: ref mIdx, stringBuilder: sb, remainElements: ref remElem);
                        await Send(client, update, stringBuilder: sb, remainElements: remElem, city: cities[2]);
                    }
                }
                // Санкт-Петербург
                else if (update.CallbackQuery.Message.Text.Contains(cities[3])) 
                {
                    if (Spb is null) 
                        await SendCities(client, update);

                    else
                    {
                        int remElem = 0;
                        StringBuilder sb = new ();
            
                        NextChunkVacancies(vacancies: Spb, index: ref sIdx, stringBuilder: sb, remainElements: ref remElem);
                        await Send(client, update, stringBuilder: sb, remainElements: remElem, city: cities[3]);
                    }
                }
            }
            break;        
        }
    }

    private async Task Send(ITelegramBotClient botClient, Update update, StringBuilder stringBuilder, 
                                     int remainElements, string city)
    {
        await MessageDeleter.DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, oldBotMsgId);

        if (remainElements <= 10)
        {
            await botClient.SendTextMessageAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            text: $"Список вакансий на hh.ru в городе {city}:\n{stringBuilder}",
                            parseMode: ParseMode.Html,
                            replyMarkup: backToKeyboard);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            text: $"Список вакансий на hh.ru в городе {city}:\n{stringBuilder}",
                            parseMode: ParseMode.Html,
                            replyMarkup: navKeyboard);
        }
    }
}