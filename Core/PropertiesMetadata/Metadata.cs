using System;
using System.Reflection;

namespace Core.PropertiesMetadata
{
    public class Metadata<T> : MetadataBase
        where T : class
    {

        public override Type UnderlyingType => typeof(T);

        public Metadata()
            : base(typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
        }
    }
}
