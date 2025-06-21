using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Extension methods for TypedHttpResource to add common functionality
    /// </summary>
    public static class TypedHttpResourceExtensions
    {
        /// <summary>
        /// Adds cancellation token support to any request
        /// </summary>
        public static HttpResourceRequest<T> WithCancellation<T>(
            this HttpResourceRequest<T> request, 
            CancellationToken cancellationToken)
        {
            return request.ConfigureRequest(req => 
            {
                // Store token in request properties for later use
                req.Options.Set(new HttpRequestOptionsKey<CancellationToken>("CancellationToken"), cancellationToken);
            });
        }

        /// <summary>
        /// Adds cancellation token support to non-generic requests
        /// </summary>
        public static HttpResourceRequest WithCancellation(
            this HttpResourceRequest request, 
            CancellationToken cancellationToken)
        {
            return request.ConfigureRequest(req => 
            {
                // Store token in request properties for later use
                req.Options.Set(new HttpRequestOptionsKey<CancellationToken>("CancellationToken"), cancellationToken);
            });
        }

        /// <summary>
        /// Adds pagination support to requests
        /// </summary>
        public static HttpResourceRequest<PagedResult<T>> WithPaging<T>(
            this HttpResourceRequest<PagedResult<T>> request,
            PagedRequest<T> paging)
        {
            return request
                .WithQueryParam("page", paging.Page.ToString())
                .WithQueryParam("pageSize", paging.PageSize.ToString())
                .WithQueryParam("sortBy", paging.SortBy ?? "")
                .WithQueryParam("sortDesc", paging.SortDescending.ToString())
                .WithQueryParams(paging.Filters);
        }

        /// <summary>
        /// Adds pagination support with simplified parameters
        /// </summary>
        public static HttpResourceRequest<PagedResult<T>> WithPaging<T>(
            this HttpResourceRequest<PagedResult<T>> request,
            int page,
            int pageSize,
            string? sortBy = null,
            bool sortDescending = false)
        {
            return request.WithPaging(new PagedRequest<T>
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            });
        }

        /// <summary>
        /// Adds retry configuration to a request
        /// </summary>
        public static HttpResourceRequest<T> WithRetry<T>(
            this HttpResourceRequest<T> request,
            int maxAttempts,
            TimeSpan? delay = null)
        {
            // Since we can't modify the HttpResource options after creation,
            // we'll store retry configuration in the request for later use
            return request.ConfigureRequest(req =>
            {
                req.Options.Set(new HttpRequestOptionsKey<int>("MaxRetryAttempts"), maxAttempts);
                req.Options.Set(new HttpRequestOptionsKey<TimeSpan>("RetryDelay"), delay ?? TimeSpan.FromSeconds(1));
            });
        }

        /// <summary>
        /// Adds timeout configuration to a request
        /// </summary>
        public static HttpResourceRequest<T> WithTimeout<T>(
            this HttpResourceRequest<T> request,
            TimeSpan timeout)
        {
            return request.ConfigureRequest(req =>
            {
                req.Options.Set(new HttpRequestOptionsKey<TimeSpan>("Timeout"), timeout);
            });
        }

        /// <summary>
        /// Adds bearer token authentication to a request
        /// </summary>
        public static HttpResourceRequest<T> WithBearerToken<T>(
            this HttpResourceRequest<T> request,
            string token)
        {
            return request.WithHeader("Authorization", $"Bearer {token}");
        }

        /// <summary>
        /// Adds bearer token authentication to a non-generic request
        /// </summary>
        public static HttpResourceRequest WithBearerToken(
            this HttpResourceRequest request,
            string token)
        {
            return request.WithHeader("Authorization", $"Bearer {token}");
        }
    }

    /// <summary>
    /// Represents a paginated request
    /// </summary>
    public class PagedRequest<T>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public Dictionary<string, string> Filters { get; set; } = new();
    }

    /// <summary>
    /// Represents a paginated result
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}