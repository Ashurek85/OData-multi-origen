using System;
using System.Reflection;
using System.Linq;

namespace Core.PropertiesMetadata
{
    public abstract class MetadataBase
    {
        private readonly PropertyInfo[] properties;

        public abstract Type UnderlyingType { get; }

        protected MetadataBase(PropertyInfo[] properties)
        {
            this.properties = properties;
        }

        public PropertyInfo GetProperty(string propertyName)
        {
            return properties.FirstOrDefault(f => f.Name == propertyName);
        }
    }
}
