using AngleSharp.Dom.Events;
using Parser.SQLite_Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace Parser
{
    //TODO: 1) Поставить, чтобы он обновлял только если что-то меняется (просматривает 
    // сначала дату, а потом старую позицию (поместить в бд)!
    // поставить, чтобы чекал раз в минуту мб
    // 2) Захостить
    // 3) Сделать кнопки по выбору специальностей!
    static class BSU_RatingBot
    {
        private static Timer _timer;
        private static ITelegramBotClient _botClient;
        private const string ACCESS_TOKEN = "1182722222:AAEqxHofQbnKxwf7q8Mbi9EunUc4wS4R4aQ";
        static BSU_RatingBot()
        {
            _botClient = new TelegramBotClient(ACCESS_TOKEN);
            _botClient.OnMessage += Bot_OnMessage;
        }
        public static void StartReceiving() 
        { 
            _botClient.StartReceiving();
            _timer = new Timer(
                e => NotifyUsersAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5)
            );
        }
        public static void StopReceiving() 
        { 
            _botClient.StopReceiving(); 
            _timer.Dispose(); 
        }

        //сравнивать время и обновлять!
        private static async void NotifyUsersAsync()
        {
            var users = TelegramDbRepository.GetAllNotifyUsers();
            foreach(var user in users)
            {
                string message = await Parser.ToString_GetSpecUpdateAsync(user.Spec, (uint) user.CTScore);
                await _botClient.SendTextMessageAsync(
                  chatId: user.Id,
                  text: message
                );
            }
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

                //проверяю, есть ли такой чат в бд, если нет добавляю и ставлю начальное состояние
                //если есть, то беру текущее состояние. Потом через switch отвечаю на это состояние
                //само состояние можно через enum попробовать

                long id = e.Message.Chat.Id;
                var status = TelegramDbRepository.GetStatusAndCreateIfNotExist(id);
                Console.WriteLine(status);
                string textToSend = "";
                switch (status)
                {
                    case Status.Default:
                    case Status.ToNotify:
                        {
                            switch(e.Message.Text)
                            {
                                case "/start":
                                case "/help":
                                    {
                                        textToSend = "У бота есть несколько доступных команд:\n" +
                                            "1) /help - получение инструкции (что вы читаете сейчас)\n" +
                                            "2) /info - получение информации о любой специальности\n" +
                                            "3) /notify - начать получать уведомления об изменении вашей " +
                                            "позиции в конкурсе\n" +
                                            "4) /no_notify - отмена уведомлений";
                                        break;
                                    }
                                case "/info":
                                    {
                                        textToSend = "Введите специальность:";
                                        TelegramDbRepository.UpdateUser(id, Status.SpecEnterWaiting);
                                        break;
                                    }
                                case "/notify":
                                    {
                                        textToSend = "Введите вашу специальность:";
                                        TelegramDbRepository.UpdateUser(id, Status.ToNotify_SpecEnterWaiting);
                                        break;
                                    }
                                case "/no_notify":
                                    {
                                        textToSend = "Вы больше не получаете уведомлений";
                                        TelegramDbRepository.UpdateUser(id, Status.Default);
                                        break;
                                    }
                                default:
                                    {
                                        textToSend = "Введите /help для того, чтобы узнать, что я умею!";
                                        break;
                                    }
                            }
                            break;
                        }
                    case Status.SpecEnterWaiting:
                        {
                            try
                            {
                                textToSend = await Parser.ToString_GetSpecInfoAsync(e.Message.Text.ToLower());
                                TelegramDbRepository.UpdateUser(id, Status.Default);
                            }
                            catch
                            {
                                textToSend = "Такой специальности не существует! Введите ещё раз:";
                            }
                            break;
                        }
                    case Status.ToNotify_SpecEnterWaiting:
                        {
                            var spec = await Parser.GetSpecRowAsync(e.Message.Text.ToLower());
                            if (spec == null)
                                textToSend = "Такой специальности не существует! Введите данные ещё раз";
                            else
                            {
                                textToSend = "Отлично! Теперь напишите ваш балл:";
                                TelegramDbRepository.UpdateUser(id, Status.ToNotify_ScoreEnterWaiting, e.Message.Text.ToLower());
                            }
                            break;
                        }
                    case Status.ToNotify_ScoreEnterWaiting:
                        {
                            uint score;
                            try { score = uint.Parse(e.Message.Text); }
                            catch
                            {
                                textToSend = "Неправильный формат ввода! Введите ещё раз!";
                                break;
                            }
                            if (score > 400)
                            {
                                textToSend = "Балл всегда меньше 400! Введите ещё раз!";
                                break;
                            }

                            textToSend = "Всё готово! Теперь вам будут приходить уведомления!";
                            TelegramDbRepository.UpdateUser(id, Status.ToNotify, score: score);
                            break;
                        }
                }

                await _botClient.SendTextMessageAsync(
                  chatId: e.Message.Chat,
                  text: textToSend
                );
            }
        }
    }
}
