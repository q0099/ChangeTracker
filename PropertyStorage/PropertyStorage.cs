using System.Collections;
using System.Reflection;

namespace ChangeTracking.PropertyStorage
{
    class PropertyStorage<T> : PropertyStorageBase
    {
        private T _value;
        private readonly IEqualityComparer _comparer;

        public PropertyStorage() : base(null, null)
        {
        }

        public PropertyStorage(object item, PropertyInfo propertyInfo, IEqualityComparer comparer) : base(item, propertyInfo)
        {
            _comparer = comparer;
        }

        public override void AcceptValue()
        {
            _value = (T)PropertyInfo.GetValue(Item);
        }

        public override void RestoreValue()
        {
            PropertyInfo.SetValue(Item, _value);
        }

        public override bool IsDirty()
        {
            var newValue = (T)PropertyInfo.GetValue(Item);

            return _comparer != null
                ? !_comparer.Equals(_value, newValue)
                : !_value?.Equals(newValue) ?? !newValue?.Equals(_value) ?? false;
        }
    }
}
