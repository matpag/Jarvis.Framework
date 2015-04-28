using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using System.Threading;

namespace Jarvis.Framework.Kernel.ProjectionEngine
{
    public class CachedMongoStorage2<TModel, TKey> : IMongoStorage<TModel, TKey> where TModel : class, IReadModelEx<TKey>
    {
        readonly MongoStorage<TModel, TKey> _storage;

        public CachedMongoStorage2(MongoCollection<TModel> collection)
        {
            _storage = new MongoStorage<TModel, TKey>(collection);
        }

        public bool IndexExists(IMongoIndexKeys keys)
        {
            return _storage.IndexExists(keys);
        }

        #region Cache

        private readonly IDictionary<TKey, TModel> _cache = new Dictionary<TKey, TModel>();
        private readonly HashSet<TKey> _cacheDeleted = new HashSet<TKey>();

        public Boolean IsCacheActive { get; private set; }
        //when collection is empty we can batch insert.
        private Boolean _isCollectionEmpty;

        private void AddInCache(TKey id, TModel value)
        {
            _cache[id] = value;
            _cacheDeleted.Remove(id);
        }

        //  private class CacheEntry<TModel>
        //{
        //    public TModel Entry { get; set; }

        //    public CacheStatus Status { get; set; }

        //    public CacheEntry<TModel>(TModel entry)
        //    {
        //        Entry = entry;
        //        Status = CacheStatus.Inserted;
        //    }
        //}

        //private enum CacheStatus
        //{ 
        //    Inserted = 0,
        //    Deleted = 1
        //}
        #endregion

        void FlushBeforeMongoAccess()
        {
            if (IsCacheActive)
                Flush();
        }

        public void CreateIndex(IMongoIndexKeys keys, IMongoIndexOptions options = null)
        {
            _storage.CreateIndex(keys, options);
        }

        public void InsertBatch(IEnumerable<TModel> values)
        {
            if (IsCacheActive)
            {
                foreach (var value in values)
                {
                    AddInCache(value.Id, value);
                }
            }
            else
            {
                _storage.InsertBatch(values);
            }
        }

        public IQueryable<TModel> All
        {
            get
            {
                return IsCacheActive ? _cache.Values.AsQueryable() : _storage.All;
            }
        }

        public TModel FindOneById(TKey id)
        {
            if (IsCacheActive)
            {
                if (_cache.ContainsKey(id))
                    return _cache[id];

                var storageData = _storage.FindOneById(id);
                AddInCache(id, storageData);
                return storageData;
            }
            else
            {
                return _storage.FindOneById(id);
            }
        }

        public IQueryable<TModel> Where(Expression<Func<TModel, bool>> filter)
        {
            return IsCacheActive ?
                _cache.Values.AsQueryable().Where(filter).ToArray().AsQueryable() :
                _storage.Where(filter);
        }

        public bool Contains(Expression<Func<TModel, bool>> filter)
        {
            return IsCacheActive ?
                _cache.Values.AsQueryable().Any(filter) :
                _storage.Contains(filter);
        }

        public InsertResult Insert(TModel model)
        {
            if (IsCacheActive)
            {
                try
                {
                    _cache.Add(model.Id, model);
                    return new InsertResult()
                    {
                        Ok = true
                    };
                }
                catch (Exception ex)
                {
                    return new InsertResult()
                    {
                        Ok = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
            return _storage.Insert(model);
        }

        public SaveResult SaveWithVersion(TModel model, int orignalVersion)
        {
            if (IsCacheActive)
            {
                // non posso controllare le versioni perch� l'istanza � la stessa
                AddInCache(model.Id, model);
                return new SaveResult { Ok = true };
            }

            return _storage.SaveWithVersion(model, orignalVersion);
        }

        public void Save(TModel model)
        {
            if (IsCacheActive)
            {
                // non posso controllare le versioni perch� l'istanza � la stessa
                AddInCache(model.Id, model);
            }
            else
            {
                _storage.Save(model);
            }

        }

        public DeleteResult Delete(TKey id)
        {
            if (IsCacheActive)
            {
                var removed = _cache.Remove(id);
                _cacheDeleted.Add(id);
                return new DeleteResult
                {
                    Ok = true,
                    DocumentsAffected = removed ? 1 : 0
                };
            }

            return _storage.Delete(id);
        }

        public void Drop()
        {
            _storage.Drop();
        }

        public MongoCollection<TModel> Collection
        {
            get
            {
                FlushBeforeMongoAccess();
                return _storage.Collection;
            }
        }
        public IEnumerable<BsonDocument> Aggregate(IEnumerable<BsonDocument> operations)
        {
            FlushBeforeMongoAccess();
            return _storage.Aggregate(operations);
        }

        public IEnumerable<BsonDocument> Aggregate(AggregateArgs aggregateArgs)
        {
            FlushBeforeMongoAccess();
            return _storage.Aggregate(aggregateArgs);
        }

        public void Flush()
        {
            if (IsCacheActive)
            {
                if (_cache.Any())
                {
                    if (_isCollectionEmpty)
                    {
                        _storage.InsertBatch(_cache.Values);
                    }
                    else
                    {
                        Parallel.ForEach(_cache.Values, e => _storage.Save(e));
                    }
                }
                if (_cacheDeleted.Any() && !_isCollectionEmpty)
                {
                    Parallel.ForEach(
                          _cacheDeleted,
                          key => _storage.Delete(key));
                }
                CheckCollectionEmpty();
            }
        }

        public void DisableCache()
        {
            Flush();
            _cache.Clear();
            _cacheDeleted.Clear();
            IsCacheActive = false;
        }

        public void EnableCache()
        {
            IsCacheActive = true;
            CheckCollectionEmpty();
        }

        private void CheckCollectionEmpty()
        {
            _isCollectionEmpty = _storage.All.Any() == false;
        }


    }


}