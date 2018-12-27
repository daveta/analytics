using System;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Configuration;

namespace makeAppInsightsUrl
{
    class Program
    {
        /// <summary>
        /// Demonstrates how to make a Application Insights portal query pre-populate.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            BotConfiguration botConfig = BotConfiguration.Load("sample.bot");
            ExportQueryHelper queryHelper = new ExportQueryHelper(botConfig);
            var query = "This is a new query!";

            Console.WriteLine(queryHelper.BuildNavigationUrl(query));
            Console.WriteLine("Done!");

        }
    }
}
