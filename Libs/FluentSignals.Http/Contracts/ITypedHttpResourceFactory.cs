using System;
using FluentSignals.Http.Options;

namespace FluentSignals.Http.Contracts
{
    /// <summary>
    /// Factory interface for creating typed HTTP resources
    /// </summary>
    /// <typeparam name="TResource">The type of resource to create</typeparam>
    public interface ITypedHttpResourceFactory<TResource> where TResource : class, ITypedHttpResource
    {
        /// <summary>
        /// Creates an instance of the typed HTTP resource
        /// </summary>
        TResource Create();
        
        /// <summary>
        /// Creates an instance of the typed HTTP resource with custom options
        /// </summary>
        /// <param name="configure">Action to configure the HTTP resource options</param>
        TResource Create(Action<HttpResourceOptions> configure);
    }
}