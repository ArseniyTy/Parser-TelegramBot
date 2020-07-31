using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AngleSharp;

using Parser.SQLite_Db;

namespace Parser
{
    /// <summary>
    /// <para>Made with AngleSharp</para>
    /// <para>Static class that parses webPage and processes this info</para>
    /// </summary>
    static class Parser
    {
        private static readonly IConfiguration config = Configuration.Default.WithDefaultLoader();
        private static readonly IBrowsingContext context = BrowsingContext.New(config);
        private const string URL = "https://abit.bsu.by/formk1?id=1";

        /// <summary>
        /// It calls when you need to check on server: are there any updates?
        /// </summary>
        /// <param name="spec">Users specialty</param>
        /// <param name="userScore">Users CT score</param>
        /// <param name="prevPos">Users previos update position in rating</param>
        /// <param name="userId">Users id</param>
        /// <returns>String with info or null if no updates</returns>
        public static async Task<string> ToString_GetSpecUpdateAsync(string spec, uint userScore, uint? prevPos, long userId)
        {
            var info = await GetSpecInfoAsync(spec);

            var updateTime = DateTime.Parse((string)info["Последнее обновление"]);
            if (updateTime == TelegramDbRepository.LastUpdateTime && prevPos!=null)
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

            TelegramDbRepository.UpdateUser(userId, pos: (uint?) pos);
            return $"Обновление!\n" +
                $"Вы {pos}/{maxPeople} в конкурсе (в худшем случае)!\n" +
                $"На специальности {info["Олимпиадники"]} олимпиадников";
        }


        /// <summary>
        /// Converts info about speciality from GetSpecInfoAsync to string
        /// </summary>
        /// <param name="spec">Speciality you want to get info about</param>
        /// <returns>String with info about speciality</returns>
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

        /// <summary>
        /// Contains some logic about processing parsed info
        /// </summary>
        /// <param name="spec">Speciality you want to get info about</param>
        /// <returns>Dictionary with info about speciality</returns>
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

                { "Средний балл", allScore/(applications-olimpCount) },
                { "Проходной балл", enterScore },

                { "Олимпиадники",  olimpCount },
                { "Распределение по баллам", scoreDict },
            };
            return info;
        }


        /// <summary>
        /// Parses row with speciality data
        /// </summary>
        /// <param name="spec">Speciality you want to get info about</param>
        /// <returns>List of string data about speciality</returns>
        public async static Task<List<string>> GetSpecRowAsync(string spec)
        {
            var document = await context.OpenAsync(URL);
            var tableRows = document.QuerySelectorAll("#Abit_K11_TableResults tbody tr");

            var specRow = tableRows
                .FirstOrDefault(row => row.QuerySelector("td.vl") != null ?
                row.QuerySelector("td.vl").TextContent == spec : false);

            if (specRow == null)
                return null;


            var data = specRow.QuerySelectorAll("td")
                              .ToList();
            if (!data[0].ClassList.Contains("vl"))
                data.RemoveRange(0, 2);

            return data.Select(t => t.TextContent)
                       .ToList();
        }
    }
}
