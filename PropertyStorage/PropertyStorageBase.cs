using System.Reflection;

namespace ChangeTracking.PropertyStorage
{
    abstract class PropertyStorageBase
    {
        public object Item { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public PropertyStorageBase(object item, PropertyInfo propertyInfo)
        {
            Item = item;
            PropertyInfo = propertyInfo;
        }

        public abstract void AcceptValue();

        public abstract void RestoreValue();

        public abstract bool IsDirty();
    }
}
