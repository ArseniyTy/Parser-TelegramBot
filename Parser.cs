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


            var scoreDict = new Dictionary<string, int>();
            int scorePrefix = 40;
            for (int i = 8; i < specData.Count - 1; i++)
            {
                string key = $"{scorePrefix - 1}1-{scorePrefix--}0"; //will be: "391-400"
                if(specData[i] != "")
                    scoreDict.Add(key, int.Parse(specData[i]));
            }
            if (specData[specData.Count - 1] != "")
                scoreDict.Add("000-120", int.Parse(specData[specData.Count - 1]));
            
            //int peopleLess340Count = 0;
            //for (int i = 14; i < specData.Count; i++)
            //    peopleLess340Count += specData[i] != "" ? int.Parse(specData[i]) : 0;
            //scoreDict.Add("000-340", peopleLess340Count);


            var info = new Dictionary<string, Object>
            {
                { "Специальность", spec },
                { "Последнее обновление", updateTime },
                { "Заявлений", int.Parse(specData[4]) },
                { "Макс на бюджет", int.Parse(specData[1]) },
                { "Макс на платку", int.Parse(specData[3]) },
                { "Олимпиадники", specData[6] == "" ? 0 : int.Parse(specData[6]) },
                { "Распределение по баллам", scoreDict },
            };
            return info;
        }
    }
}
