// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { EventHubClient } = require('@azure/event-hubs');
const util = require('util');
/**
 * BatchingEventHubSender, takes in an activity and then writes it to the Azure EventHub in batches.
 * Round robins between partitions.
 */
class BatchingEventHubSender {
    /**
     * @param {string} connectionString Azure Eventhub Connection String.  ie, Endpoint=sb://*.servicebus.windows.net/;SharedAccessKeyName=*;SharedAccessKey=*
     * @param {string} entityPath Azure Eventhub name or entity path.
     * @param {string} batchSize The number of events to batch before they are pushed to the EventHub.
     * @param {string} batchIntervalMs The number of milliseconds to batch before events are pushed to EventHub.
     */
    constructor(connectionString, entityPath, batchSize = null, batchIntervalMs = null) {
        this._client = EventHubClient.createFromConnectionString(connectionString, entityPath);
        this._cache = [];
        this._batchSize = batchSize || process.env.HubsBatchSize || 15;
        this._partition_index = 0;
        this._partitionIds = null;
        this._batchIntervalMs = batchIntervalMs || process.env.HubsBatchIntervalMs || 9500;
    }

    async send(activity) {
        if (this._partitionIds === null) {
            this._partitionIds = await this._client.getPartitionIds();
        }
    
        if (activity.conversation) {
            var date = new Date();
            var newEvent = {body: { 
                "message":  activity.text,
                "timestamp": date.toISOString(), 
                "channelId": activity.channelId,
                "type": activity.type,
                "transcript": activity
            }};

            this._cache.push(newEvent);
        
            if (this._cache.length >= this._batchSize)
            {
                await this.triggerSend();
                return;
            }

            // ensure an invocation timeout is set if anything is in the buffer
            if (!this._timeoutHandle && this._cache.length > 0) {
                this._timeoutHandle = setTimeout(async () => {
                    this._timeoutHandle = null;
                    await this.triggerSend();
                }, this._batchIntervalMs);
            }            
        }
    }

    /**
     * Immediately send buffered data
     */
    async triggerSend() {
        let bufferIsEmpty = this._cache.length < 1;
        if (!bufferIsEmpty) {
            // invoke send
            const partition = this._partition_index;
            const batch = Array.from(this._cache);
            this._partition_index = (this._partition_index + 1) % this._partitionIds.length;

            await this._client.sendBatch(batch).then(() => {
                console.warn('Batch sent');
            })
        }

        // update lastSend time to enable throttling
        this._lastSend = +new Date;

        // clear buffer
        this._cache.length = 0
        clearTimeout(this._timeoutHandle);
        this._timeoutHandle = null;
    }    

}

exports.BatchingEventHubSender = BatchingEventHubSender;
