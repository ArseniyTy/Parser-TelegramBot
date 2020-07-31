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
                .Where(u => u.Status == Status.ToNotify)
                .ToList();
        }
        public static Status GetStatusAndCreateIfNotExist(long id)
        {
            var db = new TelegramContext();
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                db.Users.Add(new User()
                {
                    Id = id,
                    Status = Status.Default,
                });
                db.SaveChanges();
                user = db.Users.FirstOrDefault(u => u.Id == id);
                //Console.WriteLine("User added to db");
            }
            //else
            //    Console.WriteLine("User is already in db");

            return user.Status;
        }

        public static void UpdateUser(long id, Status? status = null, string spec = null, uint? score = null, uint? pos = null)
        {
            var db = new TelegramContext();
            var user = db.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                if (status != null)
                    user.Status = (Status) status;
                if (spec != null)
                    user.Spec = spec;
                if (score != null && score >= 0)
                    user.CTScore = score;
                if (pos != null && pos >= 0)
                    user.RatePosition = pos;

                db.Users.Update(user);
                db.SaveChanges();
                //Console.WriteLine("User Updated!");
            }
            //else
            //    Console.WriteLine("EXCEPTION!!!!!!!!!");
        }

        public static DateTime? LastUpdateTime
        {
            get 
            {
                var db = new TelegramContext();
                var time = db.LastUpdateTime.FirstOrDefault();
                if (time == null)
                    return null;

                return time.Time;
            }
            set 
            {
                var db = new TelegramContext();
                db.LastUpdateTime.RemoveRange(db.LastUpdateTime);
                if (value == null)
                    throw new Exception("Can't apply null to TimeUpdate.Time!");
                db.LastUpdateTime.Add(new TimeUpdate { Time = (DateTime)value });
                db.SaveChanges();
            }
        }

    }
}
