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
            using (var db = new TelegramContext())
            {
                db.Database.Migrate();
                //db.Users.RemoveRange(db.Users);
                //db.SaveChanges();
            }


            BSU_RatingBot.StartReceiving();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            BSU_RatingBot.StopReceiving();



            //string specPI = "прикладная информатика (направление - программное обеспечение компьютерных систем)";
            //Console.WriteLine(await Parser.ToString_GetSpecInfoAsync(specPI));
        }
    }
}
