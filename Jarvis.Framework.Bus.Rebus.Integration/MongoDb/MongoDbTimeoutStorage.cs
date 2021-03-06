﻿using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using Rebus;
using Rebus.Timeout;
using Castle.Core.Logging;

namespace Jarvis.Framework.Bus.Rebus.Integration.MongoDb
{
    /// <summary>
    /// Implementation of <see cref="IStoreTimeouts"/> that stores timeouts in a MongoDB
    /// </summary>
    public class MongoDbTimeoutStorage : IStoreTimeouts
    {
        const string ReplyToProperty = "reply_to";
        const string CorrIdProperty = "corr_id";
        const string TimeProperty = "time";
        const string SagaIdProperty = "saga_id";
        const string DataProperty = "data";
        const string IdProperty = "_id";
        readonly IMongoCollection<BsonDocument> collection;
        readonly ILogger logger;

        /// <summary>
        /// Constructs the timeout storage, connecting to the Mongo database pointed to by the given connection string,
        /// storing the timeouts in the given collection
        /// </summary>
        public MongoDbTimeoutStorage(string connectionString, string collectionName, ILogger logger)
        {
            var database = MongoHelper.GetDatabase(connectionString);
            collection = database.GetCollection<BsonDocument>(collectionName);
            collection.Indexes.CreateOne(
                Builders<BsonDocument>.IndexKeys.Ascending(TimeProperty),
                new CreateIndexOptions()
                {
                    Background = true,
                    Unique = false,
                });
            this.logger = logger;
        }

        /// <summary>
        /// Adds the timeout to the underlying collection of timeouts
        /// </summary>
        public void Add(global::Rebus.Timeout.Timeout newTimeout)
        {
            var doc = new BsonDocument()
                .Add(CorrIdProperty, newTimeout.CorrelationId)
                .Add(SagaIdProperty, newTimeout.SagaId)
                .Add(TimeProperty, newTimeout.TimeToReturn)
                .Add(DataProperty, newTimeout.CustomData)
                .Add(ReplyToProperty, newTimeout.ReplyTo);

            collection.InsertOne(doc);
        }

        /// <summary>
        /// Gets all timeouts that are due by now. Doesn't remove the timeouts or change them or anything,
        /// each individual timeout can be marked as processed by calling <see cref="DueTimeout.MarkAsProcessed"/>
        /// </summary>
        public IEnumerable<DueTimeout> GetDueTimeouts()
        {
            var result = collection.Find(Builders<BsonDocument>.Filter.Lte(TimeProperty, RebusTimeMachine.Now()))
                                   .Sort(Builders<BsonDocument>.Sort.Ascending(TimeProperty));

            return result
                .ToCursor()
                .ToEnumerable()
                .Select(r => new DueMongoTimeout(r[ReplyToProperty].AsString,
                                                 GetString(r, CorrIdProperty),
                                                 r[TimeProperty].ToUniversalTime(),
                                                 GetGuid(r, SagaIdProperty),
                                                 GetString(r, DataProperty),
                                                 collection,
                                                 (ObjectId) r[IdProperty]));
        }

        static Guid GetGuid(BsonDocument doc, string propertyName)
        {
            return doc.Contains(propertyName) ? doc[propertyName].AsGuid : Guid.Empty;
        }

        static string GetString(BsonDocument doc, string propertyName)
        {
            return doc.Contains(propertyName) ? doc[propertyName].AsString : "";
        }

        class DueMongoTimeout : DueTimeout
        {
            readonly IMongoCollection<BsonDocument> collection;
            readonly ObjectId objectId;

            public DueMongoTimeout(string replyTo, string correlationId, DateTime timeToReturn, Guid sagaId, string customData, IMongoCollection<BsonDocument> collection, ObjectId objectId) 
                : base(replyTo, correlationId, timeToReturn, sagaId, customData)
            {
                this.collection = collection;
                this.objectId = objectId;
            }

            public override void MarkAsProcessed()
            {
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq(IdProperty, objectId));
            }
        }
    }
}