namespace FluentSignals.Http.Types
{
    /// <summary>
    /// Represents an HTTP error response
    /// </summary>
    public class HttpError
    {
        /// <summary>
        /// The HTTP status code
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional error details
        /// </summary>
        public object? Details { get; set; }
        
        /// <summary>
        /// The raw response content
        /// </summary>
        public string? RawContent { get; set; }
    }
}