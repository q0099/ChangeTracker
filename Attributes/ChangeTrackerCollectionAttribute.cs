using System;

namespace ChangeTracking.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ChangeTrackerCollectionAttribute : Attribute
    {
        public bool StoreCollection { get; private set; }

        /// <param name="storeCollectionInstance">If true, at restore tracker will restore an instance of collection followed by value restoration. If false, tracker will try to restore values in current collection, which will cause an exception if current collection is null.</param>
        public ChangeTrackerCollectionAttribute(bool storeCollectionInstance = true)
        {
            StoreCollection = storeCollectionInstance;
        }
    }
}
