using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using TelegramBotScrapper.Helpers;
using Update = Telegram.Bot.Types.Update;

namespace BotScrapper;
/// <summary>
/// Простой Telegram-бот. Принимает команды от пользователя, обрабатывает кнопки и возвращает вакансии по городам  
/// </summary>
/// <remarks>
/// При отключении бота и его перепзапуска, кнопка "Далее" возвращает список городов, а не вакансии в виду 
/// NullReferenceException.
/// </remarks>
public sealed class Bot : BackgroundService
{
    private readonly ILogger<Bot> logger;
    private TelegramBotClient? Client;
    private readonly string token = "token";
    
    // BotConf
    ReceiverOptions receiverOptions;
    InlineKeyboardMarkup servicesKeyboard;

    // Helpers
    IHhRuVacancySender hhRuVacancySender;

    private int oldBotMsgId = 0; //ID сообщения для удаления
    private readonly string greetingMessage = "Добро пожаловать в парсер вакансий C#. Выберите сервис, в котором вас интересуют вакансии.";

    public Bot(IHhRuVacancySender hhRuVcSndr, ILogger<Bot> lggr)
    {
        Client = new (token);
        hhRuVacancySender = hhRuVcSndr;
        logger = lggr;

        receiverOptions = new ()
        {
            AllowedUpdates = new UpdateType[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            }
        };

        servicesKeyboard = new (new []
        {
            new InlineKeyboardButton[] { new InlineKeyboardButton("HeadHunter") { CallbackData = "hh.ru" }},
            new InlineKeyboardButton[] { new InlineKeyboardButton("Работа.Ру") { CallbackData = "rabota.ru" }},                  
        });
    }
    protected async override Task ExecuteAsync(CancellationToken cancellationToken)
    {    
        Client.StartReceiving(
                updateHandler: HandleUpdateAsync, 
                pollingErrorHandler: HandlePollingAsync, receiverOptions,
                cancellationToken: cancellationToken);

        var user = await Client.GetMeAsync();

        logger.LogInformation($"Start listening for @{user.Username}");
    }

    public async Task HandlePollingAsync(ITelegramBotClient client, Exception exception, CancellationToken token) => 
    logger.LogError($"HandException: Ошибка при работе бота:\n{exception}");
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type is UpdateType.Message)
        {
            if (update.Message.Text is "/start")
                await SendServices(botClient, update);
            
            else
            {
                var botMsg = await botClient.SendTextMessageAsync(
                                            chatId: update.Message.Chat.Id,
                                            text: "Неправильная команда. Для старта бота нажмите /start");

                await MessageDeleter.DeleteMessage(botClient, update.Message.Chat.Id, update.Message.MessageId);
                await MessageDeleter.DeleteMessage(botClient, update.Message.Chat.Id, oldBotMsgId);
                
                oldBotMsgId = botMsg.MessageId;
            }
        }
        else if (update.Type is UpdateType.CallbackQuery)   
        {
            await MessageDeleter.DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, oldBotMsgId);

            if (update.CallbackQuery.Data is "hh.ru")
            {
                if (update.CallbackQuery.Message.Text.Contains(greetingMessage) is true)
                    await MessageDeleter.DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                    
                await hhRuVacancySender.SendVacancies(botClient, update);
            }

            else if (update.CallbackQuery.Data is "rabota.ru")
                await SendServices(botClient, update);

            else if (update.CallbackQuery.Data is "К списку сервисов")
            {
                await MessageDeleter.DeleteMessage(botClient, update.CallbackQuery.Message.Chat.Id, update.CallbackQuery.Message.MessageId);
                await SendServices(botClient, update);
            }

            else if (update.CallbackQuery.Message.Text.Contains("hh")) 
                await hhRuVacancySender.SendVacancies(botClient, update);
            
            else 
            {
                var botMsg = await botClient.SendTextMessageAsync(
                                                 chatId: update.CallbackQuery.Message.Chat.Id,
                                                 text: "Ошибка обработки кнопок. Нажмите /start, чтобы перезапустить парсер.");
                    
                oldBotMsgId = botMsg.MessageId;
            }            
        }
    }

    private async Task SendServices(ITelegramBotClient botClient, Update update)
    {
        long chatId = 0;

        if (update.Type is UpdateType.CallbackQuery)
            chatId = update.CallbackQuery.Message.Chat.Id;

        else if (update.Type is UpdateType.Message)
        {
            chatId = update.Message.Chat.Id;
            await MessageDeleter.DeleteMessage(botClient, chatId, update.Message.MessageId);
        }

        var botMsg = await botClient.SendTextMessageAsync(
                                     chatId: chatId,
                                     text: greetingMessage,
                                     replyMarkup: servicesKeyboard);

        await MessageDeleter.DeleteMessage(botClient, chatId, oldBotMsgId);

        oldBotMsgId = botMsg.MessageId;        
    }
}