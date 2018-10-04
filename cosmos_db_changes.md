# Cosmos DB support Bot Framework Storage Changes

## Summary
Currently, the Bot Framework v4.0 has a Cosmos DB storage provider that enables customers to store state.  This proposes changes to our implementation around tuning and scalability.  This does not address the overall bot contract with storage. 

## Background
For reference, this is what we currently store within CosmosDB.
```csharp
        /// <summary>
        /// Internal for storing items in a CosmosDB Collection.
        /// </summary>
        private class DocumentStoreItem
        {
            /// <summary>
            /// Gets or sets the sanitized Id/Key used as PrimaryKey.
            /// </summary>
            [JsonProperty("id")]
            public string Id { get; set; }

            /// <summary>
            /// Gets or sets the un-sanitized Id/Key.
            /// </summary>
            [JsonProperty("realId")]
            public string ReadlId { get; internal set; }

            /// <summary>
            /// Gets or sets the persisted object.
            /// </summary>
            [JsonProperty("document")]
            public JObject Document { get; set; }

            /// <summary>
            /// Gets or sets the ETag information for handling optimistic concurrency updates.
            /// </summary>
            [JsonProperty("_etag")]
            public string ETag { get; set; }
        }
```
We ship two common Storage providers:

- UserState

- ConversationState

In addition, customers can add custom storage providers.

Each of the state providers store the same payload, the only difference is the key they use.

```csharp
 // (Rougly)  The key for User State 
 string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId;
            var userId = turnContext.Activity.From?.Id;
            return $"{channelId}/users/{userId}";
        }
 // (Roughly) The key for Conversation State
 string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId;
            var conversationId = turnContext.Activity.Conversation?.Id;
            return $"{channelId}/conversations/{conversationId}";
        }
```

## Indexing
Our query usage is very key-value oriented.  A user begins a conversation, and retrieves a UserState and/or ConversationState.  There are no scenarios where we query patterns within the Document payload.
- **Exclude (/document/*)**
Exclude the entire Document path from being index.  Customers with query patterns within Document can override with a custom State provider.
- **Index precision: -1**
Keep consistent index precision.
## Consistency
- **Session**
CosmosDB have devised some preset models that predefine system behavior to address CAP theorem tradeoffs.  Session level consistency matches our pattern, where primarily a client that reads and writes on the same thread guarantees consistency, with relatively lower guarantees across region.  It does maintain order.  
## Partitioning
We can define a partioning key based on the storage key.  This will result in a single row per key, but apparently doesn't present a problem for Cosmos.
## Database/Collections
The customer is in control of how many databases.  We can  recommend a single database for all state providers.  Each state provider will have a single colletion, which is a 10Gb limit.
## TTL (Stretch)
This is something that Vishwac is interested in, Cosmos supports aging out data.  We would most likely want more expressive SDK primitives across all languages (ie, attributes).
## Permissions
We will employ a single resource key for all users to perform read/write storage operations.
## Multi-tenancy considerations
We are using Multiple Users <=>Single Collection model.  Customers can deploy different models if they so desire.


