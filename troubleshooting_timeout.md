## Bot Troubleshooting Timeout errors
The first step in troubleshooting timeout errors is enabling Application Insights. 

### Enable Application Insights on ASP.Net
For basic Application Insights support, consult [here](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-asp-net).
The Bot Framework provides an additional level of Application Insights telemetry, but it will not be required for diagnosing timeout errors.

### Enable on node.js
For basic Application Insights support, consult [here](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-nodejs?toc=/azure/azure-monitor/toc.json).
The Bot Framework provides an additional level of Application Insights telemetry, but it willnot be required for diagnosing timeout errors.

## Query for exceptions
The easiest method of analyzing timeout errors is to begin with exceptions.

The following queries will tell you the most recent exceptions:
```sql
exceptions 
| order by timestamp desc
| where type == "Microsoft.Bot.Schema.BotTimeoutException" 
| project timestamp, operation_Id, appName 
```

From the previous query, select a few `operation_Id`'s and then look for more information, setting the variable `my_operation_id` with a operation_Id selected:

```sql
let my_operation_id = "d298f1385197fd438b520e617d58f4fb";
let union_all = () {
    union
    (traces | where operation_Id == my_operation_id),
    (customEvents | where operation_Id == my_operation_id),
    (requests | where operation_Id == my_operation_id),
    (dependencies | where operation_Id  == my_operation_id),
    (exceptions | where operation_Id == my_operation_id)
};

union_all
    | order by timestamp desc
```
If you have only `exceptions`, analyze the details and see if they correspond to anywhere in the code. If you only see exceptions coming from the Channel Connector (`Microsoft.Bot.ChannelConnector`) then see [Web API](#no-application-insights-events-from-asp.net-web-api) or [Core](#no-application-insights-events-from-ap.net-core) to ensure that Application Insights is set up correctly.


## No Application Insights Events from ASP.Net Web API
If you are receiving 500 errors and there are no further events within Application Insights from your bot, check the following:
**Ensure bot runs locally**
Make sure your bot runs locally first with the emulator.  
**Ensure configuration files are being copied**
Make sure your `.bot` configuration file and `appsettings.json` file are being packaged correctly during the deployment process.
**Validate Application Insights assemblies dependencies**
During the deployment process, ensure the Application Insights assemblies:
- Microsoft.ApplicationInsights
- Microsoft.ApplicationInsights.TraceListener
- Microsoft.AI.Web
- Microsoft.AI.WebServer
- Microsoft.AI.ServeTelemetryChannel
- Microsoft.AI.PerfCounterCollector
- Microsoft.AI.DependencyCollector
- Microsoft.AI.Agent.Intercept

**Verify appsettings.json**
Within your `appsettings.json` file ensure the Instrumentation Key is set.
```json
{
  "Logging": {
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Debug",
      "System": "Information",
      "Microsoft": "Information"
    },
    "Console": {
      "IncludeScopes": "true"
    }  
  }
}
```

**Verify .bot config file**
Ensure there's an Application Insights key.

```json
        {
            "type": "appInsights",
            "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "resourceGroup": "my resource group",
            "name": "my appinsights name",
            "serviceName": "my service name",
            "instrumentationKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "applicationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "apiKeys": {},
            "id": ""
        },
```
**Check logs**
ASP.Net contains logs at the server level that can be inspected.

*Set up browser to watch logs*
Using a browser, navigate to the Azure Portal and navigate to your bot.  Click on "App Service Settings"/"All App service settings" to expand and see all service settings.

Click on Monitoring/Diagnostics Logs, ensure logging "Application Logging (Filesystem)" is turned on.  Be sure to click the "Save" button.

Click on Monitoring/Log Stream.  Select "Web server logs" and ensure you see a message you are connected.  It should look something like the following:

```bash
Connecting...
2018-11-14T17:24:51  Welcome, you are now connected to log-streaming service.
```
Keep this window open.

*Set up browser to restart bot service*
Using a separate browser, navigate to the Azure Portal and navigate to your bot.  Click on "App Service Settings"/"All App service settings" to expand and see all service settings.

Click on "Overview", and then click on the "Restart" button.  It will prompt if you are sure, select yes.

Return to the first browser window watching the logs.

Verify that you are receiving new logs.  If there is no activity, redeploy your bot.
Switch to the "Application logs" and look for any errors.
## No Application Insights Events from ASP.Net Core 
If you are receiving 500 errors and there are no further events within Application Insights from your bot, check the following:
**Ensure bot runs locally**
Make sure your bot runs locally first with the emulator.  
**Ensure configuration files are being copied**
Make sure your `.bot` configuration file and `appsettings.json` file are being packaged correctly during the deployment process.
**Ensure Application Insights assemblies copied**
During the deployment process, ensure the Application Insights assemblies are being copied.  If you right click, publish your project, watch the output window even if it runs locally. 
**Verify appsettings.json**
Within your `appsettings.json` file ensure the Instrumentation Key is set.
```json
{
  "botFilePath": "mybot.bot",
  "botFileSecret": "<my secret>",
  "ApplicationInsights": {
    "InstrumentationKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  }
}
```
**Verify .bot config file**
Ensure there's an Application Insights key.

```json
        {
            "type": "appInsights",
            "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "resourceGroup": "my resource group",
            "name": "my appinsights name",
            "serviceName": "my service name",
            "instrumentationKey": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "applicationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
            "apiKeys": {},
            "id": ""
        },
```
**Check logs**
ASP.Net contains logs at the server level that can be inspected.

*Set up browser to watch logs*
Using a browser, navigate to the Azure Portal and navigate to your bot.  Click on "App Service Settings"/"All App service settings" to expand and see all service settings.

Click on Monitoring/Diagnostics Logs, ensure logging "Application Logging (Filesystem)" is turned on.  Be sure to click the "Save" button.

Click on Monitoring/Log Stream.  Select "Web server logs" and ensure you see a message you are connected.  It should look something like the following:

```bash
Connecting...
2018-11-14T17:24:51  Welcome, you are now connected to log-streaming service.
```
Keep this window open.

*Set up browser to restart bot service*
Using a separate browser, navigate to the Azure Portal and navigate to your bot.  Click on "App Service Settings"/"All App service settings" to expand and see all service settings.

Click on "Overview", and then click on the "Restart" button.  It will prompt if you are sure, select yes.

Return to the first browser window watching the logs.

Verify that you are receiving new logs.  If there is no activity, redeploy your bot.
Switch to the "Application logs" and look for any errors.

