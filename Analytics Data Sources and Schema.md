# Analytics Data Sources and Schema

## Summary
A typical Azure Bot Service employs several classes of components that work together to create a production bot.  Ths document describes data source and proposes the schema to collect for each data source.  A primary goal is to enable tracing a message flow throughout the system through correlated identifiers.  This data is optionally collected and intended for the customer only.  The internal Microsoft teams will not have access to the data collected.

This is primarily capturing events that occur on the wire.  There are also events that occur within each service as processing and custom events the bot developer will want to correlate.

![Summary of data sources](https://raw.githubusercontent.com/daveta/analytics/master/AnalyticsDataSources.png)

Also see "Analytics and Logging" for further background.

## 1. Extenal Messaging Service

Examples of external messaging services are Slack, Facebook Messenger, WeChat, etc.

Data is logged from the Bot Connector Service:
```json
{
    "@context" : "http://www.microsoft.com/botFramework/schemas/v1",
    "@type" : "botAnalyticsRecord",
    "@id" : "{activityId}",
    "receivedAtDateTime" : "2017-08-25T10:49:05.234Z",
    "botId" : "{Unique bot identifier}"
}
```
```json
{
    "@type" : "botAnalyticsRecord, botConversation",
    "conversationId" : "{converatonId}",
    "conversationTurn" : "{int}"
}
```

Properties for *botAnalyticsRecord*
Property | Expected Type | Description
--- | --- | ---
receivedAtDateTime | [DateTime](http://schema.org/DateTime) | The time the activity was received by the bot.
botId | [identifier](http://schema.org/identifier) | The identifer that uniquely identifes the bot.

**Note**: Not all records will a Microsoft defined activity Id. Examples of this include bot initiated processing in the context of a timer or other external signals. For these activities, the bot should create a new unique Activity Id and populate the @id field with that result.

## 2. 1st Party Messaging Service
Examples of 1st party messaging services are Skype, Microsoft Teams, etc.  The only distinction is these are services run internally within Microsoft.

## 3. Customer Bot Service
There are two messages that are passed.
### Connector Service to Bot
These are Activity events that occur when an end-use has posted a message or performed an action that the Connector Service notifies the Bot service about.
### Bot to Connector Service
These are Activity events that occur whena  response is posted back from the ser


## 4. QNA Maker

## 5. LUIS


```json
{
    "@type" : "botAnalyticsRecord, luisOperations",
    "luisOperations" : [
        {
            "@id" : "{LUIS Operation ID}",
            "requestDateTime" : "{datetime}",
            "duration" : "{timespan}",
            "responseCode" : "200",
            "languageModelId" : "{Language Model Id}",
            "q":"turn on the camera",
            "rawResponse":"[{\"intent\":\"OpenCamera\",\"score\":0.976928055},
                        {\"intent\":\"None\",\"score\":0.0230718572}]"
        },
        {
            "@id" : "{LUIS Operation ID}",
            "requestDateTime" : "{datetime}",
            "duration" : "{timespan}",
            "responseCode" : "200",
            "languageModelId" : "{Language Model Id}",
            "q":"turn on the camera",
            "rawResponse":"[{\"intent\":\"takePicture\",\"score\":0.976928055},
                        {\"intent\":\"None\",\"score\":0.0230718572}]"
        }
    ]
}
```

Properties for *luisOperations*

Note: LUIS Operations is an array of LuisOperation

Property | Expected Type | Description
--- | --- | ---
duration | [Duration](http://schema.org/Duration) | The elapsed time by the API call to Luis.
requestDateTime | [DateTime](http://schema.org/DateTime) | The time the request to Luis was initiated.
languageModelId | [identifier](http://schema.org/identifier) | The id of the LUIS model used for the operation.
q | String | The Users Utterance (the "q" parameter passed into the LUIS query)
rawResponse | string | The raw response from LUIS.
responseCode | int | The HTTP Response code returned from LUIS


## 7. Customer Code
For bots that navigate users between prompts, a navigation facet is added into the model. This facet treats each prompt as a developer geneated IRI (similar to a web page), and provides origin -> destion nodes.
;
```json
{
    "@type" : "botAnalyticsRecord, botNavigation",
    "origin": "{userDefinedIRI}/promptName",
    "destination": "{userDefinedIRI}/promptName"
}
```

Properties for *botNavigation*

Property | Expected Type | Description
--- | --- | ---
origin | IRI | The developer defined identifer that identifies a prompt within the scope of a given bot. This field represents the prompt the user last visited. In terms of workflow, the user visited this prompt, entered an utterance, and then (as the result of this activity) navigated to the *destination* prompt. This field is generally expected to be of the form: *bot://companyname/botname/dialogname/promptName*.
destination | IRI | The developer defined identifer, in IRI form, that identifies a prompt within the scope of a given bot. This field represents the prompt the user is being redirected to as part of the activity being processed. In terms of workflow, the user visited the *origin* prompt, entered an utterance, and now is being send to a new prompt. This field is generally expected to be of the form: *bot://companyname/botname/dialogname/promptName*.


For example:
1. Bot sends a greeting prompt to the user. "Hello. I'm ComBot. Ask me about COM Interfaces.".
    * The URI for the prompt is: bot://contoso.com/combot/greetingPrompt
2. User types "What is IUnknown?"
3. Bot sends a card with details around IUnknown.
    * The URI for the prompt is: bot://contoso.com/combot/IUnknownOverviewCard


## 8. Cosmos DB Graph Database
The destination for all of the data will reside in Cosmos DB as JSON-LD documents.



## 9. Application Insights
Existing Application Insights data collection will still exist.


