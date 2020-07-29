using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parser.SQLite_Db
{
    static class TelegramDbRepository
    {
        public static IList<User> GetAllNotifyUsers()
        {
            var db = new TelegramContext();
            return db.Users
                .Where(u => u.Status == Status.Update)
                .ToList();
        }
        public static Status GetStatus(long id)
        {
            var db = new TelegramContext();
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                db.Users.Add(new User()
                {
                    Id = id,
                    Status = Status.Start,
                });
                db.SaveChanges();
                user = db.Users.FirstOrDefault(u => u.Id == id);
                Console.WriteLine("User added to db");
            }
            else
                Console.WriteLine("User is already in db");

            return user.Status;
        }

        public static void UpdateUser(long id, Status? status = null, string spec = null, bool? documentsApplied = null, uint? score = null)
        {
            var db = new TelegramContext();
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if (status != null)
                    user.Status = (Status) status;
                if (spec != null)
                    user.Spec = spec;
                if (documentsApplied != null)
                    user.IfDocumentsApplied = documentsApplied;
                if (score != null && score >= 0)
                    user.CTScore = score;

                db.Users.Update(user);
                db.SaveChanges();
                Console.WriteLine("User Updated!");
            }
            else
                Console.WriteLine("EXCEPTION!!!!!!!!!");
        }
    }
}
