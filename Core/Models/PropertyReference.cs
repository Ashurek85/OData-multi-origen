namespace Core.Models
{
    public class PropertyReference<TProperty>
    {
        public TProperty Value { get; }

        public bool IsShadowReference { get; }

        private PropertyReference(TProperty value, bool isShadowReference)
        {
            Value = value;
            IsShadowReference = isShadowReference;
        }

        public static PropertyReference<TProperty> Build(TProperty value)
        {
            return new PropertyReference<TProperty>(value, false);
        }

        public static PropertyReference<TProperty> BuildShadowReference(TProperty value)
        {
            return new PropertyReference<TProperty>(value, true);
        }
    }
}
