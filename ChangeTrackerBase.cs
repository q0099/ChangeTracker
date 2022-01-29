using ChangeTracking.Attributes;
using ChangeTracking.PropertyStorage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public abstract class ChangeTrackerBase<T> : ICollection<T> where T : class
    {
        private static readonly Guid _iListGuid = typeof(IList).GUID;
        private static readonly Guid _genericIListGuid = typeof(IList<>).GUID;
        private static readonly Guid _genericICollectionGuid = typeof(ICollection<>).GUID;

        private static readonly Guid[] _expectedInterfaceGuids = new[] { _iListGuid, _genericIListGuid, _genericICollectionGuid };

        private readonly IDictionary<T, IList<PropertyStorageBase>> _items;

        public static IEnumerable<PropertyInfo> TrackableProperties(Type type) =>
            type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetMethod.IsPublic && p.SetMethod.IsPublic && p.GetCustomAttribute<ChangeTrackerIgnoreAttribute>() == null);

        #region ICollection<T>

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public ChangeTrackerBase()
        {
            _items = new Dictionary<T, IList<PropertyStorageBase>>();
        }

        public abstract void Add(T item);

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                Add(item);
        }

        public void Clear() => _items.Clear();

        public bool Contains(T item) => _items.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentOutOfRangeException();

            foreach (var item in _items.Keys)
            {
                array[arrayIndex++] = item;
            }
        }

        public IEnumerator<T> GetEnumerator() => _items.Keys.GetEnumerator();

        public bool Remove(T item) => _items.Remove(item);

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        #endregion

        public void AcceptChanges(T item) => AcceptChangesInner(Enumerable.Repeat(item, 1));

        public void AcceptChanges(IEnumerable<T> items) => AcceptChangesInner(items);

        public void AcceptChanges() => AcceptChangesInner(_items.Keys);

        public void RejectChanges(T item) => RejectChangesInner(Enumerable.Repeat(item, 1));

        public void RejectChanges(IEnumerable<T> items) => RejectChangesInner(items);

        public void RejectChanges() => RejectChangesInner(_items.Keys);

        public bool IsDirty(T item) => DirtyPropertiesInner(item).Any();

        public IList<PropertyInfo> GetDirtyProperties(T item) => DirtyPropertiesInner(item).Select(p => p.PropertyInfo).ToList();

        public IEnumerable<Tuple<T, IList<PropertyInfo>>> DirtyItmes
        {
            get
            {
                foreach (var item in _items.Keys)
                {
                    var dirtyProperties = DirtyPropertiesInner(item).ToList();

                    if (dirtyProperties.Count > 0)
                    {
                        yield return Tuple.Create(item, (IList<PropertyInfo>)dirtyProperties.Select(p => p.PropertyInfo).ToList());
                    }
                }
            }
        }

        #region events

        public delegate void ChangesHandler(object sender, ChangesArguments args);

        public event ChangesHandler OnAcceptChanges;

        public event ChangesHandler OnRejectChanges;

        #endregion

        #region protected

        protected void AddInner(T item, IList<PropertyInfo> trackingProperties, IDictionary<PropertyInfo, IEqualityComparer> comparers)
        {
            var propertyStorageList = new List<PropertyStorageBase>();

            _items.Add(item, propertyStorageList);

            foreach (var propertyInfo in trackingProperties)
            {
                IEqualityComparer comparer = null;

                if (comparers != null && comparers.TryGetValue(propertyInfo, out var matchedComparer))
                {
                    comparer = matchedComparer;
                }

                var isCollectionAttribute = propertyInfo.GetCustomAttribute<ChangeTrackerCollectionAttribute>();

                if (isCollectionAttribute != null)
                {
                    IEnumerable<Type> interfaces = propertyInfo.PropertyType.GetInterfaces();

                    if (propertyInfo.PropertyType.IsInterface)
                        interfaces = interfaces.Append(propertyInfo.PropertyType);

                    if (interfaces.All(i => !_expectedInterfaceGuids.Contains(i.GUID)))
                        throw new InvalidOperationException($"Type {propertyInfo.PropertyType} of property {propertyInfo.Name} is not supported as trackable collection.");

                    var keepOrder = interfaces.Any(i => i.GUID.Equals(_iListGuid) || i.GUID.Equals(_genericIListGuid));

                    Type type;
                    if (propertyInfo.PropertyType is IList)
                    {
                        type = typeof(object);
                    }
                    else
                    {
                        var iCollectionInterface = interfaces.First(i => i.GUID.Equals(_genericICollectionGuid));

                        type = iCollectionInterface.GenericTypeArguments[0];
                    }

                    propertyStorageList.Add((PropertyStorageBase)Activator.CreateInstance(typeof(PropertyStorageOfCollection<,>).MakeGenericType(typeof(object), type), item, propertyInfo, comparer, keepOrder, isCollectionAttribute.StoreCollection));
                }
                else
                {
                    propertyStorageList.Add((PropertyStorageBase)Activator.CreateInstance(typeof(PropertyStorage<>).MakeGenericType(propertyInfo.PropertyType), item, propertyInfo, comparer));
                }
            }

            foreach (var p in propertyStorageList)
            {
                p.AcceptValue();
            }
        }

        #endregion

        #region private

        private void AcceptChangesInner(IEnumerable<T> items) => ProcessItems(items, s => s.AcceptValue(), OnAcceptChanges);

        private void RejectChangesInner(IEnumerable<T> items) => ProcessItems(items, s => s.RestoreValue(), OnRejectChanges);

        private void ProcessItems(IEnumerable<T> items, Action<PropertyStorageBase> action, ChangesHandler eventHandler)
        {
            foreach (var item in items)
            {
                var dirtyProperties = DirtyPropertiesInner(item).ToList();

                if (dirtyProperties.Count > 0)
                {
                    IEnumerable<PropertyStorageBase> properties;

                    if (eventHandler != null)
                    {
                        var args = new ChangesArguments(item, dirtyProperties.Select(p => p.PropertyInfo).ToList());

                        eventHandler?.Invoke(this, args);

                        if (!args.Cancel && args.DirtyProperties.Count > 0)
                        {
                            properties = dirtyProperties.Where(p => args.DirtyProperties.Contains(p.PropertyInfo));
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        properties = dirtyProperties;
                    }

                    foreach (var property in properties)
                    {
                        action(property);
                    }
                }
            }
        }

        private IEnumerable<PropertyStorageBase> DirtyPropertiesInner(T item)
        {
            foreach (var property in _items[item])
            {
                if (property.IsDirty())
                    yield return property;
            }
        }

        #endregion
    }
}
