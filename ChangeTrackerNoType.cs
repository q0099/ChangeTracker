using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangeTracker : ChangeTrackerBase<object>
    {
        public void Add(object item, IList<PropertyInfo> properties = null, IDictionary<PropertyInfo, IEqualityComparer> comparers = null)
            => AddInner(item, properties, comparers);

        public override void Add(object item) => Add(item, null, null);
    }
}
