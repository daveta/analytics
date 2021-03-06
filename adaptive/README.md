# Adaptive Dialog Telemetry
**DRAFT DRAFT**

## Summary 
This is a **dirty read** attempt around collecting telemetry for Adaptive Dialog.

**Scenario Set A**: Enable Business User
- Parity with existing Waterfall Telemetry

**Scenario Set B**: Enable Developer
- Provide events on what's happening inside Adaptive wormhole

## Background
- [4.6 Dashboards](https://github.com/daveta/analytics/tree/master/dashboards)
- [4.2 Waterfall Events](https://github.com/daveta/analytics/blob/master/appinsights_blog_42/bot_application_insights.md#waterfalldialog-events)
- [4.4 Telemetry](https://github.com/daveta/analytics/blob/master/telemetry_enhancements/TelemetryEnhancements.md)
- [Adaptive Docs/Samples](https://github.com/microsoft/BotBuilder-Samples/tree/master/experimental/adaptive-dialog)

## Scenario A: Enable Business User

### Baseline
Get roughly analogous events to `WaterfallStart`, `WaterfallStep`, `WaterfallComplete` and `WaterfallCancel`.  Conceptually, Waterfall Dialog telemetry was about collecting enough information from the bot user to fulfill a business objective (ie, ask for a quote).  The events were designed to track that progress.  

  > **Note**: There are probably better places to collect the data, but not familiar enough with Adaptive to know how it works.

  
EventName |Properties | Description
--- | --- | ---
`AdaptiveStart`| user_id, session_id, activity_id, activity_type, channel_id, dialog_id, dialog_id (?), instance_id (?) | Called when a new instance of the sequence has been pushed onto the stack and is being activated (`SequenceStarted`).  Alternative could be logged as BeginDialog in the Dialog base class. 
`AdaptiveStep` | compute_id, user_id, session_id, activity_id, activity_type, channel_id, dialog_id, dialog_id (?), instance_id (?) | Called when the "sequence" step is executing (?) - `RunCommand`.  I believe a step within a Adaptive dialog can invoke a Waterfall, so need to figure out how all the ID's work to track across entire sequence.
`AdaptiveComplete` | user_id, session_id, activity_id, activity_type, channel_id, dialog_id, dialog_id (?), instance_id (?) |  Called when the "sequence" is ending (`SequenceEnded`).  Possibly performed in the Planner class.
`AdaptiveCancel` | user_id, session_id, activity_id, activity_type, channel_id, dialog_id, dialog_id (?), instance_id (?) | Specific type of Adaptive Event (`CancelDialog`). At this point, it seems to me that dialogs and sequences are comingled based on interruptions/other. I'm assuming you can infer parent sequence.



## Scenario B: Enable Developer


### DialogCommand
These appear to be composer primitives.

EventName |Properties | Description
--- | --- | ---
CancelAllDialogs | ?? | ??
CodeStep | ?? | ??
DeleteProperty/SetProperty | ?? | ??
EditArray | ?? | ??
EditSteps | ?? | ??
EmitEvent | ?? | ??
EndDialog | ?? | ??
ReplaceDialog | ?? | ???
PropertySet/Delete | ?? | ??

### Sequence
Seems to be the thread of execution for a formerly known as Dialog Steps.

EventName |Properties | Description
--- | --- | ---
SequenceUpdate | StepChangeList properties including StepChangeType | Includes appending steps, insertStepsBeforeTags
SequenceReplace | StepState | ??
SequenceInsert | InsertSteps, InsertStepsBeforeTags | ??
SequenceFlush | ApplyChanges | ??

### Base Dialog
If we want all-up instrumentation of all dialogs.

EventName |Properties | Description
--- | --- | ---
BeginDialog| DialogContext Props, Options? | Called when a new instance of the dialog has been pushed onto the stack and is being activated.
ContinueDialog | DialogContext properties | Called to continue execution of a multi-turn dialog.
ResumeDialog | DialogContext props, DialogReason props, result? | Called when an instance of the dialog is being returned to from another dialog.
EndDialog | TurnContext props, DialogInstance props, DialogReason | Called when the dialog is ending.
PreBubbleEvent | DialogContext Props, DialogEvent props | Intercept of an event returning 'True" will prevent further bubbling to dialog parents and prevent children default processing. 
PostBubbleEvent |DialogContext Props, DialogEvent props | Intercept of an event returning 'True" will prevent any processing of the event by child dialogs.
RePromptDialog | TurnContext props, DialogInstance props |  multi-turn dialogs that wish to provide custom re-prompt logic
OnDialogevent | DialogContext props, DialogEvent props | The dialog is responsible for bubbling the event up to its parent dialog.

