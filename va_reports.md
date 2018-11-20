# Virtual Assistant Bot Analytics

## Summary
Virtual Assistant will require additional telemetry logged from the Bot Framework.  This document attempts to describe the changes required.  The schema for the events is from the [Enterprise Template](https://github.com/Microsoft/AI/tree/master/templates/Enterprise-Template), the **NEW** tags indicate new columns/fields that need to be added.  Based on [the Virtual Assistant Bot Analytics documentation](https://na01.safelinks.protection.outlook.com/?url=https%3A%2F%2Fmicrosoft-my.sharepoint.com%2F%3Ap%3A%2Fp%2Fsrmallan%2FETf7DjcCRKRNvumlc5Ezx0EB2cTn6EYI8p_UrWOus6Q2pA%3Fe%3D15m8a5&data=02%7C01%7Cdewainr%40microsoft.com%7C619bfe4ba5f4476fbfb508d63ea97129%7C72f988bf86f141af91ab2d7cd011db47%7C1%7C0%7C636765294804345126&sdata=lJHT8yLGmV56L%2BktJ9IeYw0r5jhCVXeCi%2BNeglEQZ8o%3D&reserved=0).

The elements ~~striked through~~ below are logged, but not consumed for the Virtual Assistant reports.

### Identifiers
A new Application Insights Telemetry Initializer will be added in v4.2 Bot Framework SDK.  Assume the **UserID** and **ConversationID** and **ActivityID** reside in all `customEvents`.  These will manifest as `user_id`/`session_id` within the schema.

UserID = [ChannelID](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#channel-id) + [From.Id](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#from)

[ConversationID](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#conversation)

For more details on all the Activity ID's, see [the bot activity spec](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md)

### CustomEvent: BotMessageReceived (IpaBotMessageReceived)
**Logged From:** TelemetryLoggerMiddleware
Logged when bot receives new message.

- UserID (From Telemetry Initializer)
- ConversationID (From Telemetry Initializer)
- ActivityID (From Telemetry Initializer)
- Channel  (Source channel - e.g. Skype, Cortana, Teams)
- Text (Optional for PII)
- FromId
- FromName
- RecipientId
- RecipientName
- ConversationId
- ConversationName
- Locale
- **NEW** Geolocation per [activity spec](https://github.com/Microsoft/botframework-obi/blob/master/botframework-activity/botframework-activity.md#semantic-action-entities)

### CustomEvent: BotMessageSend (IpaBotMessageSent)
**Logged From:** TelemetryLoggerMiddleware
Logged when bot sends a message.

- UserID (From Telemetry Initializer)
- ConversationID (From Telemetry Initializer)
- ActivityID (From Telemetry Initializer)
- ReplyToID
- Channel  (Source channel - e.g. Skype, Cortana, Teams)
- RecipientId
- ConversationName
- Locale
- Text (Optional for PII)
- RecipientName (Optional for PII)

~~### CustomEvent: BotMessageUpdate~~
~~**Logged From:** TelemetryLoggerMiddleware~~
~~Logged when a message is updated by the bot (rare case)~~


~~### CustomEvent: BotMessageDelete~~
~~**Logged From:** TelemetryLoggerMiddleware~~
~~Logged when a message is deleted by the bot (rare case)~~



### CustomEvent: LuisIntent.INENTName (LUISResult)
**Logged From:** TelemetryLuisRecognizer
Logs results from LUIS service.

- UserId
- ConversationId
- ActivityId
- Intent
- IntentScore
- Question
- ConversationId
- SentimentLabel
- SentimentScore
- *LUIS entities*
- **NEW** DialogId


### CustomEvent: QnAMessage
**Logged From:** TelemetryQnaMaker
Logs results from QnA Maker service.

- ActivityID
- Username
- ConversationId
- OriginalQuestion
- Question
- Answer
- Score (*Optional*: if we found knowledge)


**NEW**
### CustomEvent: "WaterfallDialogStep" (BotFlowStatus)
Example: `profileDialog.Step4of4`
**Note**: Steps numbers may be skipped if no prompt is performed within the step.

**Logged From:** SDK when Logger present
Logs individual steps from a Waterfall Dialog.

- UserID (from Telemetry Initializer Id)
- Conversation ID (from Telemetry Initializer Id)
- DialogId


**NEW**
### CustomEvent: "WaterfallDialogConvert"
**Logged From:** SDK when Logger present
Logs when a Waterfall Dialog completes.

- UserID (from Telemetry Initializer Id)
- Conversation ID (from Telemetry Initializer Id)
- DialogId
- StepId

**NEW**
### CustomEvent: "WaterfallDialogCancel" (Name TBD)
**Logged From:** SDK when Logger present
Logs when a Waterfall Dialog is canceled.

- UserID (from Telemetry Initializer Id)
- Conversation ID (from Telemetry Initializer Id)
- DialogId
- StepId


~~### CustomEvent: "Activity"~~
~~**Logged From:** Channel Service~~
~~Logged by the Channel Service when a message received.~~

~~### Exception: Bot Errors~~
~~**Logged From:** Channel Service~~
~~Logged by the channel when a call to the Bot returns a non-2XX Http Response.~~


# Usage Metrics for Virtual Assistant

Item |Type | Description
--- | --- | ---
Number of users who have enabled or registered VA | User Activity | Assumption: The registration process contains a Waterfall dialog.  This will be the count of events for the WaterfallDialogConvert event
Number of Active (Engaged) VA users | User Activity | Distinct count of BotMessageReceived events on UserID
Number of Anonymous Users | User Activity | Distinct count of BotMessageReceived events on UserID minus Registered Users.
Number of new registerations | User Activity | Count of WaterfallDialogConvert events for the registeration dialog.
Average number of sessions per user | Engagment | BotMessageReceived : UserID by SessionID
Average time spent per session | Engagement |Based on  BotMessageReceived 
Average # of skills per person  | Engagement |Based on WaterfallDialogStep distinct on UserID
Average number of interactions per user | Engagement |Based on BotMessageReceived
Number of Skills (per Region/Language/etc) | Engagement | Based on BotMessageReceived
Number of abandoned/cancelled Skills (last N sessions) | Churn | Based on WaterfallDialogCancel
Common (high frequency) abandoned/cancelled skills | Churn | WaterfallDialogStep / WaterfallDialogCancel
Skills with high interaction acount | Churn | WaterfallDialogStep



# Conversational Metrics for Virtual Assistant

Item |Type | Description
--- | --- | ---
Most popular/least popular skills | Skills Insight | WaterfallDialogStep
Skills take a long time to complete | Skills Insight | WaterfallDialogStep / WaterfallDialogComplete
Average number of Interations per skill | Skills Insight | WaterfallDialogStep / WaterfallDialogComplete
Duration of each stage | Skills Insight | WaterfallDialogStep
Skills that take higher interactions | Skills Insight | WaterfallDialogStep
Count user OK with recommendations | Skills Insight | ? Custom Event
Missed / retried utterance | Skills Insight | LuisIntent.INENTName 
Skill successfully handled by IPA | Skills Insight | WaterfallDialogConvert
Skill transferred to Agent | Skills Insight | ? Custom Event
Number of times Skill Completed/Cancelled/Abandoned | Skills Insight | WaterfallDialogCancel / WaterfallDialogComplete / WaterfallDialogStep
Cancel / Abandon Correlation: Duration | Skills Insight | WaterfallDialogStep
Cancel / Abandon Correlation: Too many messages | Skills Insight | WaterfallDialogStep 
Cancel / Abandon Correlation: Retry | Skills Insight | WaterfallDialogStep
Cancel / Abandon Correlation: Not happy | Skills Insight | Sentiment LuisIntent.INENTName


# Machine Learning for Virtual Assistant
***PRI2 Not in scope for 4.2** 

Item |Type | Description
--- | --- | ---
Identify user unsubscribe | Churn | ? Custom Event for Unsubscribe?  Otherwise look-alike model
Identify user abandon | Churn | ? Assume abandon defined by duration.  Look-alike model
Identify low consumption | Churn | Look-alike model
User preference | Recommendations | ? LUIS entities / intents? Look-alike? Need more information 
Buy new product in future | Sales/Marketing | LUIS entities / Qna?