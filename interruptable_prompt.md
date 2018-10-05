# Interruptable Prompt


## Summary
Cafe and Enterprise Bot  are handling interruptions in similar but differen ways.  An example of an interruption is "I would like help" or "I want to cancel".  Both solutions use LUIS/QnA (Dispatch) to detect interruptions.  This proposes a new prompt that integrates LUIS/QnA models for interruption support.  In addition, this proposes a flexible data collection for model improvement, model measurement and custom reports.

Note:
Enterprise Bot handles interruptions at a dialog level.  Cafe handled at prompt level.  This approach is taking the most granular approach using prompts.

## Proposal
Below is a proposal of how a prompt (and future dialog) could be defined using a data definition. 

Assumptions/Notes:

- Data collection (Telemetry) is performed using Application Insights.
- Prompts use LUIS/QnA/Dispatch to detect interruptions.
- Entities recognized will be returned back to underlying caller (dialog). 
- "Training" mode could help validate and enrich the model at runtime.
- Matching Entities enable automatic LUIS entity detection for prompt and enables updates for other entities.

Below  is how a prompt ("username_prompt") might be defined. 

```json5
 {
  "name": "username_prompt",
  "prompt": "What is your name?",
  // type : [ string | int ]
  "type": "string",

  // run_mode [training | dev | none]
  "run_mode": "none",

  // Dispatch/QnA/LUIS first class with prompt.
  "model": {
    "name": "getUserProfile",
    "type": "luis",
    "description": "Common model",
    "matching_entities": [ "userName_patternAny", "userName" ]
  },

  // Data collection at prompt.
  "telemetry": [
    {
      "custom_event_name": "custom1",
      "fields": [
        "Activity.LocalTimestamp",
        "Activity.Id as activityid",
        "Activity.Text as msg",
        "RecognizerResult.Intents as intents"
      ]
    },
    {
      "custom_event_name": "top_intent_activity",
      "properties": [
        "Activity.LocalTimestamp",
        "Activity.Id",
        "Prompt.Value"
      ]
    }
  ]
}
```
And could orchestrate into a dialog which could facilitate data collection across prompts:

```json5
{
    "version": "0.1",
    // FUTURE  - Data Driven Dialog
    "name": "greetings_dialog",
        // Data Driven Prompt
        "prompts" : [
            {
                "username_prompt" : {
                    "prompt" : "What is your name?",
                    "type" : "string",
                    // FUTURE - Pre/post hook scripts.
                    "prerecognize" : "<.csx/.js/.python reference>",
                    "postrecognize" : "<.csx/.js/.python> reference",
                    // runmode [Training | Dev | None]
                    "runmode" : "training",
                    // Dispatch/QnA/LUIS first class with prompt.
                    "model": [
                        {
                            "usernameDispatcher" : {
                                "type":"luis",
                                "description": "Common model",
                                "location" : "<model file>"
                                }
                        }
                    ],
                    // Data collection first class with prompt.
                    "data_capture" : [
                        {
                            "custom_event_name": "custom1",
                            "fields": ["activity.ActivityId", "luis.usernameDispatcher.Topintent", "luis.usernameDispatcher.Entities"]
                        },
                        { 
                            "custom_event_name": "top_intent_activity",
                            "fields": ["activity.ActivityId", "prompt.Value", "common.datetime"]
                        }
                    ]
                }
            }
        ],
        // FUTURE: Override all prompts
        "run_mode" : "confirm",
        // FUTURE: Data capture across all prompts
        "data_capture" : [
            {
                "custom_event_name": "across_prompt_data",
                "fields": ["username_prompt.luis.usernameDispatcher.Topintent", "username_prompt.luis.usernameDispatcher.Entities"]
            }
        ]
    }
}
```

# User Stories
We may be able to cover a few of these stories.

## Developer Stories
- As a sdk user, I want to be able to handle cancel  in my prompts in order to provide a good customer experience.
- As a sdk user, I want to be able to handle help  in my prompts and provide prompt-specific guidance in order to provide a good customer experience.
- As a sdk user, I want to be able to handle arbitrary chit chat in my prompts in order to provide a good customer experience.
- As a sdk user, I want to be able to handle other classes of responses (aggressive behavior, questioning validity of prompt, etc) in my prompts in order to provide a good customer experience.
- As a sdk user, I want to be able to integrate language models to assist in interpreting if the user is interrupting normal flow  in order to provide a good customer experience.
- As a sdk user, I want reports or feedback in order to understand if my language models are working.
- As a sdk user, I want the ability to the ability to  provide customer training data per prompt into the language understanding models  in order to increase accuracy. 
- As a sdk user, I want a curating process to curate per prompt user training data into the language understanding models in order to reject faulty data.
- As a sdk user, I want the ability to the language understanding models to update themselves in a timely manner so the customer experience improves with low latency.
- As a analyst, I want the ability modify the way the data is stored in order to construct the correct queries for reports.

## Business Stories
- As a analyst, I want to be able to correlate all my data together within a turn.
- As a analyst, I want to be able to create dialog level data together easily in order to construct dialog-level reports.
- As a analyst, I want to be able to understand where in dialog flow customers are not responding in order to understand my customer behavior and modify my prompts.
- As a analyst, I want the ability to see raw customer data, in order to understand what customers are seeking within the bot.
