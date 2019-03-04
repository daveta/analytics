# Telemetry Enhancements


### Summary
Today, the majority of the Bot Framework Telemetry being logged is from sample code.  This was to provide the maximum flexibility of data being logged.  In the Bot Framework v4.4 timeframe, we are enhancing the product to log from the product.

## Overview of changes
There are three new components  added to the SDK.  All components log using the *IBotTelemetryClient* interface which can be overridden with a custom implementation.

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
| A default implementation should log events to Application Insights as "CustomEvent" | See event definitions below - search for "CustomEvent" |



The following issues also must be addressed:
https://github.com/Microsoft/AI/issues/762
https://github.com/Microsoft/AI/issues/840


## Telemetry Middleware
**Microsoft.Bot.Builder.TelemetryLoggerMiddleware**

There following is an example `Startup.cs` where the Telemetry Logger Middleware is being created.  Once the middleware component is created, it can be added directly to the Middlware collection.  However, in the example below, the code is overriding the data that's being logged for the Middleware component to add custom properties.

There are additional overrides when a message is sent, deleted or updated which share the same signature.

```csharp
var telemetryClient = sp.GetService<IBotTelemetryClient>();
var appInsightsLogger = new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true);

// Override Receive Event to add "MyImportantProperty"
appInsightsLogger.OnReceiveEvent(async (ctx, logPersonalInformation) => { 
    var activity = ctx.Activity;
    Dictionary<string, string> properties = new Dictionary() 
    {
       { "MyImportantProperty", "ImportantValue" },
    };
    
    TelemetryLoggerMiddleware.FillStandardReceiveProperties(activity, logPersonalInformation);
    
    return properties;
})

options.Middleware.Add(appInsightsLogger);
```
### CustomEvent: BotMessageReceived 
Logged when bot receives new message.

This event is logged from `Microsoft.Bot.Builder.TelemetryLoggerMiddleware` using the `Microsoft.Bot.Builder.IBotTelemetry.TrackEvent()` method.

- UserID  
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer`  as the  **user** identifier (*Temelemtry.Context.User.Id*) used within Application Insights.  
  - A combination of the [Channel Identifier](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#id) and the [User ID](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#id) (concatenated together) of the Bot Protocol.
- ConversationID
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer`  as the  **session**  identifier (*Telemetry.Context.Session.Id*) used within Application Insights.  
  - Corresponds to the [Conversation ID](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#conversation) of the Bot Protocol.
- ActivityID 
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.  
  - Corresponds to the [Activity ID](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#Id) of the Bot Protocol.
- Channel 
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.  
  - Corresponds to the [Channel Identifier](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#id) of the Bot Protocol.
- ActivityType 
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.  
  - Corresponds to the [Activity Type](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#type) of the Bot Protocol.
- Text
  - Optionally logged when the `logPersonalInformation` property is set to `true`.
  - Corresponds to the [Activity Text](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#text) field of the Bot Protocol.
- FromId
- FromName
- RecipientId
- RecipientName
- ConversationId
- ConversationName
- Locale

### CustomEvent: BotMessageSend 
**Logged From:** TelemetryLoggerMiddleware (**Enterprise Sample**)

Logged when bot sends a message.

- UserID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ConversationID ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- Channel  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityType  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ReplyToID
- Channel  (Source channel - e.g. Skype, Cortana, Teams)
- RecipientId
- ConversationName
- Locale
- Text (Optional for PII)
- RecipientName (Optional for PII)


### CustomEvent: BotMessageUpdate
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is updated by the bot (rare case)

### CustomEvent: BotMessageDelete~~
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is deleted by the bot (rare case)



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





### CustomEvent: LuisIntent.INENTName 
**Logged From:** TelemetryLuisRecognizer (**Enterprise Sample**)

Logs results from LUIS service.

- UserID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ConversationID ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- Channel  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityType  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- Intent
- IntentScore
- Question
- ConversationId
- SentimentLabel
- SentimentScore
- *LUIS entities*
- **NEW** DialogId


### CustomEvent: QnAMessage
**Logged From:** TelemetryQnaMaker (**Enterprise Sample**)

Logs results from QnA Maker service.

- UserID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ConversationID ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityID  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- Channel  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- ActivityType  ([From Telemetry Initializer](#identifiers-added-to-custom-events))
- Username
- ConversationId
- OriginalQuestion
- Question
- Answer
- Score (*Optional*: if we found knowledge)

