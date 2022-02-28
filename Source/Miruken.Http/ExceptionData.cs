namespace Miruken.Http;

using System;

public class ExceptionData
{
    public ExceptionData()
    {          
    }

    public ExceptionData(Exception exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        ExceptionType = exception.GetType().AssemblyQualifiedName;
        HelpLink      = exception.HelpLink;
        Message       = exception.Message;
        Source        = exception.Source;
    }

    public string ExceptionType { get; set; }
    public string HelpLink      { get; set; }
    public string Message       { get; set; }
    public string Source        { get; set; }
}