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
        static OperationIdInitializer _operationIdInitializer = new OperationIdInitializer();

        static void Main(string[] args)
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateFromConfiguration(File.ReadAllText("ApplicationInsights.config"));
            configuration.TelemetryInitializers.Add(_operationIdInitializer);
            var telemetryClient = new TelemetryClient(configuration);

            // 

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
                
                telemetryClient.Context.User.Id = Guid.NewGuid().ToString();
                

                
                
                var traceString = $"UserId:{telemetryClient.Context.Session.Id}:SessionId:{telemetryClient.Context.User.Id}";
                foreach (var dialog in dialogs)
                {

                    var properties = CreateProperties();

                    _operationIdInitializer.OperationID = Guid.NewGuid().ToString();
                    telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

                    var dialogInstanceId = Guid.NewGuid().ToString();
                    
                    properties.Add("DialogId", dialog.Name);
                    properties.Add("InstanceId", dialogInstanceId);
                    traceString += $":DialogId:{properties["DialogId"]}:";

                    
                    LogLuisIntent(telemetryClient, properties);

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

        static void LogLuisIntent(TelemetryClient telemetryClient, Dictionary<string, string> properties)
        {
            var props = new Dictionary<string, string>(properties);
            LUISIntentStats[] intents =
            {
                new LUISIntentStats("GreetingIntent", 5),
                new LUISIntentStats("ChitChatIntent", 5),
                new LUISIntentStats("QuestionIntent", 5)
            };

            bool found = false;
            foreach (var intent in intents)
            {
                if (intent.CurCount < intent.MaxCount)
                {
                    props.Add("Question", $"{intent.Name} question");
                    props.Add("Intent", intent.Name);
                    props.Add("IntentScore", ".45");
                    telemetryClient.TrackEvent($"LuisIntent.{intent.Name}", props);
                    found = true;
                    break;
                }
            }

            // No slots open, take first one.
            if (!found)
            {
                var intent2 = intents[0];
                props.Add("Question", $"{intent2.Name} question");
                props.Add("Intent", intent2.Name);
                props.Add("IntentScore", ".48");
                telemetryClient.TrackEvent($"LuisIntent.{intent2.Name}", props);
            }
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

    public class LUISIntentStats
    {
        public LUISIntentStats(string name, int maxCount)
        {
                Name = name;
                MaxCount = maxCount;
        }

        public string Name { get; set; }
        public int MaxCount { get; set; }
        public int CurCount { get; set; } = 0;
    }
}
