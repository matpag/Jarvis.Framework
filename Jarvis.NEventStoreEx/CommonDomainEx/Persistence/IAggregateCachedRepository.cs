﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis.NEventStoreEx.CommonDomainEx.Persistence
{
    /// <summary>
    /// This is a really reduced repository that wraps access to a single entity, it is useful
    /// because it can be cached for a single aggregate.
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public interface IAggregateCachedRepository<TAggregate> : IDisposable where TAggregate : class, IAggregateEx
    {
        TAggregate Aggregate { get; }

        void Save(Guid commitId, Action<IDictionary<string, object>> updateHeaders);
    }

    /// <summary>
    /// Abstract the factory for a repository with single entity, needed to use cache.
    /// </summary>
    public interface IAggregateCachedRepositoryFactory
    {
        /// <summary>
        /// It is IMPORTANT to notice that the default implementation of this method
        /// can return a single aggregate repository with an entity in cache, so it is
        /// IMPORTANT that you prevent multiple thread to ask and use for the very same
        /// aggregateId.
        /// </summary>
        /// <typeparam name="TAggregate"></typeparam>
        /// <param name="id">The id of the aggregate, remember to Lock to prevent
        /// multiple threads to use the same aggregate concurrently.</param>
        /// <returns></returns>
        IAggregateCachedRepository<TAggregate> Create<TAggregate>(IIdentity id) where TAggregate : class, IAggregateEx;
    }
}
