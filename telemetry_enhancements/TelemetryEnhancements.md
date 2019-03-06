# Telemetry Enhancements


### Summary
Today, the majority of the Bot Framework Telemetry being logged is from sample code.  This was to provide the maximum flexibility of data being logged.  In the Bot Framework v4.4 timeframe, we are enhancing the product to log from the product.

## Overview of changes
There are three new components  added to the SDK.  All components log using the *IBotTelemetryClient* interface which can be overridden with a custom implementation.

- A  Bot Framework Middleware component (*TelemetryLoggerMiddleware*) that will log when messages are received, sent, updated or deleted. User override for custom logging.
- *TelemetryLuisRecognizer* class.  User can override for custom logging.  
- *TelemetryQnAMaker*  class.  User can override for custom logging.

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
| Telemetry should be usable from LuisClient and Luis Recognizer | Customers using either the LuisClient or LuisRecognizer should be able to override pull telemetry |



Other Issues:
https://github.com/Microsoft/AI/issues/762 : We'll need to punt on this until v4.4 when storage is fixed.
https://github.com/Microsoft/AI/issues/840 : This can be fixed


## Telemetry Middleware
**Microsoft.Bot.Builder.TelemetryLoggerMiddleware**

### Usage
#### Out of box usage
The TelemetryLoggerMiddleware is a Bot Framework component that can be added without modification, and it will peform logging that enables out of the box reports that ship with the Bot Framework SDK. 
```csharp
var telemetryClient = sp.GetService<IBotTelemetryClient>();
var telemetryLogger = new TelemetryLoggerMiddleware(telemetryClient, logPersonalInformation: true);
options.Middleware.Add(telemetryLogger);  // Add to the middleware collection
```
#### Adding properties
If the developer decides to add additional properties, the TelemetryLoggerMiddleware class can be derived.  For example, if the developer would like to add the property "MyImportantProperty" to the `BotMessageReceived` event.  `BotMessageReceived` is logged when the user sends a message to the bot.  Adding the additional property can be accomplished in the following way:
```csharp
class MyTelemetryMiddleware : TelemetryLoggerMiddleware
{
    ...
    public Task OnReceiveActivityAsync(
                  Activity activity,
                  CancellationToken cancellation)
    {
        // Fill in the "standard" properties for BotMessageReceived
        // and add our own property.
        var properties = FillReceiveEventProperties(activity, 
                    new Dictionary<string, string>
                    { {"MyImportantProperty", "myImportantValue" } } );
                    
        // Use TelemetryClient to log event
        TelemetryClient.TrackEvent(
                        TelemetryLoggerConstants.BotMsgReceiveEvent,
                        properties);
    }
    ...
}
```
And in Startup, we would add the new class:
```csharp
var telemetryLogger = new TelemetryLuisRecognizer(telemetryClient, logPersonalInformation: true);
options.Middleware.Add(telemetryLogger);  // Add to the middleware collection
```
#### Completely replacing properties / Additional event(s)
If the developer decides to completely replace properties being logged, the `TelemetryLoggerMiddleware` class can be derived (like above when extending properties).   Similarly, logging new events is performed in the same way.

For example, if the developer would like to completely replace the`BotMessageSend` properties and send multiple events, the following demonstrates how this could be performed:

```csharp
class MyTelemetryMiddleware : TelemetryLoggerMiddleware
{
    ...
    public Task<RecognizerResult> OnLuisRecognizeAsync(
                  Activity activity,
                  string dialogId = null,
                  CancellationToken cancellation)
    {
        // Override properties for BotMsgSendEvent
        var botMsgSendProperties = new Dictionary<string, string>();
        properties.Add("MyImportantProperty", "myImportantValue");
        // Log event
        TelemetryClient.TrackEvent(
                        TelemetryLoggerConstants.BotMsgSendEvent,
                        botMsgSendProperties);
                        
        // Create second event.
        var secondEventProperties = new Dictionary<string, string>();
        secondEventProperties.Add("activityId",
                                   activity.Id);
        secondEventProperties.Add("MyImportantProperty",
                                   "myImportantValue");
        TelemetryClient.TrackEvent(
                        "MySecondEvent",
                        secondEventProperties);
    }
    ...
}
```
Note: When the standard properties are not logged, it will cause the out of box reports shipped with the product to stop working.

### Events Logged from Telemetry Middleware
[BotMessageSend](#botmessagesend)
[BotMessageReceived](#botmessagereceived)
[BotMessageUpdate](#botmessageupdate)
[BotMessageDelete](#botmessagedelete)

## Telemetry support LUIS 

C#: **Microsoft.Bot.Builder.AI.Luis.TelemetryLuisRecognizer **


### Usage
#### Out of box usage
The TelemetryLuisClientHandler is a Bot Framework component that can be added without modification, and it will peform logging that enables out of the box reports that ship with the Bot Framework SDK. 

```csharp
var client = new TelemetryLuisRecognizer(telemetryClient, luisApp, luisOptions, logPersonalInformation:true);
```
#### Adding properties
If the developer decides to add additional properties, the TelemetryLuisClientHandler class can be derived.  For example, if the developer would like to add the property "MyImportantProperty" to the `LuisResult` event.  `LuisResult` is logged when a LUIS prediction call is performed.  Adding the additional property can be accomplished in the following way:
```csharp
class MyLuisRecognizer : TelemetryLuisRecognizer 
{
   ...
   protected Task OnRecognizerResult(RecognizerResult result,
                  CancellationToken cancellation)
   {
       var luisEventProperties = FillLuisEventProperties(result, 
               new Dictionary<string, string>
               { {"MyImportantProperty", "myImportantValue" } } );
        
        TelemetryClient.TrackEvent(
                        LuisTelemetryConstants.LuisResultEvent,
                        luisEventProperties);
       }    
    ...
}
```

#### Completely replacing properties / Additional event(s)
If the developer decides to completely replace properties being logged, the `TelemetryLuisRecognizer` class can be derived (like above when extending properties).   Similarly, logging new events is performed in the same way.

For example, if the developer would like to completely replace the`BotMessageSend` properties and send multiple events, the following demonstrates how this could be performed:

```csharp
class MyLuisRecognizer : TelemetryLuisRecognizer
{
    ...
    public Task OnRecognizerResult(RecognizerResult result,
                  CancellationToken cancellation)
    {
        // Override properties for BotMsgSendEvent
        var luisProperties = new Dictionary<string, string>();
        properties.Add("MyImportantProperty", "myImportantValue");
        // Log event
        TelemetryClient.TrackEvent(
                        LuisTelemetryConstants.LuisResultEvent,
                        botMsgSendProperties);
        // Create second event.
        var secondEventProperties = new Dictionary<string, string>();
        secondEventProperties.Add("activityId",
                                   Activity.Id);
        secondEventProperties.Add("MyImportantProperty",
                                   "myImportantValue");
        TelemetryClient.TrackEvent(
                        "MySecondEvent",
                        secondEventProperties);
    }
    ...
}
```
Note: When the standard properties are not logged, it will cause the out of box reports shipped with the product to stop working.

### Events Logged from TelemetryLuisRecognizer
[LuisResult](#luisresult)

## Telemetry QnA Recognizer

C#: **Microsoft.Bot.Builder.AI.Luis.TelemetryQnAMaker **


### Usage
#### Out of box usage
The TelemetryQnAMaker is a Bot Framework component that can be added without modification, and it will peform logging that enables out of the box reports that ship with the Bot Framework SDK. 

```csharp
var client = new TelemetryQnAMaker(telemetryClient, endpoint, options, logPersonalInformation:true);
```
#### Adding properties
If the developer decides to add additional properties, the TelemetryQnAMaker class can be derived.  For example, if the developer would like to add the property "MyImportantProperty" to the `QnAMessage` event.  `QnAMessage` is logged when a QnA call is performed.  Adding the additional property can be accomplished in the following way:
```csharp
class MyQnAMaker : TelemetryQnAMaker 
{
   ...
   protected Task OnRecognizerResult(QueryResult result,
                  CancellationToken cancellation)
   {
       var qnaEventProperties = FillQnAEventProperties(result, 
               new Dictionary<string, string>
               { {"MyImportantProperty", "myImportantValue" } } );
        
        TelemetryClient.TrackEvent(
                        QnATelemetryConstants.QnAMessage,
                        qnaEventProperties);
       }    
    ...
}
```

#### Completely replacing properties / Additional event(s)
If the developer decides to completely replace properties being logged, the `TelemetryQnAMaker` class can be derived (like above when extending properties).   Similarly, logging new events is performed in the same way.

For example, if the developer would like to completely replace the`QnAMessage` properties and send multiple events, the following demonstrates how this could be performed:

```csharp
class MyLuisRecognizer : TelemetryQnAMaker
{
    ...
    public Task OnRecognizerResult(RecognizerResult result,
                  CancellationToken cancellation)
    {
        // Override properties for QnAMessage
        var qnaProperties = new Dictionary<string, string>();
        qnaProperties.Add("MyImportantProperty", "myImportantValue");
        // Log event
        TelemetryClient.TrackEvent(
                        QnATelemetryConstants.QnAMessage,
                        qnaProperties);
        // Create second event.
        var secondEventProperties = new Dictionary<string, string>();
        secondEventProperties.Add("activityId",
                                   Activity.Id);
        secondEventProperties.Add("MyImportantProperty",
                                   "myImportantValue");
        TelemetryClient.TrackEvent(
                        "MySecondEvent",
                        secondEventProperties);
    }
    ...
}
```
Note: When the standard properties are not logged, it will cause the out of box reports shipped with the product to stop working.

### Events Logged from TelemetryLuisRecognizer
[QnAMessage](#qnamessage)



# Appendix A : Middleware Events
## BotMessageReceived 
Logged when bot receives new message from a user.

When not overridden, this event is logged from `Microsoft.Bot.Builder.TelemetryLoggerMiddleware` using the `Microsoft.Bot.Builder.IBotTelemetry.TrackEvent()` method.

- Session Identifier  
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer`  as the  **session** identifier (*Temelemtry.Context.Session.Id*) used within Application Insights.  
  - Corresponds to the [Conversation ID](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#conversation) as defined by Bot Framework protocol..
  - The property name logged is `session_id`.

- User Identifier
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer`  as the  **user**  identifier (*Telemetry.Context.User.Id*) used within Application Insights.  
  - The value of this property is a combination of the [Channel Identifier](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#channel-id) and the [User ID](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) (concatenated together) properties as defined by the Bot Framework protocol.
  - The property name logged is `user_id`.
- ActivityID 
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.
  - Corresponds to the [Activity ID](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#Id) as defined by Bot Framework protocol..
  - The property name is `activityId`.
- Channel Identifier
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.  
  - Corresponds to the [Channel Identifier](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#id) of the Bot Framework protocol.
  - The property name logged is `channelId`.
- ActivityType 
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer` as a Property to the event.  
  - Corresponds to the [Activity Type](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#type) of the Bot Framework protocol.
  - The property name logged is `activityType`.
- Text
  - Optionally logged when the `logPersonalInformation` property is set to `true`.
  - Corresponds to the [Activity Text](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#text) field of the Bot Framework protocol.
  - The property name logged is `text`.
- FromId
  - Corresponds to the [From Identifier](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromId`.
- FromName
  - Optionally logged when the `logPersonalInformation` property is set to `true`.
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.
- RecipientId
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.
- RecipientName
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.
- ConversationId
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.
- ConversationName
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.
- Locale
  - Corresponds to the [From Name](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromName`.

## BotMessageSend 
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


## BotMessageUpdate
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is updated by the bot (rare case)

## BotMessageDelete
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is deleted by the bot (rare case)

# Appendix B: LUIS Events

## CustomEvent: LuisIntent.INENTName 
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

# Appendix C: QnA Events

## CustomEvent: QnAMessage
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
