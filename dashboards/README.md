# Bot Dashboards
We are creating an Azure Dashboard in v4.3.  This is also what Application Insights uses for their dashboards.  There are two types of dashboards for Bots we are deploying.

## System Health dashboard (1 of 2)
An Application Insights-based dashboard, where the visualizations and data retrieval is performed within the Application Insights components.

The dashboard here is 100% recycled components that have been tested, the work is when and where to publish the dashboard.  Ideally, it

## Conversation Health dashboard (2 of 2)

### % Complete Dialog

```sql
customEvents
| where name=="WaterfallStart"
| extend DialogId = customDimensions['DialogId']
| extend InstanceId = tostring(customDimensions['InstanceId'])
| join kind=leftouter (customEvents | where name=="WaterfallComplete" | extend InstanceId = tostring(customDimensions['InstanceId'])) on InstanceId    
| summarize starts=countif(name=='WaterfallStart'), completes=countif(name1=='WaterfallComplete') by bin(timestamp, 1d), tostring(DialogId)
| project Percentage=max_of(0.0, completes * 1.0 / starts), timestamp, tostring(DialogId) 
| render timechart
```

### % Cancel Dialog
```sql
customEvents
| where name=="WaterfallStart"
| extend DialogId = customDimensions['DialogId']
| extend InstanceId = tostring(customDimensions['InstanceId'])
| join kind=leftouter (customEvents | where name=="WaterfallCancel" | extend InstanceId = tostring(customDimensions['InstanceId'])) on InstanceId    
| summarize starts=countif(name=='WaterfallStart'), completes=countif(name1=='WaterfallCancel') by bin(timestamp, 1d), tostring(DialogId)
| project Percentage=max_of(0.0, completes * 1.0 / starts), timestamp, tostring(DialogId) 
| render timechart
```

### % Abandon Dialog
```sql
customEvents
| where name=="WaterfallStart"
| extend DialogId = customDimensions['DialogId']
| extend InstanceId = tostring(customDimensions['InstanceId'])
| join kind=leftouter (customEvents | where name=="WaterfallComplete" | extend InstanceId = tostring(customDimensions['InstanceId'])) on InstanceId    
| join kind=leftouter (customEvents | where name=="WaterfallCancel" | extend InstanceId = tostring(customDimensions['InstanceId'])) on InstanceId    
| summarize starts=countif(name=='WaterfallStart'), cancels=countif(name=='WaterfallCancel'), completes=countif(name1=='WaterfallComplete') by bin(timestamp, 1d), tostring(DialogId)
| project Percentage=max_of(0.0, (starts-(completes+cancels)) * 1.0 / starts), timestamp, tostring(DialogId) 
| render timechart
```

### % Complete  Intent

Note: All the Intent ones may merge the last two queries.  This may perform a bit better.  That will do one less table join (albeit on a very small table).  The QP doesn't appear to be smart enough to combine.

In my testing, this appeared to be a bit more consistent.

```sql
let CompleteWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallComplete"
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize completes=count() by bin(timestamp, 1d), intentName
};
let StartWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize starts=count() by bin(timestamp, 1d), intentName
};
StartWithIntentAggregate
| join kind=leftouter (CompleteWithIntentAggregate) on timestamp, intentName
| project Percentage=max_of(0.0, completes * 1.0 / starts), timestamp, tostring(intentName)
| render timechart
```

### % Cancel Intent
```sql
let CancelWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallCancel"
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize completes=count() by bin(timestamp, 1d), intentName
};
let StartWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize starts=count() by bin(timestamp, 1d), intentName
};
StartWithIntentAggregate
| join kind=leftouter (CancelWithIntentAggregate) on timestamp, intentName
| project Percentage=max_of(0.0, completes * 1.0 / starts), timestamp, tostring(intentName)
| render timechart
```

### % Abandon Intent
```sql
let CompleteWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallComplete"
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize completes=count() by bin(timestamp, 1d), intentName
};
let CancelWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallCancel"
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize cancels=count() by bin(timestamp, 1d), intentName
};
let StartWithIntentAggregate = () {
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize starts=count() by bin(timestamp, 1d), intentName
};
StartWithIntentAggregate
| join kind=leftouter (CompleteWithIntentAggregate) on timestamp, intentName
| join kind=leftouter (CancelWithIntentAggregate) on timestamp, intentName
| project Percentage=max_of(0.0, (starts - (cancels + completes)) * 1.0 / starts), timestamp, tostring(intentName) 
| render timechart
```
### Count Intent

```sql
customEvents
| where name startswith "LuisResult"  
| where timestamp > ago(24d) 
| summarize count() by bin(timestamp, 1d), name
| render timechart
```
### Completed by Intent
```sql
-- The inner join set (WaterfallComplete) could be materialized if scale becomes a problem.
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallComplete"
     | where timestamp > ago(24d)
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize count() by intentName 
| render barchart 
```
### Cancelled  by Intent
```sql
customEvents
| join(
  customEvents 
   | where name == "WaterfallStart" 
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallCancel"
     | where timestamp > ago(24d)
     | extend instanceId = tostring(customDimensions['InstanceId']))
     on instanceId
   | project operation_Id ) 
    on operation_Id
| where name startswith "LuisResult"
| extend intentName = tostring(customDimensions['intent'])
| summarize count() by intentName 
| render barchart 
```


## Count Incomplete by Dialog 
Histogram of (current) incomplete dialogs.
```sql
customEvents 
   | where name == "WaterfallStart" 
   | where timestamp > ago(24d)   
   | extend DialogId = customDimensions['DialogId']
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join kind=leftanti (
     customEvents
     | where name == "WaterfallComplete" 
     | extend instanceId = tostring(customDimensions['InstanceId'])
     )
     on instanceId
   | summarize count() by bin(timestamp, 1d), tostring(DialogId)
   | render barchart 
```
## Count Complete by Dialog 
Histogram of (current) complete dialogs.
```sql
customEvents 
   | where name == "WaterfallStart" 
   | where timestamp > ago(24d)   
   | extend DialogId = customDimensions['DialogId']
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join (
     customEvents
     | where name == "WaterfallComplete" 
     | extend instanceId = tostring(customDimensions['InstanceId'])
     )
     on instanceId
   | summarize count() by bin(timestamp, 1d), tostring(DialogId)
   | render barchart 
```



### Incomplete by Intent

```sql
-- The inner join set (WaterfallComplete) could be materialized if scale becomes a problem.
customEvents
| where name startswith "LuisResult"
| where timestamp > ago(1d)   
| join (
  customEvents 
   | where name == "WaterfallStart"
   | extend instanceId = tostring(customDimensions['InstanceId'])
   | join kind=leftanti (
     customEvents
     | where name == "WaterfallComplete"
     | extend instanceId = tostring(customDimensions['InstanceId'])
     )
     on instanceId
  ) 
  on operation_Id
| summarize count() by bin(timestamp, 1d), name 
| render barchart 
```

### Average Steps per Dialog
Note: This may be inaccurate, depending how Waterfall implemented.  If steps repeated over and over per dialog, it's going to have to factor in count of `WaterfallStart` into calculation. 

```sql
customEvents
| where timestamp > ago(24d) 
| extend DialogId = customDimensions['DialogId']
| where name == "WaterfallStep"
| summarize count() by bin(timestamp, 1h), tostring(DialogId), user_Id
| summarize avg(count_) by tostring(DialogId)
| render barchart
```

### Average Dialog Duration
Durations are difficult to graph in Kusto (haven't found a meaningful visualization).
```sql
customEvents
| where name=="WaterfallStart"
| extend DialogId = customDimensions['DialogId']
| extend instanceId = tostring(customDimensions['InstanceId'])
| join kind=leftouter (customEvents | where name=="WaterfallCancel" | extend instanceId = tostring(customDimensions['InstanceId'])) on instanceId 
| join kind=leftouter (customEvents | where name=="WaterfallComplete" | extend instanceId = tostring(customDimensions['InstanceId'])) on instanceId 
| extend duration = case(not(isnull(timestamp1)), timestamp1 - timestamp, 
                       not(isnull(timestamp2)), timestamp2 - timestamp, 
                       now()-timestamp)
| summarize avg(duration) by bin(timestamp, 1d),  tostring(DialogId)
```
### Sentiment P90
Sentiment analysis is the ability of LUIS to understand if a user’s utterance is positive, neutral, or negative. LUIS returns a sentiment and a score between 0 and 1 (negative < 0.5 (neutral) < positive).
```sql
customEvents
| where name startswith("LuisIntent")
| extend SentimentLabel = customDimensions['SentimentLabel']
| extend SentimentScore = customDimensions['SentimentScore']
| summarize percentiles(todouble(SentimentScore),50,90,99) by bin(timestamp, 1h), tostring(SentimentLabel)
| render timechart
```

### Sentiment by Dialog
Sentiment analysis is the ability of LUIS to understand if a user’s utterance is positive, neutral, or negative. LUIS returns a sentiment and a score between 0 and 1 (negative < 0.5 (neutral) < positive).
TODO

### Total Messages


## How to test Azure Dashboards

Look at [create a template from the json](https://docs.microsoft.com/en-us/azure/azure-portal/azure-portal-dashboards-create-programmatically#create-a-template-from-the-json) documentation for details on how the contents of the template works, but essentially you name variables and replace when deploying.

## Parameters
For the Application Insights-based dashbaord, we define parameters for our template.  For example:
```json
"parameters": {
    "insightsComponentName": {
      "type": "string"
    },
    "insightsComponentResourceGroup": {
      "type": "string"
    },
    "dashboardName": {
      "type": "string"
    }
  }
```



## Testing

Easiest way to test is using [Azure portal's template deployment page](https://portal.azure.com/#create/Microsoft.Template).  
- Click ["Build your own template in the editor"]
- Copy and Paste
- Click "Save"
- Populate `Basics`: 
   - Subscription: <your test subscription>
   - Resource group: <a test resource group>
   - Location: <such as West US>
- Populate `Settings`:
   - Insights Component Name: <like `core672so2hw`>
   - Insights Component Resource Group: <like `core67`>
   - Dashboard Name:  <like `'ConversationHealth'` or `SystemHealth`>
- Click `I agree to the terms and conditions stated above`
- Click `Purchase`
- Validate
   - Click on [`Resource Groups`](https://ms.portal.azure.com/#blade/HubsExtension/Resources/resourceType/Microsoft.Resources%2Fsubscriptions%2FresourceGroups)
   - Select your Resource Group from above (like `core67`).
   - If you don't see a new Resource, then look at "Deployments" and see if any have failed.
   - Here's what you typically see for failures:
```json
{"code":"DeploymentFailed","message":"At least one resource deployment operation failed. Please list deployment operations for details. Please see https://aka.ms/arm-debug for usage details.","details":[{"code":"BadRequest","message":"{\r\n \"error\": {\r\n \"code\": \"InvalidTemplate\",\r\n \"message\": \"Unable to process template language expressions for resource '/subscriptions/45d8a30e-3363-4e0e-849a-4bb0bbf71a7b/resourceGroups/core67/providers/Microsoft.Portal/dashboards/Bot Analytics Dashboard' at line '34' and column '9'. 'The template parameter 'virtualMachineName' is not found. Please see https://aka.ms/arm-template/#parameters for usage details.'\"\r\n }\r\n}"}]}
```




