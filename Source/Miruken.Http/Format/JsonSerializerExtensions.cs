namespace Miruken.Http.Format;

using Newtonsoft.Json;

public static class JsonSerializerExtensions
{   
    public static JsonSerializer Clone(this JsonSerializer serializer)
    {
        var copy = new JsonSerializer
        {
            Context                        = serializer.Context,
            Culture                        = serializer.Culture,
            ContractResolver               = serializer.ContractResolver,
            ConstructorHandling            = serializer.ConstructorHandling,
            CheckAdditionalContent         = serializer.CheckAdditionalContent,
            DateFormatHandling             = serializer.DateFormatHandling,
            DateFormatString               = serializer.DateFormatString,
            DateParseHandling              = serializer.DateParseHandling,
            DateTimeZoneHandling           = serializer.DateTimeZoneHandling,
            DefaultValueHandling           = serializer.DefaultValueHandling,
            EqualityComparer               = serializer.EqualityComparer,
            FloatFormatHandling            = serializer.FloatFormatHandling,
            Formatting                     = serializer.Formatting,
            FloatParseHandling             = serializer.FloatParseHandling,
            MaxDepth                       = serializer.MaxDepth,
            MetadataPropertyHandling       = serializer.MetadataPropertyHandling,
            MissingMemberHandling          = serializer.MissingMemberHandling,
            NullValueHandling              = serializer.NullValueHandling,
            ObjectCreationHandling         = serializer.ObjectCreationHandling,
            PreserveReferencesHandling     = serializer.PreserveReferencesHandling,
            ReferenceResolver              = serializer.ReferenceResolver,
            ReferenceLoopHandling          = serializer.ReferenceLoopHandling,
            SerializationBinder            = serializer.SerializationBinder,
            StringEscapeHandling           = serializer.StringEscapeHandling,
            TraceWriter                    = serializer.TraceWriter,
            TypeNameHandling               = serializer.TypeNameHandling,
            TypeNameAssemblyFormatHandling = serializer.TypeNameAssemblyFormatHandling
        };
        foreach (var converter in serializer.Converters)
            copy.Converters.Add(converter);
        return copy;
    }
}