using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

using Update = Telegram.Bot.Types.Update;

using JsonCrud;

namespace BotSpace;
/// <summary>
/// Простой Telegram-бот. Принимает команды от пользователя, обрабатывает кнопки и возвращает вакансии по городам  
/// </summary>
/// <remarks>
/// При отключении бота и его перепзапуска, кнопка "Далее" возвращает список городов, а не вакансии в виду 
/// NullReferenceException.
/// </remarks>
sealed class Bot
{
    private TelegramBotClient? Client;
    private readonly string token = "6090888687:AAGRhpeWsQ0G7_yMKxKgV5GtdqoOngNpNnQ";
    ReceiverOptions receiverOptions;

    InlineKeyboardMarkup inlineKeyboardMarkup;
    InlineKeyboardMarkup navKeyboard;
    InlineKeyboardMarkup citiesKeyboard;

    private int oldBotMsgId = 0; //ID сообщения для удаления
    private readonly string greetingMessage = "Добро пожаловать в парсер вакансий C#. Выберите город, в котором вас интересует список вакансий.";
    private readonly string[] cities = {"Челябинск", "Екатеринбург", "Москва", "Санкт-Петербург"};

    JsonVacancy json;   
    private IReadOnlyList<(string, string)> Chlb, Ekb, Msk, Spb;
    private int c, e, m, s;  //индексы для доступа к элементам массивов вакансий

    public Bot()
    {
        Client = new (token);
        json = new ();

        receiverOptions = new ()
        {
            AllowedUpdates = new UpdateType[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            }
        };

        inlineKeyboardMarkup = new (new []
        {
            new InlineKeyboardButton[] { new InlineKeyboardButton(cities[0]) { CallbackData = cities[0] },
                                         new InlineKeyboardButton(cities[1]) { CallbackData = cities[1] }
                                       },
                        
            new InlineKeyboardButton[] { new InlineKeyboardButton(cities[2]) { CallbackData = cities[2] },
                                         new InlineKeyboardButton(cities[3]) { CallbackData = cities[3] }
                                       }
        });

        var citiesButton = new InlineKeyboardButton[] { "К списку городов" };
        navKeyboard = new (new []
        {
            new InlineKeyboardButton[] { "Далее" },
            citiesButton
        });
        citiesKeyboard = new (new []
        {
            citiesButton
        });
    } 
    
    public async Task Start()
    {
        using CancellationTokenSource cts = new ();

        Client.StartReceiving(
               updateHandler: HandleUpdateAsync, 
               pollingErrorHandler: HandlePollingAsync, receiverOptions,
               cancellationToken: cts.Token );

        var user = await Client.GetMeAsync();

        Console.WriteLine($"Start listening for @{user.Username}");
        Console.ReadLine();

        cts.Cancel();
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type is UpdateType.Message)
        {
            if (update.Message.Text.ToLower() == "/start")
            {
                await SendCities(botClient, update);
            }
            else
            {
                var botMsg = await botClient.SendTextMessageAsync(
                                             chatId: update.Message.Chat.Id,
                                             text: "Неправильная команда. Для старта бота нажмите /start");

                await DeleteMessage(botClient, update.Message.Chat.Id, update.Message.MessageId);
                await DeleteMessage(botClient, update.Message.Chat.Id, oldBotMsgId);
                
                oldBotMsgId = botMsg.MessageId;
            }
        }
        else if (update.Type is UpdateType.CallbackQuery)
        {
            await DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, oldBotMsgId);

            switch(update.CallbackQuery.Data)
            {
                case "Челябинск":
                {
                    Chlb = await json.GetVacancies(update.CallbackQuery.Data);
                    
                    if (Chlb is null || Chlb.Count is 0)
                    {
                        await NoVacancies(botClient, update);        
                    }
                    else
                    {
                        StringBuilder sb = new ();
                        
                        FirstChunkVacancies(vacancies: Chlb, stringBuilder: sb, index: ref c);
                        await SendVacancies(botClient, update, stringBuilder: sb, remainElements: Chlb.Count, city: cities[0]); 
                    }
                }
                break;

                case "Екатеринбург":
                {
                    Ekb = await json.GetVacancies(update.CallbackQuery.Data);
                    
                    if (Ekb is null || Ekb.Count is 0)
                    {
                        await NoVacancies(botClient, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        FirstChunkVacancies(vacancies: Ekb, stringBuilder: sb, index: ref e);
                        await SendVacancies(botClient, update, stringBuilder: sb, remainElements: Chlb.Count, city: cities[1]); ; 
                    }
                }
                break;

                case "Москва":
                {
                    Msk = await json.GetVacancies(update.CallbackQuery.Data);
                    
                    if (Msk is null || Msk.Count is 0)
                    {
                        await NoVacancies(botClient, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();
                        
                        FirstChunkVacancies(vacancies: Msk, stringBuilder: sb, index: ref m);
                        await SendVacancies(botClient, update, stringBuilder: sb, remainElements: Chlb.Count, city: cities[2]); 
                    }
                }
                break;

                case "Санкт-Петербург":
                {
                    Spb = await json.GetVacancies(update.CallbackQuery.Data);
                    
                    if (Spb is null || Spb.Count is 0)
                    {
                        await NoVacancies(botClient, update);
                    }
                    else
                    {
                        StringBuilder sb = new ();

                        FirstChunkVacancies(vacancies: Spb, stringBuilder: sb, index: ref s);
                        await SendVacancies(botClient, update, stringBuilder: sb, remainElements: Chlb.Count, city: cities[3]); 
                    }
                }
                break;

                case "Далее":
                {
                    try
                    {
                        if (update.CallbackQuery.Message.Text.Contains(cities[0])) // Челябинск
                        {
                            int remElem = 0;

                            StringBuilder sb = new ();
                            
                            NextChunkVacancies(vacancies: Chlb, index: ref c, stringBuilder: sb, remainElements: ref remElem);
                            await SendVacancies(botClient, update, stringBuilder: sb, remainElements: remElem, city: cities[0]);                                                                       
                        }
                        else if (update.CallbackQuery.Message.Text.Contains(cities[1])) // Екатеринбург
                        {
                            int remElem = 0;

                            StringBuilder sb = new ();
                            
                            NextChunkVacancies(vacancies: Ekb, index: ref e, stringBuilder: sb, remainElements: ref remElem);
                            await SendVacancies(botClient, update, stringBuilder: sb, remainElements: remElem, city: cities[1]);
                        }
                        else if (update.CallbackQuery.Message.Text.Contains(cities[2])) // Москва
                        {
                            int remElem = 0;

                            StringBuilder sb = new ();
            
                            NextChunkVacancies(vacancies: Msk, stringBuilder: sb, remainElements: ref remElem, index: ref m);
                            await SendVacancies(botClient, update, stringBuilder: sb, remainElements: remElem, city: cities[2]);
                        }
                        else if (update.CallbackQuery.Message.Text.Contains(cities[3])) // Санкт-Петербург
                        {
                            int remElem = 0;
                            
                            StringBuilder sb = new ();
                
                            NextChunkVacancies(vacancies: Spb, stringBuilder: sb, remainElements: ref remElem, index: ref s);
                            await SendVacancies(botClient, update, stringBuilder: sb, remainElements: remElem, city: cities[3]);
                        }
                    }
                    catch (Exception)
                    {
                        await SendCities(botClient, update);
                    }
                }
                break;

                case "К списку городов":
                {
                    await SendCities(botClient, update);                    
                }
                break;

                default:
                {
                    var botMsg = await botClient.SendTextMessageAsync(
                                                 chatId: update.CallbackQuery.Message.Chat.Id,
                                                 text: "Ошибка обработки кнопок. Нажмите /start, чтобы перезапустить парсер.");
                    
                    oldBotMsgId = botMsg.MessageId;                    
                }
                break;
            }
        }
    }

    public async Task HandlePollingAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"HandException: {exception}");
        
        Bot bt = new ();
        await bt.Start();
    }

    private async Task SendCities(ITelegramBotClient botClient, Update update)
    {
        long chatId = 0;

        if (update.Type is UpdateType.CallbackQuery)
        {
            chatId = update.CallbackQuery.Message.Chat.Id;
        }
        else if (update.Type is UpdateType.Message)
        {
            chatId = update.Message.Chat.Id;
            await DeleteMessage(botClient, chatId, update.Message.MessageId);
        }

        var botMsg = await botClient.SendTextMessageAsync(
                                     chatId: chatId,
                                     text: greetingMessage,
                                     replyMarkup: inlineKeyboardMarkup);
        
        await DeleteMessage(botClient, chatId, oldBotMsgId);

        oldBotMsgId = botMsg.MessageId;
    }

    private async Task DeleteMessage(ITelegramBotClient botClient, long chatId, int msgId)
    {
        try
        {
            await botClient.DeleteMessageAsync(
                            chatId: chatId,
                            messageId: msgId);
        }
        catch (Exception) {};
    }

    private void FirstChunkVacancies(IReadOnlyList<(string, string)> vacancies, StringBuilder stringBuilder, ref int index)
    {
        if (vacancies.Count <= 10)
        {
            for (index = 0; index < vacancies.Count; index++)
            {
                stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
            }
        }
        else
        {
            for (index = 0; index < 10; index++)
            {
                stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
            }
        }
    }

    private void NextChunkVacancies(IReadOnlyList<(string, string)> vacancies, StringBuilder stringBuilder, ref int remainElements, ref int index)
    {
        remainElements = vacancies.Count - index;

        if (remainElements > 10)
        {
            for (int j = index + 10; index < j; index++)
            {
                stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
            }
        }
        else
        {
            for (; index < vacancies.Count; index++)
            {
                stringBuilder.Append($"<a href=\"{vacancies[index].Item1}\">{vacancies[index].Item2}\n</a>");
            }

            index = 0;
        }
    }

    private async Task SendVacancies(ITelegramBotClient botClient, Update update, StringBuilder stringBuilder, 
                                     int remainElements, string city)
    {
        if (remainElements <= 10)
        {
            await botClient.SendTextMessageAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            text: $"Список вакансий в городе {city}:\n{stringBuilder.ToString()}",
                            parseMode: ParseMode.Html,
                            replyMarkup: citiesKeyboard);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                            chatId: update.CallbackQuery.Message.Chat.Id,
                            text: $"Список вакансий в городе {city}:\n{stringBuilder.ToString()}",
                            parseMode: ParseMode.Html,
                            replyMarkup: navKeyboard);
        }
    }

    private async Task NoVacancies(ITelegramBotClient botClient, Update update)
    {
        var botMsg = await botClient.SendTextMessageAsync(
                                     chatId: update.CallbackQuery.Message.Chat.Id,
                                     text: $"В этом городе нет подходящих вакансий.",
                                     replyMarkup: citiesKeyboard);
                        
        oldBotMsgId = botMsg.MessageId;
    }
}