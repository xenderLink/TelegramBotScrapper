using Telegram.Bot;

namespace TelegramBotScrapper.Helpers;

public static class MessageDeleter
{
    public static async Task DeleteMessage(ITelegramBotClient client, long chatId, int msgId)
    {
        try
        {
            await client.DeleteMessageAsync(
                            chatId: chatId,
                            messageId: msgId);
        }
        catch (Exception) {};
    }
}