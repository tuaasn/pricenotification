using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace TelegramBot
{
    public class TelegramBot
    {
        private ITelegramBotClient botClient;
        private UpdateHandler updateHandler;


        public event EventHandler<MessageArgs> OnMessageRegister;
        public TelegramBot(string tokenKey)
        {
            botClient = new TelegramBotClient(tokenKey);
        }
        public async Task InitReceive()
        {
            try
            {
                updateHandler = new UpdateHandler(this);
                await botClient.ReceiveAsync(updateHandler);
            }
            finally { }
        }


        public async Task SendMessage(string message, long chatId)
        {
            try
            {
                var result = await botClient.SendTextMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task SendMessage(string message, string username)
        {
            //string urlString = "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}";
            //string apiToken = "my_bot_api_token";
            //string chatId = "@my_channel_name";
            //string text = "Hello world!";
            //urlString = String.Format(urlString, apiToken, chatId, text);
            try
            {
                var result = await botClient.SendTextMessageAsync(username, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public async Task SendFileMessage(string filePath, string caption, long chatId)
        {
            try
            {
                Stream stream = System.IO.File.Open(filePath, FileMode.Open);
                InputOnlineFile input = new InputOnlineFile(stream);
                var result = await botClient.SendPhotoAsync(chatId, input, caption);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task HandleUpdate(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            MessageArgs messageArgs = new MessageArgs();
            messageArgs.Message = update.Message;
            OnMessageRegister(null, messageArgs);
            return Task.CompletedTask;
        }

    }
    public class MessageArgs : EventArgs
    {
        public Message Message { get; set; }
    }
    public class UpdateHandler : IUpdateHandler
    {
        private TelegramBot telegramBot;
        public UpdateHandler(TelegramBot telegramBot)
        {
            this.telegramBot = telegramBot;
        }

        public UpdateType[] AllowedUpdates => new UpdateType[] { UpdateType.Message };

        public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            return telegramBot.HandleError(botClient, exception, cancellationToken);
        }

        public Task HandleUpdate(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            return telegramBot.HandleUpdate(botClient, update, cancellationToken);
        }
    }
}
