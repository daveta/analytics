// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { BatchingEventHubSender } = require('./batchingEventHubSender');
const { TurnContext } = require('botbuilder');
const botframework_schema = require("botframework-schema");
/**
 * EventHubTranscriptLogger, takes in an activity and then writes it to Event Hub.
 */
class EventHubTranscriptLogger {
    /**
     *
     * @param {string} connectionString 
     * @param {string} entityPath 
     */
    constructor(connectionString, entityPath) {
        this.batchSender = new BatchingEventHubSender(connectionString, entityPath);
    }
    /**
     * Initialization for middleware turn.
     * @param context Context for the current turn of conversation with the user.
     * @param next Function to call at the end of the middleware chain.
     */
    async onTurn(context, next) {
        // log incoming activity at beginning of turn
        if (context.activity) {
            if (!context.activity.from.role) {
                context.activity.from.role = 'user';
            }
            this.logActivity(this.cloneActivity(context.activity));
        }
        // hook up onSend pipeline
        context.onSendActivities(async (ctx, activities, next) => {
            // run full pipeline
            const responses = await next();
            activities.forEach((a) => this.logActivity(this.cloneActivity(a)));
            return responses;
        });
        // hook up update activity pipeline
        context.onUpdateActivity(async (ctx, activity, next) => {
            // run full pipeline
            const response = await next();
            // add Message Update activity
            const updateActivity = this.cloneActivity(activity);
            updateActivity.type = botframework_schema.ActivityTypes.MessageUpdate;
            this.logActivity(updateActivity);
            return response;
        });
        // hook up delete activity pipeline
        context.onDeleteActivity(async (ctx, reference, next) => {
            // run full pipeline
            await next();
            // add MessageDelete activity
            // log as MessageDelete activity
            const deleteActivity = TurnContext.applyConversationReference({
                type: botframework_schema.ActivityTypes.MessageDelete,
                id: reference.activityId
            }, reference, false);
            this.logActivity(deleteActivity);
        });
        // process bot logic
        await next();
    }


    /**
     * Log an activity to the log file.
     * @param activity Activity being logged.
     */
    async logActivity(activity) {
        if (!activity) {
            throw new Error('Activity is required.');
        }

        this.batchSender.send(activity);
    }

    /**
     * Clones the Activity entity.
     * @param activity Activity to clone.
     */
    cloneActivity(activity) {
        return Object.assign({}, activity);
    }
}
exports.EventHubTranscriptLogger = EventHubTranscriptLogger;
