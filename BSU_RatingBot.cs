using System;
using System.Threading;

using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

using Parser.SQLite_Db;

namespace Parser
{

    /// <summary>
    /// <para>Made with Telegram.Bot</para>
    /// <para>Static class where all bot logic is situated.</para>
    /// <para>TODO: Захостить можно было бы на azure, heroku (бесплатно, и хоть там нельзя напрямую,
    /// но через mono пользовательский пак или через docker как-то)</para>
    /// </summary>
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
        /// <summary>
        /// Bot starts receiving messages and giving reactions to them.
        /// It also initializes timer, that ticks each minute to call NotifyUsersAsync.
        /// </summary>
        public static void StartReceiving()
        {
            _botClient.StartReceiving();
            _timer = new Timer(
                e => NotifyUsersAsync(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(1)
            );
        }
        /// <summary>
        /// Bot stops receiving messages.
        /// </summary>
        public static void StopReceiving()
        {
            _botClient.StopReceiving();
            _timer.Dispose();
        }


        /// <summary>
        /// Checks update on server. If there are some, it notifies users. 
        /// </summary>
        private static async void NotifyUsersAsync()
        {
            var users = TelegramDbRepository.GetAllNotifyUsers();
            foreach (var user in users)
            {
                string message = await Parser.ToString_GetSpecUpdateAsync(user.Spec, (uint)user.CTScore, user.RatePosition, user.Id);         
                if(message!=null)
                {
                    await _botClient.SendTextMessageAsync(
                      chatId: user.Id,
                      text: message
                    );
                }
            }
        }





        /// <summary>
        /// Calls when bot receives message from user. Is needed to handle commands.
        /// </summary>
        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text == null)
                return;



            //Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id}.");

            long id = e.Message.Chat.Id; //id of the chat is the id of the user if db here
            var status = TelegramDbRepository.GetStatusAndCreateIfNotExist(id);
            //Console.WriteLine(status);


            string textToSend = "";     //bot answer
            var replyKeyboardMarkup = new ReplyKeyboardMarkup();
            var specEnterKeyboard = new KeyboardButton[][]  //custom keyboard (applyes to reply later)
            {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("информатика"),
                        new KeyboardButton("прикладная информатика (направление - программное обеспечение компьютерных систем)"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("теология"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("история (по направлениям)"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("математика и информационные технологии (направление - веб-программирование и интернет-технологии)"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("геоинформационные системы (направление - земельно-кадастровые)"),
                        new KeyboardButton("геоэкология"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("информация и коммуникация"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("международное право"),
                        new KeyboardButton("международные отношения"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("экономическая информатика"),
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("правоведение"),
                    },
            };
            switch (status)     //handling different situations (in order to answer on multiple bot's questions after commands)
            {
                case Status.Default:
                case Status.ToNotify:
                    {
                        switch (e.Message.Text)     //handling commands (may be not so good practice)
                        {
                            case "/start":
                            case "/help":
                                {
                                    textToSend = "Доступных команд:\n" +
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
                                    replyKeyboardMarkup.Keyboard = specEnterKeyboard;
                                    TelegramDbRepository.UpdateUser(id, Status.SpecEnterWaiting);
                                    break;
                                }
                            case "/notify":
                                {
                                    textToSend = "Введите вашу специальность:";
                                    replyKeyboardMarkup.Keyboard = specEnterKeyboard;
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
                        replyKeyboardMarkup.Keyboard = specEnterKeyboard;
                        break;
                    }
                case Status.ToNotify_SpecEnterWaiting:
                    {
                        var spec = await Parser.GetSpecRowAsync(e.Message.Text.ToLower());
                        if (spec == null)
                        {
                            textToSend = "Такой специальности не существует! Введите данные ещё раз";
                            replyKeyboardMarkup.Keyboard = specEnterKeyboard;
                        }
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
              text: textToSend,
              replyMarkup: replyKeyboardMarkup.Keyboard != null ?
                                    replyKeyboardMarkup : null
            );
        }
    }
}
