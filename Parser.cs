using AngleSharp;
using AngleSharp.Dom;
using Parser.SQLite_Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parser
{
    static class Parser
    {
        private static readonly IConfiguration config = Configuration.Default.WithDefaultLoader();
        private static readonly IBrowsingContext context = BrowsingContext.New(config);
        private const string URL = "https://abit.bsu.by/formk1?id=1";

        public static async Task<string> ToString_GetSpecUpdateAsync(string spec, uint userScore, uint? prevPos)
        {
            var info = await GetSpecInfoAsync(spec);

            var updateTime = DateTime.Parse((string)info["Последнее обновление"]);
            if (updateTime == TelegramDbRepository.LastUpdateTime)
                return null;
            else
                TelegramDbRepository.LastUpdateTime = updateTime;


            var dictScore = (Dictionary<string, int>) info["Распределение по баллам"];
            int maxPeople = (int) info["Макс на бюджет"];
            int pos = (int) info["Олимпиадники"];
            foreach (var score in dictScore)
            {
                uint u_bound = uint.Parse(score.Key.Split('-')[1]);
                if (userScore <= u_bound)
                    pos+= score.Value;
            }
            if (prevPos == pos)
                return null;

            return $"Обновление!\n" +
                $"Вы {pos}/{maxPeople} в конкурсе (в худшем случае)!\n" +
                $"На специальности {info["Олимпиадники"]} олимпиадников";
        }
        public static async Task<string> ToString_GetSpecInfoAsync(string spec)
        {
            var info = await GetSpecInfoAsync(spec);

            string toReturn = "";
            foreach (var infoObj in info)
            {
                if(infoObj.Key== "Распределение по баллам")
                {
                    toReturn += $"\n{infoObj.Key}:";
                    foreach (var score in (Dictionary<string, int>)infoObj.Value)
                        toReturn += $"\n\t{score.Key}:\t{score.Value}";
                }
                else
                    toReturn += $"\n{infoObj.Key}:\t{infoObj.Value}";
            }
            return toReturn.Remove(0,1);
        }

        public async static Task<List<string>> GetSpecRowAsync(string spec)
        {
            var document = await context.OpenAsync(URL);
            var tableRows = document.QuerySelectorAll("#Abit_K11_TableResults tbody tr");

            var specRow = tableRows
                .FirstOrDefault(row => row.QuerySelector("td.vl") != null ?
                row.QuerySelector("td.vl").TextContent == spec : false);

            if(specRow==null)
                return null;

            return specRow.QuerySelectorAll("td")
                           .Select(t => t.TextContent)
                           .ToList();
        }

        private async static Task<Dictionary<string, object>> GetSpecInfoAsync(string spec)
        {
            var document = await context.OpenAsync(URL);
            var updateTime = document.QuerySelector("#Abit_K11_lbCurrentDateTime").TextContent;

            List<string> specData = await GetSpecRowAsync(spec);
            if (specData == null)
                throw new Exception("specData is null!");

            int olimpCount = specData[6] == "" ? 0 : int.Parse(specData[6]);
            int leftForFree = int.Parse(specData[1])- olimpCount;
            string enterScore = "все приняты";
            int allScore = 0;

            var scoreDict = new Dictionary<string, int>();
            int scorePrefix = 400;
            int i = 8;
            while(i < specData.Count)
            {
                int numOfPeople = specData[i] == "" ? 0 : int.Parse(specData[i]);
                if (numOfPeople != 0)
                {
                    string key;
                    if (i == specData.Count - 1)
                    {
                        key = "000-120";
                        allScore += numOfPeople * 100;
                    }
                    else
                    {
                        key = $"{scorePrefix - 9}-{scorePrefix}"; //will be: "391-400"
                        allScore += numOfPeople * (scorePrefix - 5);
                    }
                    scoreDict.Add(key, numOfPeople);


                    leftForFree -= numOfPeople;
                    if (leftForFree <= 0 && enterScore== "все приняты")
                        enterScore = key;
                }
                scorePrefix -= 10;
                i++;
            }


            int applications = specData[4]=="" ? 0 : int.Parse(specData[4]);
            var info = new Dictionary<string, Object>
            {
                { "Специальность", spec },
                { "Последнее обновление", updateTime },

                { "Заявлений", applications },
                { "Макс на бюджет", int.Parse(specData[1]) },
                { "Макс на платку", int.Parse(specData[3]) },

                { "Средний балл", allScore/applications },
                { "Проходной балл", enterScore },

                { "Олимпиадники",  olimpCount },
                { "Распределение по баллам", scoreDict },
            };
            return info;
        }
    }
}
