using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a domain rule or invariant is violated. Used instead of ArgumentException or InvalidOperationException so the Application and API layers can catch and translate it into a meaningful error response (e.g. HTTP 400) without exposing internal implementation details.
    /// </summary>

    public class DomainValidationException : Exception
    {
        public string PropertyName { get; }

        public DomainValidationException(string propertyName, string message)
            : base(message)
        {
            PropertyName = propertyName;
        }
    }
}
