using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.SQLite_Db
{
    class User
    {
        public long Id { get; set; }
        public uint? CTScore { get; set; }
        public bool? IfDocumentsApplied { get; set; }
        public string Spec { get; set; }
        //public string UpdateOptions { get; set; } //enum
        public Status Status { get; set; }
    }
}
