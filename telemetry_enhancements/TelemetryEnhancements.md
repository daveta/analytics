# Telemetry Enhancements


### Summary
Today, the majority of the Bot Framework Telemetry being logged is from sample code.  This was to provide the maximum flexibility of data being logged.  In the Bot Framework v4.4 timeframe, we are enhancing the product to log from the product.

## Overview of changes
There are three new components  added to the SDK.  All components log using the *IBotTelemetryClient*  (or BotTelemetryClient in node.js) interface which can be overridden with a custom implementation.

- A  Bot Framework Middleware component (*TelemetryLoggerMiddleware*) that will log when messages are received, sent, updated or deleted. User override for custom logging.
- *LuisRecognizer* class.  User can override for custom logging in two ways - per invocation (add/replace properties) or derived classes.
- *QnAMaker*  class.  User can override for custom logging in two ways - per invocation (add/replace properties) or derived classes.

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
C#: **Microsoft.Bot.Builder.TelemetryLoggerMiddleware**

Typescript/js: **botbuilder-core**

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

C#: **Microsoft.Bot.Builder.AI.Luis.LuisRecognizer **

TypeScript/JS: **botbuilder-ai**




### Usage
#### Out of box usage
The LuisRecognizer is an existing Bot Framework component, and telemetry can be enabled by passing a IBotTelemetryClient interface.  Without modification, and it will peform logging that enables out of the box reports (for Application Insights) that (will) ship with the Bot Framework SDK.  You can override the default properties being logged and log new events as required.

During construction, an IBotTelemetryClient object must be passed for this to work.

```csharp
var client = new LuisRecognizer(luisApp, luisOptions, ... telemetryClient);
```
#### Adding properties
If the developer decides to add additional properties, the `LuisRecognizer` class can be derived.  For example, if the developer would like to add the property "MyImportantProperty" to the `LuisResult` event.  `LuisResult` is logged when a LUIS prediction call is performed.  Adding the additional property can be accomplished in the following way:
```csharp
class MyLuisRecognizer : LuisRecognizer 
{
   ...
   override protected Task OnRecognizerResultAsync(
           RecognizerResult recognizerResult,
           ITurnContext turnContext,
           Dictionary<string, string> properties = null,
           CancellationToken cancellationToken = default(CancellationToken))
   {
       var luisEventProperties = FillLuisEventProperties(result, 
               new Dictionary<string, string>
               { {"MyImportantProperty", "myImportantValue" } } );
        
        TelemetryClient.TrackEvent(
                        LuisTelemetryConstants.LuisResultEvent,
                        luisEventProperties);
        ..
   }    
   ...
}
```

#### Completely replacing properties / Additional event(s)
If the developer decides to completely replace properties being logged, the `LuisRecognizer` class can be derived (like above when extending properties).   Similarly, logging new events is performed in the same way.

For example, if the developer would like to completely replace the`LuisResult` properties and send multiple events, the following demonstrates how this could be performed:

```csharp
class MyLuisRecognizer : LuisRecognizer
{
    ...
    override protected Task OnRecognizerResultAsync(
             RecognizerResult recognizerResult,
             ITurnContext turnContext,
             Dictionary<string, string> properties = null,
             CancellationToken cancellationToken = default(CancellationToken))
    {
        // Override properties for LuisResult event
        var luisProperties = new Dictionary<string, string>();
        properties.Add("MyImportantProperty", "myImportantValue");
        
        // Log event
        TelemetryClient.TrackEvent(
                        LuisTelemetryConstants.LuisResult,
                        luisProperties);
                        
        // Create second event.
        var secondEventProperties = new Dictionary<string, string>();
        secondEventProperties.Add("MyImportantProperty2",
                                   "myImportantValue2");
        TelemetryClient.TrackEvent(
                        "MySecondEvent",
                        secondEventProperties);
        ...
    }
    ...
}
```
Note: When the standard properties are not logged, it will cause the Application Insights out of box reports shipped with the product to stop working.

### Add properties per invocation
Sometimes it's necessary to add additional properties during the invocation:
```csharp
var additionalProperties = new Dictionary<string, string>
{
   { "dialogId", "myDialogId" },
   { "foo", "foovalue" },
};

var result = await recognizer.RecognizeAsync(turnContext,
     additionalProperties,
     CancellationToken.None).ConfigureAwait(false);
```

### Events Logged from TelemetryLuisRecognizer
[LuisResult](#luisresult)




## Telemetry QnA Recognizer

C#: **Microsoft.Bot.Builder.AI.Luis.QnAMaker **

Typescript/js: **botbuilder-ai**


### Usage
#### Out of box usage
The QnAMaker class is an existing Bot Framework component that adds two additional constructor parameters which enable logging that enable out of the box reports that ship with the Bot Framework SDK. The new `telemetryClient` references a `IBotTelemetryClient` interface which performs the logging.  

```csharp
var qna = new QnAMaker(endpoint, options, client, 
                       telemetryClient: telemetryClient,
                       logPersonalInformation: true);
```
#### Adding properties 
If the developer decides to add additional properties, there are two methods of doing this - when properties need to be added during the QnA call to retrieve answers or deriving from the `QnAMaker` class.  

The following demonstrates deriving from the `QnAMaker` class.  The example shows adding the property "MyImportantProperty" to the `QnAMessage` event.  The`QnAMessage` event is logged when a QnA `GetAnswers`call is performed.  In addition, we log a second event "MySecondEvent".

```csharp
class MyQnAMaker : QnAMaker 
{
   ...
   protected override Task OnQnaResultsAsync(
                 QueryResult[] queryResults, 
                 ITurnContext turnContext, 
                 Dictionary<string, string> telemetryProperties = null, 
                 Dictionary<string, double> telemetryMetrics = null, 
                 CancellationToken cancellationToken = default(CancellationToken))
   {
            var eventData = await FillQnAEventAsync(queryResults, turnContext, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

            // Add my property
            eventData.Properties.Add("MyImportantProperty", "myImportantValue");

            // Log QnaMessage event
            TelemetryClient.TrackEvent(
                            QnATelemetryConstants.QnaMsgEvent,
                            eventData.Properties,
                            eventData.Metrics
                            );

            // Create second event.
            var secondEventProperties = new Dictionary<string, string>();
            secondEventProperties.Add("MyImportantProperty2",
                                       "myImportantValue2");
            TelemetryClient.TrackEvent(
                            "MySecondEvent",
                            secondEventProperties);       }    
    ...
}
```

#### Completely replacing properties / Additional event(s)
If the developer decides to completely replace properties being logged, the `TelemetryQnAMaker` class can be derived (like above when extending properties).   Similarly, logging new events is performed in the same way.

For example, if the developer would like to completely replace the`QnAMessage` properties, the following demonstrates how this could be performed:

```csharp
class MyLuisRecognizer : TelemetryQnAMaker
{
    ...
    protected override Task OnQnaResultsAsync(
         QueryResult[] queryResults, 
         ITurnContext turnContext, 
         Dictionary<string, string> telemetryProperties = null, 
         Dictionary<string, double> telemetryMetrics = null, 
         CancellationToken cancellationToken = default(CancellationToken))
    {
        // Add properties from GetAnswersAsync
        var properties = telemetryProperties ?? new Dictionary<string, string>();
        // GetAnswerAsync properties overrides - don't add if already present.
        properties.TryAdd("MyImportantProperty", "myImportantValue");

        // Log event
        TelemetryClient.TrackEvent(
                           QnATelemetryConstants.QnaMsgEvent,
                            properties);
    }
    ...
}
```
Note: When the standard properties are not logged, it will cause the out of box reports shipped with the product to stop working.

#### Adding properties during GetAnswersAsync
If the developer has properties that need to be added during runtime, the `GetAnswersAsync` method can provide properties and/or metrics to add to the event.

For example, if the developer wants to add a `dialogId` to the event, it can do so like the following:
```csharp
var telemetryProperties = new Dictionary<string, string>
{
   { "dialogId", myDialogId },
};

var results = await qna.GetAnswersAsync(context, opts, telemetryProperties);
```
The `QnaMaker` class provides the capability of overriding properties, including PersonalInfomation properties.

### Events Logged from TelemetryLuisRecognizer
[QnAMessage](#qnamessage)



# Appendix A : Middleware Events
## BotMessageReceived 
Logged when bot receives new message from a user.

When not overridden, this event is logged from `Microsoft.Bot.Builder.TelemetryLoggerMiddleware` using the `Microsoft.Bot.Builder.IBotTelemetry.TrackEvent()` method.

- Session Identifier  
  - When using Application Insights, this is logged from the `TelemetryBotIdInitializer`  as the  **session** identifier (*Temeletry.Context.Session.Id*) used within Application Insights.  
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
  - **Optionally** logged when the `logPersonalInformation` property is set to `true`.
  - Corresponds to the [Activity Text](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#text) field of the Bot Framework protocol.
  - The property name logged is `text`.

- Speak

  - **Optionally** logged when the `logPersonalInformation` property is set to `true`.
  - Corresponds to the [Activity Speak](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#speak) field of the Bot Framework protocol.
  - The property name logged is `speak`.

  - 

- FromId
  - Corresponds to the [From Identifier](https://github.com/Microsoft/botframework-obi/blob/f4e9e2f75c144cfd22a9f438e5b5b139fe618aad/protocols/botframework-activity/botframework-activity.md#from) field of the Bot Framework protocol.
  - The property name logged is `fromId`.

- FromName
  - **Optionally** logged when the `logPersonalInformation` property is set to `true`.
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
**Logged From:** TelemetryLoggerMiddleware 

Logged when bot sends a message.

- UserID   (From Telemetry Initializer)
- SessionID  (From Telemetry Initializer)
- ActivityID   (From Telemetry Initializer)
- Channel   (From Telemetry Initializer)
- ActivityType   (From Telemetry Initializer)
- ReplyToID
- RecipientId
- ConversationName
- Locale
- RecipientName (Optional for PII)
- Text (Optional for PII)
- Speak (Optional for PII)


## BotMessageUpdate
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is updated by the bot (rare case)
- UserID   (From Telemetry Initializer)
- SessionID  (From Telemetry Initializer)
- ActivityID  (From Telemetry Initializer)
- Channel   (From Telemetry Initializer)
- ActivityType   (From Telemetry Initializer)
- RecipientId
- ConversationId
- ConversationName
- Locale
- Text (Optional for PII)


## BotMessageDelete
**Logged From:** TelemetryLoggerMiddleware
Logged when a message is deleted by the bot (rare case)
- UserID   (From Telemetry Initializer)
- SessionID  (From Telemetry Initializer)
- ActivityID   (From Telemetry Initializer)
- Channel   (From Telemetry Initializer)
- ActivityType   (From Telemetry Initializer)
- RecipientId
- ConversationId
- ConversationName

# Appendix B: LUIS Events

## CustomEvent: LuisEvent
**Logged From:** LuisRecognizer

Logs results from LUIS service.

- UserID   (From Telemetry Initializer)
- SessionID  (From Telemetry Initializer)
- ActivityID  (From Telemetry Initializer)
- Channel  (From Telemetry Initializer)
- ActivityType  (From Telemetry Initializer)
- ApplicationId
- Intent
- IntentScore
- Intent2 
- IntentScore2 
- FromId
- SentimentLabel
- SentimentScore
- Entities (as json)
- Question (Optional for PII)

# Appendix C: QnA Events

## CustomEvent: QnAMessage
**Logged From:** QnaMaker

Logs results from QnA Maker service.

- UserID  (From Telemetry Initializer)
- SessionID  (From Telemetry Initializer)
- ActivityID  (From Telemetry Initializer)
- Channel  (From Telemetry Initializer)
- ActivityType   (From Telemetry Initializer)
- Username (Optional for PII)
- Question (Optional for PII)
- MatchedQuestion
- QuestionId
- Answer
- Score
- ArticleFound
