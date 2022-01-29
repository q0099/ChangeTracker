using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking.PropertyStorage
{
    class PropertyStorageOfCollection<TCollection, TValue> : PropertyStorage<TCollection>
    {
        private const string _errorNotImplementing = "Can't store values of property {0}: the collection is not implementing IList or ICollection<T> interface.";
        private const string _errorNoInstance = "Can't restore values of property {0}: no instance of collection found.";
        private const string _errorReadOnly = "Can't restore values of property {0}: the collection is read only.";
        private const string _errorFixedSize = "Can't restore values of property {0}: the collection has fixed size which doesn't match to number of stored values.";

        private static readonly Func<Dictionary<TValue, int>, TValue, Dictionary<TValue, int>> _countUniqueElements = new Func<Dictionary<TValue, int>, TValue, Dictionary<TValue, int>>((context, item) => { if (!context.ContainsKey(item)) { context.Add(item, 0); } context[item] += 1; return context; });

        private readonly IEqualityComparer<TValue> _collectionValuescomparer;
        private readonly bool _keepOrder;
        private readonly bool _storeCollection;

        private List<TValue> _collectionValues;

        public PropertyStorageOfCollection(TCollection item, PropertyInfo propertyInfo, IEqualityComparer<TValue> comparer, bool keepOrder, bool storeCollection) : base(item, propertyInfo, null)
        {
            _collectionValuescomparer = comparer;
            _keepOrder = keepOrder;
            _storeCollection = storeCollection;
        }

        public override void AcceptValue()
        {
            if (_storeCollection)
                base.AcceptValue();

            var currentValue = PropertyInfo.GetValue(Item);

            if (currentValue == null)
            {
                _collectionValues = null;
            }
            else
            {
                if (currentValue is IList list)
                {
                    _collectionValues = list.Cast<TValue>().ToList();
                }
                else if (currentValue is ICollection<TValue> collection)
                {
                    _collectionValues = collection.ToList();
                }
                else
                {
                    throw new InvalidOperationException(string.Format(_errorNotImplementing, PropertyInfo.Name));
                }
            }
        }

        public override void RestoreValue()
        {
            if (_storeCollection)
                base.RestoreValue();

            var currentValue = PropertyInfo.GetValue(Item);

            if (currentValue == null)
            {
                throw new InvalidOperationException(string.Format(_errorNoInstance, PropertyInfo.Name));
            }

            if (currentValue is IList list)
            {
                if (list.IsReadOnly)
                    throw new InvalidOperationException(string.Format(_errorReadOnly, PropertyInfo.Name));

                if (list.IsFixedSize)
                {
                    if (list.Count != _collectionValues.Count)
                        throw new InvalidOperationException(string.Format(_errorFixedSize, PropertyInfo.Name));

                    var i = 0;
                    foreach (var value in _collectionValues)
                        list[i++] = value;
                }
                else
                {
                    list.Clear();

                    foreach (var value in _collectionValues)
                        list.Add(value);
                }
            }
            else if (currentValue is ICollection<TValue> collection)
            {
                if (collection.IsReadOnly)
                    throw new InvalidOperationException(string.Format(_errorReadOnly, PropertyInfo.Name));

                collection.Clear();

                foreach (var value in _collectionValues)
                    collection.Add(value);
            }
            else
            {
                throw new InvalidOperationException(string.Format(_errorNotImplementing, PropertyInfo.Name));
            }

            PropertyInfo.SetValue(Item, currentValue);
        }

        public override bool IsDirty()
        {
            if (_storeCollection && base.IsDirty())
                return true;

            var currentValue = PropertyInfo.GetValue(Item);

            if (currentValue == null)
            {
                return _collectionValues != null;
            }
            else
            {
                var currentCollection = ((IEnumerable)currentValue).Cast<TValue>();

                if (_keepOrder)
                {
                    return !_collectionValues.SequenceEqual(currentCollection, _collectionValuescomparer);
                }
                else
                {
                    if (_collectionValues.Count() != currentCollection.Count())
                        return true;

                    var storedUniqueValues = _collectionValues.Aggregate(new Dictionary<TValue, int>(), _countUniqueElements);
                    var currentUniqueValues = currentCollection.Aggregate(new Dictionary<TValue, int>(), _countUniqueElements);

                    if (storedUniqueValues.Count != currentUniqueValues.Count)
                        return true;

                    var joinedUniqueValues = storedUniqueValues
                        .Join(
                            currentUniqueValues,
                            a => a.Key,
                            b => b.Key,
                            (a, b) => new { valueA = a.Value, valueB = b.Value })
                        .ToList();

                    if (storedUniqueValues.Count != joinedUniqueValues.Count)
                        return true;

                    return joinedUniqueValues
                        .Any(n => n.valueA != n.valueB);
                }
            }
        }
    }
}
