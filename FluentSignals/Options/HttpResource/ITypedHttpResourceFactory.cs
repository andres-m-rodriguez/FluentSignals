namespace FluentSignals.Options.HttpResource
{
    /// <summary>
    /// Factory interface for creating typed HTTP resources
    /// </summary>
    /// <typeparam name="TResource">The type of the HTTP resource to create</typeparam>
    public interface ITypedHttpResourceFactory<TResource> where TResource : TypedHttpResource
    {
        /// <summary>
        /// Creates an instance of the typed HTTP resource
        /// </summary>
        TResource Create();
        
        /// <summary>
        /// Creates an instance of the typed HTTP resource with custom options
        /// </summary>
        TResource Create(Action<HttpResourceOptions> configure);
    }
}