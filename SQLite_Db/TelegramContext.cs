using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.SQLite_Db
{
    class TelegramContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public TelegramContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=TelegramUsers.db");
        }
    }
}
