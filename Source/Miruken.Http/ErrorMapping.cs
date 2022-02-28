// ReSharper disable UnusedMember.Global
namespace Miruken.Http;

using System;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using Api;
using Callback;
using Map;
using Microsoft.AspNetCore.Http;
using Validate;
using FluentValidationException = FluentValidation.ValidationException;

public class ErrorMapping : Handler
{
    [Maps]
    public int MapStatusCode(ArgumentException exception) => StatusCodes.Status400BadRequest;

    [Maps]
    public int MapStatusCode(NotImplementedException exception) => StatusCodes.Status405MethodNotAllowed;

    [Maps]
    public int MapStatusCode(SecurityException exception) => StatusCodes.Status401Unauthorized;

    [Maps]
    public int MapStatusCode(AuthenticationException exception) => StatusCodes.Status401Unauthorized;

    [Maps]
    public int MapStatusCode(UnauthorizedAccessException exception) => StatusCodes.Status403Forbidden;

    [Maps]
    public int MapStatusCode(NotSupportedException exception) => StatusCodes.Status501NotImplemented;
        
    [Maps]
    public int MapStatusCode(NotFoundException exception) => StatusCodes.Status404NotFound;
        
    [Maps]
    public int MapStatusCode(ValidationException exception) => StatusCodes.Status422UnprocessableEntity;

    [Maps]
    public int MapStatusCode(FluentValidationException exception) =>  StatusCodes.Status422UnprocessableEntity;
        
    [Maps, Format(typeof(Exception))]
    public MultipleErrorsData MapAggregateException(
        AggregateException exception, IHandler composer)
    {
        return new MultipleErrorsData
        {
            Errors = exception.InnerExceptions
                .Select(ex => composer.Map<object>(ex, typeof(Exception)))
                .ToArray()
        };
    }

    [Maps, Format(typeof(Exception))]
    public AggregateException MapMultipleErrorsData(
        MultipleErrorsData errors, IHandler composer)
    {
        return new AggregateException(errors?.Errors
            .Select(error => composer.Map<Exception>(error, typeof(Exception)))
             ?? Array.Empty<Exception>());
    }

    [Maps, Format(typeof(Exception))]
    public ExceptionData MapException(Exception exception) => new(exception);

    [Maps, Format(typeof(Exception))]
    public Exception MapExceptionData(ExceptionData data)
    {
        var type = Type.GetType(data.ExceptionType);
        if (type == null) return null;
        var constructor = type.GetConstructor(new [] { typeof(string) });
        if (constructor == null) return null;
        var exception = (Exception)constructor.Invoke(new object[] {data.Message});
        exception.HelpLink = data.HelpLink;
        exception.Source   = data.Source;
        return exception;
    }
}