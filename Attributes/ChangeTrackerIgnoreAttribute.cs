using System;

namespace ChangeTracking.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ChangeTrackerIgnoreAttribute : Attribute { }
}
