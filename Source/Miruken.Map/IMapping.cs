namespace Miruken.Map
{
    public interface IMapping
    {
        object MapFrom(object source, object format);

        object MapTo(object formattedValue, object format,
                     object typeOrInstance);
    }
}
