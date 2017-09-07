namespace Miruken.Map
{
    public interface IMapping
    {
        object MapFrom(object source, object format);

        object MapTo(object formattedValue, object format,
                     object typeOrInstance);
    }

    public static class MappingExtensions
    {
        public static T MapFrom<T>(this IMapping mapping, object source)
        {
            return (T)mapping.MapFrom(source, typeof(T));
        }
    }
}
