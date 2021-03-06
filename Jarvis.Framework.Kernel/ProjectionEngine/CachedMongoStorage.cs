using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Jarvis.Framework.Shared.ReadModel;
using MongoDB.Bson;
using MongoDB.Driver;
using Jarvis.Framework.Shared.Helpers;
using Fasterflect;

namespace Jarvis.Framework.Kernel.ProjectionEngine
{
    public class CachedMongoStorage<TModel, TKey> : IMongoStorage<TModel, TKey> where TModel : class, IReadModelEx<TKey>
    {
        readonly IInmemoryCollection<TModel, TKey> _inmemoryCollection;
        readonly MongoStorage<TModel, TKey> _storage;
        public CachedMongoStorage(
                IMongoCollection<TModel> collection,
                IInmemoryCollection<TModel, TKey> inmemoryCollection
            )
        {
            _storage = new MongoStorage<TModel, TKey>(collection);
            _inmemoryCollection = inmemoryCollection;
            _indexes = new IndexCollection(_inmemoryCollection);
        }

        #region In Memory Indexing

        private class Index : Dictionary<Object, HashSet<TModel>>
        {
            String _propertyName;

            private Dictionary<TKey, HashSet<TModel>> _ownershipMap;

            public Index(String propertyName)
            {
                _propertyName = propertyName;
                _ownershipMap = new Dictionary<TKey, HashSet<TModel>>();
            }

            public void Set(TModel instance)
            {
                var propertyValue = instance.GetPropertyValue(_propertyName);
                EnsureCache(propertyValue);
                var belongingSet = propertyValue == null ? nullEntry : base[propertyValue];
                if (_ownershipMap.ContainsKey(instance.Id) &&
                    _ownershipMap[instance.Id] != belongingSet)
                {
                    _ownershipMap[instance.Id].Remove(instance);
                }
                belongingSet.Add(instance);
                _ownershipMap[instance.Id] = belongingSet;
            }

            public void RemoveInstance(TModel instance)
            {
                if (instance == null) return;
                var propertyValue = instance.GetPropertyValue(_propertyName);
                if (_ownershipMap.ContainsKey(instance.Id))
                {
                    _ownershipMap[instance.Id].Remove(instance);
                }
                else
                {
                    //we have no ownership map, remove from all cached instances.
                    foreach (var cache in Values)
                    {
                        cache.Remove(instance);
                    }
                }
            }

            HashSet<TModel> nullEntry;
            private void EnsureCache(object propertyValue)
            {
                if (propertyValue == null)
                {
                    if (nullEntry == null) nullEntry = new HashSet<TModel>();
                }
                else if (!base.ContainsKey(propertyValue))
                {
                    base[propertyValue] = new HashSet<TModel>();
                }
            }

            public IEnumerable<TModel> GetByPropertyValue(Object propertyValue)
            {
                EnsureCache(propertyValue);
                if (propertyValue == null) return nullEntry;
                return base[propertyValue];
            }
        }

        private class IndexCollection : Dictionary<String, Index>
        {
            IInmemoryCollection<TModel, TKey> _collection;
            public IndexCollection(IInmemoryCollection<TModel, TKey> collection)
            {
                _collection = collection;
        }

            public void Insert(TModel instance)
            {
                foreach (var index in base.Values)
                {
                    index.Set(instance);
                }
            }

            internal void RemoveInstance(TModel instance)
            {
                foreach (var index in base.Values)
                {
                    index.RemoveInstance(instance);
                }
            }

            public IEnumerable<TModel> GetByPropertyValue(String propertyName, Object propertyValue)
            {
                EnsureIndex(propertyName);
                return base[propertyName].GetByPropertyValue(propertyValue);
            }

            public void EnsureIndex(string propertyName)
            {
                if (!base.ContainsKey(propertyName))
                {
                    var index = new Index(propertyName);
                    base[propertyName] = index;
                    foreach (var element in _collection.GetAll())
                    {
                        index.Set(element);
                    }
                }
            }
        }

        private IndexCollection _indexes;

        #endregion

        public bool IndexExists(String name)
        {
            return _storage.IndexExists(name);
        }

        void ThrowIfOperatingInMemory()
        {
            if (_inmemoryCollection.IsActive)
                throw new Exception("Unsupported operation while operating in memory");
        }

        public void CreateIndex(String name, IndexKeysDefinition<TModel> keys, CreateIndexOptions options = null)
        {
            _storage.CreateIndex(name, keys, options);
        }

        public void InsertBatch(IEnumerable<TModel> values)
        {
            if (_inmemoryCollection.IsActive)
            {
                foreach (var value in values)
                {
                    _inmemoryCollection.Insert(value);
                    _indexes.Insert(value);
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
                return _inmemoryCollection.IsActive ? _inmemoryCollection.GetAll() : _storage.All;
            }
        }

        public TModel FindOneById(TKey id)
        {
            return _inmemoryCollection.IsActive ?
                _inmemoryCollection.GetById(id) :
                _storage.FindOneById(id);
        }

        public IEnumerable<TModel> FindManyByProperty<TValue>(
            Expression<Func<TModel, TValue>> propertySelector,
            TValue value)
        {
            if (_inmemoryCollection.IsActive)
            {
                var propertyName = propertySelector.GetMemberName();
                _indexes.EnsureIndex(propertyName);
                return _indexes.GetByPropertyValue(propertyName, value);
            }
            else
            {
               return _storage.FindManyByProperty(propertySelector, value);
            }
        }

        public IQueryable<TModel> Where(Expression<Func<TModel, bool>> filter)
        {
            return _inmemoryCollection.IsActive ?
                _inmemoryCollection.GetAll().Where(filter).ToArray().AsQueryable() :
                _storage.Where(filter);
        }

        public bool Contains(Expression<Func<TModel, bool>> filter)
        {
            return _inmemoryCollection.IsActive ?
                _inmemoryCollection.GetAll().Any(filter) :
                _storage.Contains(filter);
        }

        public InsertResult Insert(TModel model)
        {
            if (_inmemoryCollection.IsActive)
            {
                try
                {
                    _inmemoryCollection.Insert(model);
                    _indexes.Insert(model);
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
            if (_inmemoryCollection.IsActive)
            {
                // non posso controllare le versioni perch� l'istanza � la stessa
                _inmemoryCollection.Save(model);
                _indexes.Insert(model);
                return new SaveResult { Ok = true };
            }

            return _storage.SaveWithVersion(model, orignalVersion);
        }

        public DeleteResult Delete(TKey id)
        {
            if (_inmemoryCollection.IsActive)
            {
                var instance = _inmemoryCollection.GetById(id);
                _indexes.RemoveInstance(instance);
                return new DeleteResult
                {
                    Ok = true,
                    DocumentsAffected = _inmemoryCollection.Delete(id) ? 1 : 0
                };
            }

            return _storage.Delete(id);
        }

        public void Drop()
        {
            if (_inmemoryCollection.IsActive)
            {
                _inmemoryCollection.Clear();
                _indexes.Clear();
            }
            _storage.Drop();
        }

        public IMongoCollection<TModel> Collection {
            get
            {
                ThrowIfOperatingInMemory();
                return _storage.Collection;
            }
        }


        public void Flush()
        {
            if (_inmemoryCollection.IsActive)
            {
                var allModels = _inmemoryCollection.GetAll().ToArray();
                if(allModels.Any())
                    _storage.InsertBatch(allModels);

                _inmemoryCollection.Deactivate();
            }
        }
    }
}