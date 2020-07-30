using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parser.SQLite_Db;

namespace Parser
{
    class Program
    {
        static async Task Main()
        {
            //using (var db = new TelegramContext())
            //{
            //    db.Database.Migrate();
            //}


            BSU_RatingBot.StartReceiving();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            BSU_RatingBot.StopReceiving();


            //Console.WriteLine(await Parser.ToString_GetSpecInfoAsync("экономическая информатика"));
        }
    }
}
