using System;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Attribute to specify the base URL for a typed HTTP resource
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HttpResourceAttribute : Attribute
    {
        /// <summary>
        /// Gets the base URL for the HTTP resource
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// Initializes a new instance of the HttpResourceAttribute class
        /// </summary>
        /// <param name="baseUrl">The base URL for the resource (e.g., "/api/users")</param>
        public HttpResourceAttribute(string baseUrl)
        {
            BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        }
    }
}