using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangeTracker<T> : ChangeTrackerBase<T> where T : class
    {
        private readonly IList<PropertyInfo> _trackingProperties;
        private readonly IDictionary<PropertyInfo, IEqualityComparer> _comparers;

        public ChangeTracker(IList<PropertyInfo> properties = null, IDictionary<PropertyInfo, IEqualityComparer> comparers = null) : base()
        {
            _trackingProperties = (properties ?? TrackableProperties(typeof(T)))
                .ToList();

            _comparers = comparers?
                .Where(kv => _trackingProperties.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<PropertyInfo, IEqualityComparer>();
        }

        public override void Add(T item) => AddInner(item, _trackingProperties, _comparers);

        public IEnumerable<PropertyInfo> TrackingProperties()
        {
            foreach (var propertyInfo in _trackingProperties)
                yield return propertyInfo;
        }
    }
}
