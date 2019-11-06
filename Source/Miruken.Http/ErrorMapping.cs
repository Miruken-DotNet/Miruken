namespace Miruken.Http
{
    using System;
    using System.Linq;
    using System.Security;
    using System.Security.Authentication;
    using Api;
    using Callback;
    using Map;
    using Validate;
    using FluentValidationException = FluentValidation.ValidationException;

#if NETSTANDARD
    using Microsoft.AspNetCore.Http;
#else
    using System.Data;
    using System.Net;
#endif

    public class ErrorMapping : Handler
    {
        [Maps]
        public int MapStatusCode(ArgumentException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status400BadRequest;
#else
            return (int)HttpStatusCode.BadRequest;
#endif
        }

        [Maps]
        public int MapStatusCode(NotImplementedException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status405MethodNotAllowed;
#else
            return (int)HttpStatusCode.MethodNotAllowed;
#endif
        }

        [Maps]
        public int MapStatusCode(SecurityException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status401Unauthorized;
#else
            return (int)HttpStatusCode.Unauthorized;
#endif
        }

        [Maps]
        public int MapStatusCode(AuthenticationException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status401Unauthorized;
#else
            return (int)HttpStatusCode.Unauthorized;
#endif
        }

        [Maps]
        public int MapStatusCode(UnauthorizedAccessException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status403Forbidden;
#else
            return (int)HttpStatusCode.Forbidden;
#endif
        }

        [Maps]
        public int MapStatusCode(NotSupportedException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status501NotImplemented;
#else
            return (int)HttpStatusCode.NotImplemented;
#endif
        }

#if NETFULL
        [Maps]
        public int MapStatusCode(OptimisticConcurrencyException exception)
        {
            return (int)HttpStatusCode.Conflict;
        }
#endif

        [Maps]
        public int MapStatusCode(NotFoundException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status404NotFound;
#else
            return (int)HttpStatusCode.NotFound;
#endif
        }

        [Maps]
        public int MapStatusCode(ValidationException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status422UnprocessableEntity;
#else
            return HttpStatusCodeEx.UnprocessableEntityCode;
#endif
        }

        [Maps]
        public int MapStatusCode(FluentValidationException exception)
        {
#if NETSTANDARD
            return StatusCodes.Status422UnprocessableEntity;
#else
            return HttpStatusCodeEx.UnprocessableEntityCode;
#endif
        }

        [Maps, Format(typeof(Exception))]
        public string MapNotFoundException(NotFoundException exception)
        {
            return exception.Message;
        }

        [Maps, Format(typeof(Exception))]
        public MultipleErrors MapAggregateException(
            AggregateException exception, IHandler composer)
        {
            return new MultipleErrors
            {
                Errors = exception.InnerExceptions
                    .Select(ex => composer.Map<object>(ex, typeof(Exception)))
                    .ToArray()
            };
        }

        [Maps, Format(typeof(Exception))]
        public AggregateException MapMultipleErrors(
            MultipleErrors errors, IHandler composer)
        {
            return new AggregateException(errors?.Errors
                .Select(error => composer.Map<Exception>(error, typeof(Exception)))
                ?? Array.Empty<Exception>());
        }

        [Maps, Format(typeof(Exception))]
        public ExceptionData MapException(Exception exception)
        {
            return new ExceptionData(exception);
        }

        [Maps, Format(typeof(Exception))]
        public Exception MapExceptionData(ExceptionData data)
        {
            var type = Type.GetType(data.ExceptionType);
            if (type == null) return null;
            var constructor = type.GetConstructor(new [] { typeof(string) });
            if (constructor == null) return null;
            var exception = (Exception)constructor.Invoke(new[] {data.Message});
            exception.HelpLink = data.HelpLink;
            exception.Source   = data.Source;
            return exception;
        }
    }
}
