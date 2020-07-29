using System;
using System.Collections.Generic;
using System.Text;

namespace Parser.SQLite_Db
{
    enum Status
    {
        Default,
        SpecEnterWaiting,
        ToNotify_SpecEnterWaiting,
        ToNotify_ScoreEnterWaiting,
        ToNotify
    }
}
