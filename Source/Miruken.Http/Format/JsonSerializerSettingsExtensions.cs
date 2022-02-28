namespace Miruken.Http.Format;

using Newtonsoft.Json;

public static class JsonSerializerSettingsExtensions
{
    public static JsonSerializerSettings Copy(
        this JsonSerializerSettings from, JsonSerializerSettings to = null)
    {
        to ??= new JsonSerializerSettings();
        to.Context                        = from.Context;
        to.Culture                        = from.Culture;
        to.ContractResolver               = from.ContractResolver;
        to.ConstructorHandling            = from.ConstructorHandling;
        to.CheckAdditionalContent         = from.CheckAdditionalContent;
        to.DateFormatHandling             = from.DateFormatHandling;
        to.DateFormatString               = from.DateFormatString;
        to.DateParseHandling              = from.DateParseHandling;
        to.DateTimeZoneHandling           = from.DateTimeZoneHandling;
        to.DefaultValueHandling           = from.DefaultValueHandling;
        to.EqualityComparer               = from.EqualityComparer;
        to.FloatFormatHandling            = from.FloatFormatHandling;
        to.Formatting                     = from.Formatting;
        to.FloatParseHandling             = from.FloatParseHandling;
        to.MaxDepth                       = from.MaxDepth;
        to.MetadataPropertyHandling       = from.MetadataPropertyHandling;
        to.MissingMemberHandling          = from.MissingMemberHandling;
        to.NullValueHandling              = from.NullValueHandling;
        to.ObjectCreationHandling         = from.ObjectCreationHandling;
        to.PreserveReferencesHandling     = from.PreserveReferencesHandling;
        to.ReferenceResolverProvider      = from.ReferenceResolverProvider;
        to.ReferenceLoopHandling          = from.ReferenceLoopHandling;
        to.SerializationBinder            = from.SerializationBinder;
        to.StringEscapeHandling           = from.StringEscapeHandling;
        to.TraceWriter                    = from.TraceWriter;
        to.TypeNameHandling               = from.TypeNameHandling;
        to.TypeNameAssemblyFormatHandling = from.TypeNameAssemblyFormatHandling;
        foreach (var converter in from.Converters)
            to.Converters.Add(converter);
        return to;
    }
}