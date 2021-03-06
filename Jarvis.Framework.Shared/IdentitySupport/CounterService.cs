﻿using System;
using Jarvis.Framework.Shared.MultitenantSupport;
using MongoDB.Driver;

namespace Jarvis.Framework.Shared.IdentitySupport
{
    public class CounterService : ICounterService
    {
        readonly IMongoCollection<IdentityCounter> _counters;

        public class IdentityCounter
        {
            public string Id { get; set; }
            public long Last { get; set; }
        }

        public CounterService(IMongoDatabase db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _counters = db.GetCollection<IdentityCounter>("sysCounters");
        }

        public long GetNext(string serie)
        {
            Int32 count = 0;
            while (count++ < 5)
            {
                try
                {
                    var counter = _counters.FindOneAndUpdate(
                                    Builders<IdentityCounter>.Filter.Eq(x => x.Id, serie),
                                    Builders<IdentityCounter>.Update.Inc(x => x.Last, 1),
                                    new FindOneAndUpdateOptions<IdentityCounter, IdentityCounter>()
                                    {
                                        ReturnDocument = ReturnDocument.After,
                                        IsUpsert = true,
                                    });
                    return counter.Last;
                }
                catch (MongoCommandException ex)
                {
                    //We can tolerate only duplicate exception
                    if (!ex.Message.Contains("E11000"))
                        throw;
                }
            }

            throw new ApplicationException("Unable to generate next number for serie " + serie);
        }
    }

    public class InMemoryCounterService : ICounterService
    {
        long _last = 0;

        public long GetNext(string serie)
        {
            return ++_last;
        }
    }

    public class MultitenantCounterService : ICounterService
    {
        readonly ITenantAccessor _tenantAccessor;

        public MultitenantCounterService(ITenantAccessor tenantAccessor)
        {
            _tenantAccessor = tenantAccessor;
        }

        public long GetNext(string serie)
        {
            return _tenantAccessor.Current.CounterService.GetNext(serie);
        }
    }
}