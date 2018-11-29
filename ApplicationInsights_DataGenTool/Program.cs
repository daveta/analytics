using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateFromConfiguration(File.ReadAllText("ApplicationInsights.config"));
            var telemetryClient = new TelemetryClient(configuration);
            // run app...
            SimulateUsage(telemetryClient, 500);

            // before exit, flush the remaining data
            telemetryClient.Flush();

            // flush is not blocking so wait a bit
            Task.Delay(5000).Wait();

        }

        static void SimulateUsage(TelemetryClient telemetryClient, int users)
        {
            if (users < 100)
            {
                throw new Exception("Must have at least 100 users");
            }

            DialogStats[] dialogs =
            {
                new DialogStats("Greeting", new StepStats[] { new StepStats("Name", users/10, users/10-1),
                                                              new StepStats("Location", users) }),
                new DialogStats("BookTable", new StepStats[] { new StepStats("Guests", users/100),
                                                              new StepStats("Restaurant", users/10),
                                                              new StepStats("Time", users)})

            };

            for (int i=0; i<users; i++)
            {
                telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
                telemetryClient.Context.User.Id = Guid.NewGuid().ToString();

                
                var traceString = $"UserId:{telemetryClient.Context.Session.Id}:SessionId:{telemetryClient.Context.User.Id}";
                foreach (var dialog in dialogs)
                {
                    var properties = CreateProperties();
                    var dialogInstanceId = Guid.NewGuid().ToString();
                    properties.Add("DialogId", dialog.Name);
                    properties.Add("InstanceId", dialogInstanceId);
                    traceString += $":DialogId:{properties["DialogId"]}:";

                    for (int stepIndex=0; stepIndex < dialog.Steps.Length; stepIndex++)
                    {
                        var step = dialog.Steps[stepIndex];

                        if (step.CurCount < step.MaxCount)
                        {
                            if (stepIndex == 0)
                            {
                                // Emit Start event
                                properties.Remove("StepName");
                                Console.WriteLine(traceString + "WaterfallStart");
                                telemetryClient.TrackEvent("WaterfallStart", properties);
                                continue;

                            }


                            if (step.CurCancels < step.Cancels)
                            {
                                // Emit Cancel
                                step.CurCancels++;
                                properties.Remove("StepName");
                                properties.Add("StepName", step.Name);
                                Console.WriteLine(traceString + "WaterfallCancel");
                                telemetryClient.TrackEvent("WaterfallCancel", properties);
                                continue;
                            }
                            if (step.CurCount < step.MaxCount)
                            {
                                step.CurCount++;
                                properties.Remove("StepName");
                                properties.Add("StepName", step.Name);
                                // Emit WaterfallStep
                                Console.WriteLine(traceString + $"WaterfallStep({step.Name})");
                                telemetryClient.TrackEvent("WaterfallStep", properties);
                            }
                            else
                            {
                                continue;
                            }

                            // Emit WaterfallComplete
                            if (stepIndex >= dialog.Steps.Length - 1)
                            {
                                properties.Remove("StepName");
                                Console.WriteLine(traceString + $"WaterfallComplete");
                                telemetryClient.TrackEvent("WaterfallComplete", properties);
                            }
                        }
                    }

                }
            }
        }

        static Dictionary<string, string> CreateProperties()
        {
            var activityId = Guid.NewGuid().ToString();
            var channelId = "TESTCHANNEL";
            var activityType = "message";
            return new Dictionary<string, string>()
                    {
                        { "activityId", activityId },
                        { "channelId", channelId },
                        { "activityType", activityType },
                    };
        }
    }
    public class StepStats
    {
        public StepStats(string name, int maxCount, int cancels=0)
        {
            Name = name;
            MaxCount = maxCount;
            Cancels = cancels;
        }
        public string Name { get; set; }
        public int MaxCount { get; set; }
        public int Cancels { get; set; }
        public int CurCount { get; set; } = 0;
        public int CurCancels { get; set; } = 0;
    }
    public class DialogStats
    {
        public DialogStats(string name, StepStats[] steps)
        {
            Name = name;
            Steps = steps;
        }
        public string Name { get; set; }
        public StepStats[] Steps { get; set; }
    }
}