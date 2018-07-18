# Analytics Data Sources and Schema

## Summary
A typical Azure Bot Service employs several components that work together to create a production bot.  The primary goal is to enable tracing a message flow throughout the system through correlated identifiers.  This data is optionally collected and intended for customer-use only.  The internal Microsoft teams will not have access to the data collected.

This is primarily capturing events that occur on the wire.  There are also events that occur within each service as processing and custom events the bot developer will want to correlate, for example Middleware or dialog processing.

![Summary of data sources](https://raw.githubusercontent.com/daveta/analytics/master/AnalyticsDataSources.png)

Also see "Analytics and Logging" for further background.

## Background
Today the Bot developer can opt into purchasing Application Insights for their subscription. We are adopting the Application Insights infrastructure for Bot Analytics.    Adopting Application Insights means adopting their data model. 

```https://docs.microsoft.com/en-us/azure/application-insights/application-insights-data-model```

Application Insights has many reports (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-usage-overview) that can be used if  tables are populated  appropriately. In addition, we inherit the extensibility model (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-api-custom-events-metrics) that Application Insights has in terms of custom events that can be logged and leveraged.  Not to mention the other tooling.

### What is a Turn?
A "Turn" is a concept that represents a complete round trip within a Bot - from the original request to potentially multiple responses.  The data backing a turn begins when a message is received by a Bot.  You can link events in the turn via several identifiers.  

### Identifiers
There are several correlating concepts identifiers that are being employed at two levels that tie the events together.

**Bot Level**
*ActivityID (aka MessageID)*  - Created in the Channel Service, this represents a unique message id to/from the channel service.
*ReplyID* - When a Bot responds to a message, the ReplyID is the ActivityID of the original message.

*ConversationID* - Channel-specific representation of a conversation. 

**Application Insights Level** 
*CorrelationID (aka TraceID)* - This identifier represents a single instance of processing a message.  Or conceptually, it a container that holds a graph of dependency calls (see DependencyID).  There are events that contain this identifier begining from the channel receiving a message, sending the message to the Bot Service, the Bot Service invocations of Middleware/LUIS/QNA, and the Bot Service N-reponses back to the customer.  
*DependencyID* - This identifier represent a single call from one component to another.  The component could be in-process or out of process.  The DependencyID is logged both in the caller and callee.

### CustomEvent: BotMessageReceived

Currently the TranscriptLoggerMiddleware logs entire Activity objects to storage, including when new Activity objects are received.  We will leverage the existing infrastructure to log the BotMessageReceived object into Application Insights.  

- Channel  (Source channel - e.g. Skype, Cortana, Teams)
- Text (Contents of the message - may not be populated in extreme circumstances where the customer has asked us not to)
- FromId
- FromName
- ConversationId
- ConversationName
- Sentiment (optional but should be there in most cases)
- Locale
- Language
- ClientInfo (most channels will provide this and contents will vary). Skype consumer only has locale, webchat has none, etc.
	○ Here is an example from Teams
	{
	  "locale": "en-GB",
	  "country": "GB",
	  "platform": "Windows"
	}
- ActivityID
- CorrelationID


Intent.INTENTName
### CustomEvent: Luis Intent

The LUIS Recognizer will use the existing infrastructure to log Trace Activity events.  Currently, Trace Activity events are logged by the TranscriptLoggerMiddleware and consumed by the Emulator.  This infrastructure will be leveraged to log into Application Insights.

**Name of Event**: Intent.INTENTName

Every LUIS Intent hit will result in an Application Insights event being created. This event is called: Intent.INTENTNAME. It will have the following custom dimensions:

- Score
- Question
- ConversationId (for correlation purposes)
- ActivityID
- CorrelationID



### Unknown Question
If a question goes to a Knowledge Source (QnA Maker) an event will be raised to track this including information to help you understand if we found something for the user or if we didn't. 

The QNAMaker Recognizer will fire Trace Activities that will feed the necessary data.

**Name of Event**: UnknownQuestion

If we found knowledge the following custom dimensions will be added

- Question
- FoundInKnowledgeSource - set to true
- UserAcceptedAnswer (if the user provided feedback and the developer asked the user) set to true or false based on feedback
- ActivityID
- CorrelationID

If we did *not* find knowledge for the user the following custom dimensions will be added:

- Question
- FoundInKnowledgeSource - set to false
- KnowledgeItemsDiscarded - if we find items but discard them because the score is too low we add these to help with diagnosis. RecordID and Title will be provided
- Will be provided in the format of ID=Title,ID=Title,ID=Title

### Dependency
Understanding flow between components is a primary scenario that our customers are interested in. Application Insights has a concept of a dependency that allows you to track a single operation that is serviced by multiple components.  

Note: Just an example, Channel most likely will not be included.

![Application Insights Sample App map](https://raw.githubusercontent.com/daveta/analytics/master/appmap_bot.PNG)

Above, each green circle is logged as an Application Insight Request object, and the smaller bubbles are logged as a Dependency object.

We will assume the same Application Insights Instrumentation Key.

Each Dependency is logged by the closest component as a Trace Activity.  It will log only a few fields of the DepedencyTelemetry object (examples are below). 

The TranscriptLogger will perform the actual logging into Application Insights.  It will automatically populate the following properties so the developer won't need to:

- Calculate durations
- Keep track/stamp each dependency with appropriate CorrelationID (OperationID) and previous component (ParentID)
- Keep a single instance of the TelemetryClient in memory.



| Source | Destination | Component logged |
| :---         |     :---:      |          :--- |
| Channel Service (pri3) | Bot   | Channel Service (Direct log into AppInsights) |
| Bot     | LUIS  | LuisRecognizer (Fire TraceActivity) |
| Bot     | QNA  | QnaRecognizer (Fire TraceActivity) |
| Bot     | Middleware Component  | Middleware Component (Fire TraceActivity) |
| Bot     | Storage Component  | Storage Component (Fire TraceActivity) |
| Bot     | Channel Service (pri3)  | Transcript Logger (Fire TraceActivity) |



The following example illustrates how we can use this infrastructure.

#### Channel Service Telemetry (Pri 3)
When a customer types a message into a bot, the first stop is the Channel Service.  The service will log the Application Insights standard Request telemetry.  During this stop, the request receives an "Operation ID" (or TRACE_ID).  We will refer to this as the "Correlation ID". 

From the Channel Service, this is a GUID that is populated in their request:
```csharp
var r = new RequestTelemetry(
    name: "Facebook Receive",
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    responseCode: "200",
    success: true)
{
    Source = "" //no source specified
};
r.Context.Operation.Id = TRACE_ID; // initiate the logical operation ID (trace id or CORRELATION ID)
r.Context.Operation.ParentId = null; // this is the first span in a trace
r.Context.Cloud.RoleName = "Facebook Channel"; // this is the name of the node on app map
// Fire Request directly into Application Insights
```
**NEW WORK FOR CHANNEL SERVICE**

**Incoming Request** 

In this scenario, messages coming in from the messaging service with the Bot as the destination, the Channel Service needs to send an additional Application Insights identifier.

- Dependency ID : In addition to the "Correlation ID" a "Dependency ID" is required to fully light up Application Insights.  

Today a HTTP header is sent from the Channel Service to the bot passing the correlation ID.  In addition, the Dependency ID will also need to be passes.

**Outgoing Request**

In this scenario, messages originating from the Bot, going outbound to external messaging services need to handle new HTTP headers originating from the Bot.  

- Correlation ID: 
- Dependency ID

The Application Insights team have suggested header names.  We can continue with our existing correlation header, and possibly adopt their header.

```json
Future: Adopt W3C Standards from Sergey (which is different than what App Insights is proposing now)
https://w3c.github.io/distributed-tracing/report-trace-context.html
```




#### Request: Customer Bot Receive/Send Telemetry (Pri 1)
The bot receives the Correlation and Dependency ID's and then logs it's own Application Insights Request message.  This will be peformed automatically in the SDK in the connector.
Request telemetry will be logged with the same Correlation ID.

```csharp
traceinfo = new RequestTelemetry(
    name: "POST /api/messages",
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    responseCode: "200",
    success: true)
var traceActivity = Schema.Activity.CreateTraceActivity("Request", RequestTraceType, traceinfo, RequestTraceLabel);
await context.SendActivityAsync(traceActivity).ConfigureAwait(false);```
```
At this point, we now are correlated with the original request coming from the Channel Service.

#### Dependency: Track LUIS/QnA invocation  (Pri 1)
During processing of the Bot request, other Cognitive Services are employed.  Within each client Recognizer component, it will log Dependency telemetry to reflect the call.  This is in addition to the custom events that are also described in this document.

```csharp
traceinfo = new DependencyTelemetry(
    target: $"api.cognitive.microsoft.com",
    dependencyName: "GET /luis",
    data: "https://api.cognitive.microsoft.com/luis/v2.0/apps?q=turn%20on%20the%20bedroom%20light",  // Command which initiated call
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
var traceActivity = Schema.Activity.CreateTraceActivity("LUIS", LUISTraceType, traceinfo, LUISTraceLabel);
await context.SendActivityAsync(traceActivity).ConfigureAwait(false);```
```


#### Dependency: Track Middleware (Pri 2)
Middleware components will track their *own* dependency.  This includes tracking the duration of the time.



For example, here's what the AI.Translate logs:
```csharp

... Perform all your Middleware processing ..

// Note: The duration is inferred, so logging the dependency
// needs to be at the end so the duration is accurate.
traceinfo = new DependencyTelemetry(
    target: $"ai.translation",
    dependencyName: "Translate Text",
    data: "<Text to convert>",  // Command which initiated call
    success: true);
var traceActivity = Schema.Activity.CreateTraceActivity("MyMiddleware", TranslateTraceType, traceinfo, TranslateTraceLabel);
await context.SendActivityAsync(traceActivity).ConfigureAwait(false);```
```

#### Dependency: Track Storage Dependency
Any storage operations should be modeled as a dependency.  For example, when we perform blob read operations:
```csharp

traceinfo = new DependencyTelemetry(
    target: $"Azure Blob",
    dependencyName: "Read",
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
var traceActivity = Schema.Activity.CreateTraceActivity("AzureBlob", BlobTraceType, traceinfo, BlobTraceLabel);
await context.SendActivityAsync(traceActivity).ConfigureAwait(false);```
```

### Funnel (Pri2)
Funnels are a series of Application Insight custom events.  These events serve as milestones in the funnel.

![Application Insights Sample Funnel](https://raw.githubusercontent.com/daveta/analytics/master/funnel.PNG)

Logging events in Application Insights is pretty simple:

```csharp
telemetry.TrackEvent("MyEvent");
```
In the context, user/session identifiers should also be set.

**NEW WORK FOR SDK:**
Funnels work well for dialogs and prompts that have a clear progression.

- Prompt Identifier

If we plumb this at the Prompt level, we need a compact representation of an Event ID/Name. Normally this is an enum, but it needs to be meaningful to the customer.  Therefore a new property should be added to the prompt that will log the appropriate event.



### User Flow (Pri 4)
Userflow can give a good idea of where the common paths customers are taking.  This again can derive from events.  The proposal is Prompts are the primary thing emitting events.
![Application Insights Userflow](https://raw.githubusercontent.com/daveta/analytics/master/userflow.PNG)


