# Bot Analytics v4.2 Proposal



## Priority 0: Address Bot Channel Service Top 4 Scenarios
We regularly receive issues about bots having issues and customers are left scratching their heads.  The following are considered the **top issues** that cover a high percentage of issues encountered by customers bots through the lens of the Channel Service:

- 500 errors on bot 
- Timeout (from channel to bot)
- Bot Never responds 
- Bot works in ABS (Web Chat Test) but doesn’t work in channel X

In order to help customers quickly diagnose these issues (and enable UI/tools to surface to customers), the approach is to enable Application Insights for the Bot and beef up event data sent from the Channel Service (into the customer's Application Insights container) to completely diagnose issues.



Here is the proposal (**Note:** Ownership is shared across teams):



Item |Owner | Description
--- | --- | ---
[Enable ABS to enable Application Insights](https://fuselabs.visualstudio.com/BotFramework/_workitems/edit/25693) | Bot Portal Team | Currently, if you provision a bot through the portal and opt into Application Insights and deploy  the ABS template, Application Insights is not configured correctly.  The portal team believes there's a way to enable this (bot configuration and appsettings.json)
[Enable W3C distributed tracing](https://fuselabs.visualstudio.com/BotFramework/_workitems/edit/25537) | Bot Service Team | To correlate queries with Bot application insights 
Verify integration with Distributed Tracing | daveta/Bot Service Team | Need to test and verify operation id's are being transferred and correlated correctly.  Ideally we set up an integration environment for testing.
Ensure Request/Dependency/Event/Trace telemetry being logged in Bot and correlated | daveta | Ensure the appropriate initializers installed.
Ensure ILogger telemetry being logged in Application Insights Trace | daveta | Application Insights has ability to intercept ILogger events. 
[Log Http Status code](https://fuselabs.visualstudio.com/BotFramework/_workitems/edit/25691) | Bot Service Team | To diagnose 500 errors on bot.
[Log error on Channels into customer App Insights container](https://fuselabs.visualstudio.com/BotFramework/_workitems/edit/25692) | Bot Service Team | To diagnose never respond errors, and Channel X errors
Queries/Documentation to detect 500 errors | daveta/Bot Service Team | Once we have correlated data, we can write some documentation to demonstrate how to diagnose.  For true "500" bot errors, we can demonstrate with existing tools and some queries.  For Channel "500" errors, this will be a little more difficult depending where it fails within the channel. *(Stretch)* If 500 error is due to external dependency, clearly state the dependency and timeout in a query result.  For example, if CosmosDB is timing out, it should state so.
Queries/Documentation to detect Bot Timeout errors | daveta/Bot Service Team | Once we have correlated data, we can write some documentation to demonstrate how to diagnose.  
Queries/Documentation to detect Bot Never Responds errors | daveta/Bot Service Team | Once we have correlated data, we can write some documentation to demonstrate how to diagnose.  
Queries/Documentation to detect Bot Works in Web Chat but not in Channel X | daveta/Bot Service Team | Once we have correlated data, we can write some documentation to demonstrate how to diagnose.  



### Priority 1: Enable Out-of-the-box reports

See [the list of reports that should be enabled for bots](https://github.com/daveta/analytics/blob/master/app_insights_for_bots.md) by enabling Application Insights.

Here is the proposal:



Item |Deliverable | Description
--- | --- | ---
Simplify Application Insights registration for Bot Component | Product | Application Insights is a train wreck configuring.  Customers need carnal knowledge of their startup sequences which is terrible.  Create a platform-level appropriate integration for ASP.Net Core/WebAPI/Node.js.  Should be usable with MVC patterns.  (Some checks: Ensure ILogger hooked up, ensure appsettings.json correct, ensure our initializer telemetry registered, ensure our ASP.Net Middleware properly registered, etc) 
Simple Disable of Application Insights Feature | Product (Docs/Samples) | Disabling Application Insights should be simple (ideally single line) and any telemetry within the product should honor it.
AppInsight UserID/SessionID Telemetry | Product | Modifying the events to use Bot user/session ID's enables other Application Insights reports such as "Funnel" in propert context
LUIS Recognizer Telemetry | Sample | Keep a sample until such time that we have  either of the following:  **(A)** A reasonable understanding of  reporting scenarios that will satisfy 90% of reports to feel comfortable hard coding the data being retrieved. **(B)** we create a flexible/configuration-based method of collecting data.  (See below).  By choosing a sample, we won't be addressing endless customer enhancement issues of "Please add property X in LUIS intent data collection because I need it for my report".  The customer can modify for their report themselves.
QnA Recognizer Telemetry | Sample | See LUIS recognizer above.
Bot Message  Telemetry Middleware | Sample | See LUIS recognizer above.
*(Stretch)* Configuration-based mapping collector | Product |A configuration-based method of specifying which properties to pull from specific objects would allow customers flexibility to change the properties being logged, and where the properties will be logged (scoped within the Application Insights Custom Events and Custom Metrics).  Should support 1:M relationships.  (I question whether this component should ever be developed, bang for the buck doesn't seem worth it).

### Priority 1: Satisfy Virtual Assistant scenarios
See [nice writeup from Virtual Assistant team](https://na01.safelinks.protection.outlook.com/?url=https%3A%2F%2Fmicrosoft-my.sharepoint.com%2F%3Ap%3A%2Fp%2Fsrmallan%2FETf7DjcCRKRNvumlc5Ezx0EB2cTn6EYI8p_UrWOus6Q2pA%3Fe%3D15m8a5&data=02%7C01%7Cdewainr%40microsoft.com%7C619bfe4ba5f4476fbfb508d63ea97129%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C636765294804345126&sdata=lJHT8yLGmV56L%2BktJ9IeYw0r5jhCVXeCi%2BNeglEQZ8o%3D&reserved=0) about the metrics they would like.



Item |Deliverable | Description
--- | --- | ---
Telemetry Waterfall Dialog | Product |Enables logging to satisfy "Skills" analytics.  More comfortable putting this into product, given the nature of the data being collected that can be enhanced over time.
Prioritize metrics | Doc |  There are several metrics defined in the documentation.  Need to prioritize and document order.
Review metrics with botmetrics@microsoft alias | Email | IPA team/Darren Jefford's team
Evaluate LUIS \ QnA Telemetry | Sample  | Once metrics prioritized, reuse/modify existing telemetry components. Resulting telemetry should serve both sets of reports (superset) 
New sample demonstrating Telemetry Waterfall | Sample | Something to generate data to drive the new PowerBI template
Build PowerBI Template | Sample | New reports showing off metrics 
Review PowerBI Template | Meeting | Review with botmetrics@microsoft.com
*(Stretch)* Correlation/Causation for Skills | Sample | There are several "Surface to the user the reason why they aren't completing skills".  This is semi related to some LUIS scenarios.



### Priority 2: Enable close-the-loop scenarios for LUIS/QnA

LUIS team very interested in making a SAT/DSAT detector, segmentation/ recommender/etc.

From @Cez:

Item |Deliverable | Description
--- | --- | ---
Baseline | Design Doc | Identification of apps we could use and collect/retain logs
Close the loop | Design doc | Once we have baseline measurement, attempt to close the loop to suggest labls for utterances. 
Segmentation | Design doc | Additional feedback for improving the app and suggestions for content.  
