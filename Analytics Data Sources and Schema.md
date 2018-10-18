# Analytics Data Sources and Schema

## Summary
A typical Azure Bot Service employs several components that work together to create a production bot.  The primary goal is to enable tracing a message flow throughout the system through correlated identifiers.  This data is optionally collected and intended for customer-use only.  The internal Microsoft teams will not have access to the data collected.

This is primarily focused on capturing events that occur after the customer Bot has received a message.  There are events that occur in-process and external services the Bot requires for storage, etc.  Event correlation extending out to the Bot Connector Service and beyond is not a priority, as the company's distributed event correlation story gets ironed out (see "Application Insights Identifiers" section below).

![Summary of data sources](https://raw.githubusercontent.com/daveta/analytics/master/AnalyticsDataSources.png)

## Background
Today the Bot developer can opt into purchasing Application Insights for their subscription. Currently, the Connector Service will log events for Exceptions and Custom Events for messages received.  The Bot Framework SDK is adopting this Application Insights infrastructure for Bot Analytics.    Adopting Application Insights means adopting their data model *to a limited degree*.

```https://docs.microsoft.com/en-us/azure/application-insights/application-insights-data-model```

Application Insights has many reports (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-usage-overview) that can be used if  tables are populated  appropriately. In addition, we inherit the extensibility model (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-api-custom-events-metrics) that Application Insights has in terms of custom events that can be logged and leveraged.  Not to mention the other tooling.

### What is a Turn?
A "Turn" is a concept that represents a complete round trip within a Bot - from the original request to potentially multiple responses.  The data backing a turn begins when a message is received by a Bot.  You can link events in the turn via several identifiers.  

### Identifiers
There are several correlating concepts identifiers that are being employed at two levels that tie the events together.

**Bot Level Identifiers** 
*ActivityID (aka MessageID)*  - Created in the Channel Service, this represents a unique message id to/from the channel service.
*ReplyID* - When a Bot responds to a message, the ReplyID is the ActivityID of the original message.
*ConversationID* - Channel-specific representation of a conversation. 

**Application Insights Identifiers and Other Frameworks** 
- Application Insights is evolving their event correlation approach.  The following W3C standards is what they are evolving to.  As of Summer 2018, Application Insights has pushed in a PR that adopts this. (https://github.com/Microsoft/ApplicationInsights-dotnet-server/pull/945)

o	https://w3c.github.io/distributed-tracing/report-trace-context.html
o	https://w3c.github.io/distributed-tracing/report-correlation-context.html

This standard is anticipated to be complete by *December 2018*.

- The Codex team (which builds cV or "Correlation Vector") is a formerly internal-only effort (announced as an Open Source project on Oct 1, 2018) is another framework.  They are  working towards interoperability with the above W3C standards.  We don't have any integration with cV.
https://github.com/Microsoft/CorrelationVector

### CustomEvent: BotMessageReceived
**Logged From:** TelemetryLoggerMiddleware

- ActivityID
- Channel  (Source channel - e.g. Skype, Cortana, Teams)
- Text (Optional for PII)
- FromId
- FromName
- RecipientId
- RecipientName
- ConversationId
- ConversationName
- Locale

### CustomEvent: LuisIntent.INENTName
**Logged From:** TelemetryLuisRecognizer

- ActivityID
- CorrelationID
- Intent
- IntentScore
- Question
- ConversationId
- SentimentLabel
- SentimentScore

### QnAMessage
** Logged From:** TelemetryQnaMaker

- ActivityID
- CorrelationID
- Username
- ConversationId
- OriginalQuestion
- Question
- Answer
- Score (*Optional*: if we found knowledge)


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


