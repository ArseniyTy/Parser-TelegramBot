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
                string message = await Parser.ToString_GetSpecInfoAsync(user.Spec);
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
                var status = TelegramDbRepository.GetStatus(id);
                Console.WriteLine(status);
                string textToSend = "";
                switch(status)
                {
                    case Status.Start:
                        {
                            textToSend = "Данный бот позволяет следить за вашей позицией " +
                                "в рейтинге абитуриентов БГУ. Но сначало " +
                                "необходимо узнать ваши данные (если вы ввели что-то " +
                                "неправильно, то позже будет возможность это исправить). " +
                                "Итак, на какую специальность вы поступаете?";
                            //TelegramDbRepository.UpdateUser(id, Status.Faculty);
                            TelegramDbRepository.UpdateUser(id, Status.Spec);
                            break;
                        }
                    //case Status.Faculty:
                    //    {
                    //        textToSend = "Пока что ФАКУЛЬТЕТ не обработан!";
                    //        TelegramDbRepository.UpdateUser(id, Status.Spec);
                    //        break;
                    //    }
                    case Status.Spec:
                        {
                            var spec = await Parser.GetSpecRowAsync(e.Message.Text);
                            if(spec==null)
                                textToSend = "Такой специальности не существует! Введите данные ещё раз";
                            else
                            {
                                textToSend = "Отлично! Вы уже подали документы в приёмную комиссию? (да/нет)";
                                TelegramDbRepository.UpdateUser(id, Status.IfDocumentsApplied, e.Message.Text.ToLower());
                            }
                            break;
                        }
                    case Status.IfDocumentsApplied:
                        {
                            bool? value = null;
                            if (e.Message.Text.ToLower() == "да")
                                value = true;
                            else if (e.Message.Text.ToLower() == "нет")
                                value = false;
                            
                            if(value!=null)
                            {
                                textToSend = "Отлично! Теперь напишите ваш балл:";
                                TelegramDbRepository.UpdateUser(id, Status.Score, documentsApplied: value);
                            }
                            else
                                textToSend = "Введите да/нет!";

                            break;
                        }
                    case Status.Score:
                        {
                            uint score;
                            try { score = uint.Parse(e.Message.Text); }
                            catch 
                            { 
                                textToSend = "Неправильный формат ввода! Введите ещё раз!";
                                break;
                            }
                            if(score>400)
                            {
                                textToSend = "Балл должен быть меньше 400! Введите ещё раз!";
                                break;
                            }

                            //textToSend = "Отлично! Осталось последнее: как часто вы хотите получать уведомления?";
                            //TelegramDbRepository.UpdateUser(id, Status.UpdateOptions, score: score);
                            textToSend = "Всё готово! Теперь при изменении вашей позиции в рейтинге бот будет вас " +
                                "уведомлять об этом. Бот также может выполнять другие полезные функции (напишите " +
                                "help, чтобы узнать больше!";
                            TelegramDbRepository.UpdateUser(id, Status.Update, score: score);
                            break;
                        }
                    //case Status.UpdateOptions:
                    //    {
                    //        textToSend = "Пока что не обработан!";
                    //        break;
                    //    }
                    case Status.Update:
                        {
                            //обработка help и других команд
                            textToSend = "Пока что не обработан!";
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
