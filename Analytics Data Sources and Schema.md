# Analytics Data Sources and Schema

## Summary
A typical Azure Bot Service employs several classes of components that work together to create a production bot.  Ths document describes data source and proposes the schema to collect for each data source.  A primary goal is to enable tracing a message flow throughout the system through correlated identifiers.  This data is optionally collected and intended for the customer only.  The internal Microsoft teams will not have access to the data collected.

This is primarily capturing events that occur on the wire.  There are also events that occur within each service as processing and custom events the bot developer will want to correlate.

![Summary of data sources](https://raw.githubusercontent.com/daveta/analytics/master/AnalyticsDataSources.png)

Also see "Analytics and Logging" for further background.

## Background
Today the Bot developer can opt into purchasing Application Insights for their subscription. We are adopting the Application Insights infrastructure for Bot Analytics.    Adopting Application Insights means adopting their data model. 

```https://docs.microsoft.com/en-us/azure/application-insights/application-insights-data-model```

Application Insights has many reports (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-usage-overview) that can be used if  tables are populated  appropriately. In addition, we inherit the extensibility model (https://docs.microsoft.com/en-us/azure/application-insights/app-insights-api-custom-events-metrics) that Application Insights has in terms of custom events that can be logged and leveraged.  Not to mention the other tooling.


```Note that Application Insights is not a developer ELK or tracing infratructure play (although you can use it this way if you want to pay the money).  They cite Amazon XRay as their compete.```



### Dependency
Understanding flow between components is a primary scenario that our customers are interested. Application Insights has a concept of a dependency that allows you to track a single operation that is serviced by multiple components.  
![Application Insights Sample App map](https://raw.githubusercontent.com/daveta/analytics/master/appmap.PNG)

We can use this concept to model operations within the Bot Framework and surface information about our primary dependencies (LUIS and QnA maker) and developers can model their custom components as well.

We will assume the same Application Insights Instrumentation Key.

The following example illustrates how we can use this infrastructure.

#### Channel Service Telemetry
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
```
NEW WORK!!
In addition to the "Correlation ID" a "Dependency ID" is required to fully light up Application Insights.  
We also want to track a bot as a depdency, so we will add a dependency call.
```csharp
var d = new DependencyTelemetry(
    dependencyTypeName: "Http",
    target: $"mybot.com", //host name
    dependencyName: "POST /api/messages",
    data: "https://mybot.com/api.messages",  // Command which initiated call
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
d.Context.Operation.ParentId = r.Id;
d.Context.Operation.Id = TRACE_ID;
d.Context.Cloud.RoleName = "Frontend"; // this is the name of the node on app map

new TelemetryClient() { InstrumentationKey = SINGLE_INSTRUMENTATION_KEY }.TrackDependency(d);
```
Today a HTTP header is sent from the Channel Service to the bot passing the correlation ID.  In addition, the Dependency ID will also need to be passes.


#### Customer Bot Receive/Send Telemetry
The bot receives the Correlation and Dependency ID's and then logs it's own Application Insights Request message.  This will be peformed automatically in the SDK in the connector.
Request telemetry will be logged with the same Correlation ID.

```csharp
r = new RequestTelemetry(
    name: "POST /api/messages",
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    responseCode: "200",
    success: true)
{
    Url = new Uri("https://mybot.com/api/messages")
};
r.Context.Operation.Id = CORRELATION_ID; // received from http header
r.Context.Operation.ParentId = d.Id; // received from http header (Dependency ID)
r.Context.Cloud.RoleName = "Bot Service"; // this is the name of the node on app map

new TelemetryClient() { InstrumentationKey = SINGLE_INSTRUMENTATION_KEY }.TrackRequest(r);
```
At this point, we now are correlated with the original request coming from the Channel Service.

#### Track LUIS/QnA invocation Telemetry
During processing of the Bot request, other Cognitive Services are employed.  Within each client component, it will log Dependency telemetry to reflect the call.

```csharp
d = new DependencyTelemetry(
    dependencyTypeName: "http",
    target: $"api.cognitive.microsoft.com",
    dependencyName: "GET /luis",
    data: "https://api.cognitive.microsoft.com/luis/v2.0/apps?q=turn%20on%20the%20bedroom%20light",  // Command which initiated call
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
d.Context.Operation.ParentId = r.Id;
d.Context.Operation.Id = TRACE_ID;
d.Context.Cloud.RoleName = Bot Service"; // this is the name of the node on app map

new TelemetryClient() { InstrumentationKey = SINGLE_INSTRUMENTATION_KEY }.Track(d);```
```


#### Track Middleware Dependency
Middleware components will track their own dependency.
For example, here's what the AI.Translate logs:
```csharp
d = new DependencyTelemetry(
    dependencyTypeName: "middleware",
    target: $"ai.translation",
    dependencyName: "Translate Text",
    data: "<Text to convert>",  // Command which initiated call
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
d.Context.Operation.ParentId = r.Id;
d.Context.Operation.Id = TRACE_ID;
d.Context.Cloud.RoleName = Bot Service"; // this is the name of the node on app map

new TelemetryClient() { InstrumentationKey = SINGLE_INSTRUMENTATION_KEY }.Track(d);```
```

#### Track Storage Dependency
Any storage should be modeled as a dependency.  For example, when we log transcripts we will log:
```csharp
d = new DependencyTelemetry(
    dependencyTypeName: "storage",
    target: $"Azure Blob",
    dependencyName: "Store",
    data: "Transcripts",  // Command which initiated call
    startTime: DateTimeOffset.Now,
    duration: TimeSpan.FromSeconds(1),
    resultCode: "200",
    success: true);
d.Context.Operation.ParentId = r.Id;
d.Context.Operation.Id = TRACE_ID;
d.Context.Cloud.RoleName = Bot Service"; // this is the name of the node on app map

new TelemetryClient() { InstrumentationKey = SINGLE_INSTRUMENTATION_KEY }.Track(d);```
```

### Funnel
Funnels are a series of Application Insight custom events.  These events serve as milestones in the funnel.

![Application Insights Sample Funnel](https://raw.githubusercontent.com/daveta/analytics/master/funnel.PNG)

```csharp
telemetry.TrackEvent("MyEvent");
```
Prompts represent customer input.  Prompts will emit a new event in the format:
```
<PromptType>:<Text>
```
For example, "ConfirmPrompt: Do you want to book the reservation?".

### User Flow
Userflow can give a good idea of where the common paths customers are taking.  This again can derive from events.
![Application Insights Userflow](https://raw.githubusercontent.com/daveta/analytics/master/userflow.PNG)


