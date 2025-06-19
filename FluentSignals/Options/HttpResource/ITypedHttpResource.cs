namespace FluentSignals.Options.HttpResource
{
    /// <summary>
    /// Marker interface for typed HTTP resources
    /// </summary>
    public interface ITypedHttpResource
    {
        /// <summary>
        /// Gets the base URL for this resource
        /// </summary>
        string BaseUrl { get; }
    }
}