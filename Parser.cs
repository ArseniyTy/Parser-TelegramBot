using AngleSharp;
using AngleSharp.Dom;
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


        //public static async Task Console_GetFacultiesAsync(string spec)
        //{
        //    var info = await GetSpecInfoAsync(spec);

        //    foreach (var infoObj in info)
        //    {
        //        var dict = infoObj.Value as Dictionary<string, int>;
        //        if (dict != null)
        //        {
        //            Console.WriteLine($"{infoObj.Key}:");
        //            foreach (var score in dict)
        //                Console.WriteLine($"\t{score.Key}:\t{score.Value}");
        //        }
        //        else
        //            Console.WriteLine($"{infoObj.Key}:\t{infoObj.Value}");
        //    }
        //}

        public static async Task<string> ToString_GetSpecInfoAsync(string spec)
        {
            var info = await GetSpecInfoAsync(spec);

            string toReturn = "";
            foreach (var infoObj in info)
            {
                var dict = infoObj.Value as Dictionary<string, int>;
                if (dict != null)
                {
                    toReturn += $"\n{infoObj.Key}:";
                    foreach (var score in dict)
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

        public async static Task<Dictionary<string, object>> GetSpecInfoAsync(string spec)
        {
            var document = await context.OpenAsync(URL);
            var updateTime = document.QuerySelector("#Abit_K11_lbCurrentDateTime").TextContent;

            List<string> specData = await GetSpecRowAsync(spec);
            if (specData == null)
                throw new Exception("specData is null!");


            var scoreDict = new Dictionary<string, int>();
            int scorePrefix = 40;
            for (int i=8; i < specData.Count - 1; i++)
            {
                string key = $"{scorePrefix - 1}1-{scorePrefix--}0"; //will be: "391-400"
                scoreDict.Add(key, specData[i] == "" ? 0 : int.Parse(specData[i]));
            }
            scoreDict.Add("000-120", specData[specData.Count - 1] == "" ? 0 : int.Parse(specData[specData.Count - 1]));
            
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
                { "Олимпиадники", int.Parse(specData[6]) },
                { "Распределение по баллам", scoreDict },
            };
            return info;
        }
    }
}
