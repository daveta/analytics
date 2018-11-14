## Bot Troubleshooting 500 errors
The first step in troubleshooting 500 errors is enabling Application Insights. 

### Enable on ASP.Net Core
Step by step>
### Enable on ASP.Net Web API
Step by step>
### Enable on node.js
Step by step>


## Run queries on Application Insights


## No Application Insights Events from ASP.Net Core 
If you are receiving 500 errors and there are no further events within Application Insights from your bot, check the following:
**Ensure bot runs locally**
Make sure your bot runs locally first with the emulator.  
**Ensure configuration files are being copied**
Make sure your `.bot` configuration file and `appsettings.json` file are being packaged correctly during the deployment process.
**Ensure Application Insights assemblies copied**
During the deployment process, ensure the Application Insights assemblies are bing copied.  If you right click, publish your project, watch the output window even if it runs locally. 
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

