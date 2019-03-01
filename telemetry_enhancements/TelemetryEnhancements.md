# Telemetry Enhancements


### Summary
Today, the majority of the Bot Framework Telemetry being logged is from sample code.  This was to provide the maximum flexibility of data being logged.  In the Bot Framework v4.4 timeframe, we are enhancing the product to log from the product.

## Overview of changes
There are three new components  added to the SDK.  All components will log to the *IBotTelemetryClient* interface which can be overridden.

- A  Bot Framework Middleware component (*TelemetryLoggerMiddleware*) that will log when messages are received, sent, updated or deleted. 
- *ITelemetryLuisRecognizer* interface and associated *TelemetryLuisRecognizer* class.
- *ITelemetryQnAMaker* interface and associated *TelemetryQnAMaker* class.

**Requirements**

|Name  | Description |
|:-----|:------------|
|Out-of-the-box logging | As a developer, I can use SDK Telemetry components without additional configuration to log events with a documented schema |
| Out of the box reports | As a developer, I can use the provided SDK Telemetry Reports and Dashboards that view logged events in Application Insights to see how my bot is performing. |
| Event properties can be extended | As a developer, I can add additional data to the existing Out-of-box event data to satisfy my companies reporting needs. |
| Event properties can be completely replaced | As a developer, I can completely replace the Out-of-bot event data to satisfy my companies' reporting needs. |
| Out of box reports can be invalidated | As a developer, I can change the properties being logged and break the out of box reports and dashboards |
| Interfaces must include data privacy settings | As a developer, I can change what data is logged in the event storage based on a flag, so I can adhere to GDPR and other standards for privacy |
| Log events according to the table in the appendix | See below |



The following issues also must be addressed:
https://github.com/Microsoft/AI/issues/762
https://github.com/Microsoft/AI/issues/840


## Telemetry Middleware
There following is an example `Startup.cs` where the Telemetry Logger Middleware is being created.  Once the middleware component is created, it can be added directly to the Middlware collection.  However, in the example below, the code is overriding the data that's being logged for the Middleware component to add custom properties.

There are additional overrides when a message is sent, deleted or updated which share the same signature.

```csharp
var telemetryClient = sp.GetService<IBotTelemetryClient>();
var appInsightsLogger = new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true);

// Override Receive Events
appInsightsLogger.OnReceiveEvent(async (ctx, logPersonalInformation) => { 
    var activity = ctx.Activity;
    Dictionary<string, string> properties = new Dictionary() 
    {
       { TelemetryConstants.FromIdProperty, activity.From.Id },
       { TelemetryConstants.ConversationNameProperty, activity.Conversation.Name },
       { TelemetryConstants.LocaleProperty, activity.Locale },
       { TelemetryConstants.RecipientIdProperty, activity.Recipient.Id },
       { TelemetryConstants.RecipientNameProperty, activity.Recipient.Name },
       { "MyImportantProperty", "ImportantValue" },
    };
    if (logPersonalInformation)
    {
       if (!string.IsNullOrWhiteSpace(activity.From.Name))
       {
          properties.Add(TelemetryConstants.FromNameProperty, activity.From.Name);
       }

       if (!string.IsNullOrWhiteSpace(activity.Text))
       {
          properties.Add(TelemetryConstants.TextProperty, activity.Text);
       }

       if (!string.IsNullOrWhiteSpace(activity.Speak))
       {
         properties.Add(TelemetryConstants.SpeakProperty, activity.Speak);
       }
    }
    return properties;
})
    
options.Middleware.Add(appInsightsLogger);
```



## LUIS Recognizer

```csharp
public interface ITelemetryLuisRecognizer : IRecognizer
{
    bool LogPersonalInformation { get; }

    Task<T> RecognizeAsync<T>(DialogContext dialogContext, Dictionary<string, string> properties = DefaultLuisProperties, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();

    new Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> properties = DefaultLuisProperties, CancellationToken cancellationToken = default(CancellationToken))
            where T : IRecognizerConvert, new();
}
```

## QnA Recognizer

```csharp
public interface ITelemetryQnAMaker
{
    bool LogPersonalInformation { get; }

    Task<QueryResult[]> GetAnswersAsync(ITurnContext context, Dictionary<string, string> properties = DefaultQnAProperties, QnAMakerOptions options = null);
}
```

