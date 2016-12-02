using System;
using System.Collections.Generic;
using System.Linq;

namespace Miruken.Validation
{
    public class ValidationException : Exception
    {
        public ValidationException(string message)
            : this(message, null)
        {

        }

        public ValidationException(string message, ValidationFailure[] errors)
            : base(message)
        {
            Errors = errors;
        }

        public ValidationException(ValidationFailure[] errors)
            : base(BuildErrorMesage(errors = errors ?? new ValidationFailure[0]))
        {
            Errors = errors;
        }

        public ValidationFailure[] Errors { get; private set; }

        private static string BuildErrorMesage(IEnumerable<ValidationFailure> errors)
        {
            var arr = errors.Select(x => "\r\n -- " + x.ErrorMessage).ToArray();
            return "Validation failed: " + string.Join("", arr);
        }
    }
}
