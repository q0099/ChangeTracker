using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChangeTracking
{
    public class ChangesArguments
    {
        internal ChangesArguments(object item, IEnumerable<PropertyInfo> properties)
        {
            Item = item;

            DirtyPropertiesSource = properties.ToList();

            DirtyProperties = properties.ToList();

            Cancel = false;
        }

        public object Item { get; private set; }

        public IReadOnlyList<PropertyInfo> DirtyPropertiesSource { get; private set; }

        public IList<PropertyInfo> DirtyProperties { get; private set; }

        public bool Cancel { get; set; }
    }
}
