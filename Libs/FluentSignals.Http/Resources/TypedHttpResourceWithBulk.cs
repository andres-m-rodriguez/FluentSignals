using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentSignals.Http.Options;
using FluentSignals.Http.Types;

namespace FluentSignals.Http.Resources
{
    /// <summary>
    /// Base class for typed HTTP resources with bulk operation support
    /// </summary>
    public abstract class TypedHttpResourceWithBulk : TypedHttpResource
    {
        protected TypedHttpResourceWithBulk()
        {
        }

        protected TypedHttpResourceWithBulk(HttpClient httpClient, string baseUrl)
            : base(httpClient, baseUrl)
        {
        }

        protected TypedHttpResourceWithBulk(HttpClient httpClient, string baseUrl, HttpResourceOptions options)
            : base(httpClient, baseUrl, options)
        {
        }

        /// <summary>
        /// Creates a bulk operation request that will be processed in batches
        /// </summary>
        protected HttpResourceRequest<List<TResult>> BulkOperation<TItem, TResult>(
            string endpoint,
            IEnumerable<TItem> items,
            int batchSize = 100)
        {
            var itemsList = items.ToList();
            
            return BuildRequest<List<TResult>>(endpoint)
                .WithMethod(HttpMethod.Post)
                .WithBody(new BulkRequest<TItem> { Items = itemsList, BatchSize = batchSize })
                .Build();
        }

        /// <summary>
        /// Executes a bulk operation with progress reporting
        /// </summary>
        protected async Task<BulkResult<TResult>> ExecuteBulkAsync<TItem, TResult>(
            string endpoint,
            IEnumerable<TItem> items,
            int batchSize = 100,
            Action<BulkProgress>? onProgress = null)
        {
            var allResults = new List<TResult>();
            var errors = new List<BulkError>();
            var batches = items.Chunk(batchSize).ToList();
            var totalItems = batches.Sum(b => b.Count());
            var processedItems = 0;
            
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var batchItems = batch.ToList();
                
                try
                {
                    var resource = await Post<List<TItem>, List<TResult>>(endpoint, batchItems).ExecuteAsync();
                    
                    if (resource.Value is HttpResponse<List<TResult>> typedResponse && typedResponse.Data is List<TResult> results)
                    {
                        allResults.AddRange(results);
                        processedItems += batchItems.Count;
                    }
                    else if (resource.Error.Value != null)
                    {
                        errors.Add(new BulkError
                        {
                            BatchIndex = i,
                            Items = batchItems.Cast<object>().ToList(),
                            Error = resource.Error.Value
                        });
                        processedItems += batchItems.Count;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new BulkError
                    {
                        BatchIndex = i,
                        Items = batchItems.Cast<object>().ToList(),
                        Error = ex
                    });
                    processedItems += batchItems.Count;
                }
                
                // Report progress
                onProgress?.Invoke(new BulkProgress
                {
                    ProcessedItems = processedItems,
                    TotalItems = totalItems,
                    CurrentBatch = i + 1,
                    TotalBatches = batches.Count,
                    PercentComplete = (double)processedItems / totalItems * 100
                });
            }
            
            return new BulkResult<TResult>
            {
                Results = allResults,
                Errors = errors,
                TotalProcessed = processedItems,
                SuccessCount = allResults.Count,
                ErrorCount = errors.Sum(e => e.Items.Count)
            };
        }

        /// <summary>
        /// Executes parallel bulk operations for better performance
        /// </summary>
        protected async Task<BulkResult<TResult>> ExecuteBulkParallelAsync<TItem, TResult>(
            string endpoint,
            IEnumerable<TItem> items,
            int batchSize = 100,
            int maxParallelism = 4,
            Action<BulkProgress>? onProgress = null)
        {
            var allResults = new List<TResult>();
            var errors = new List<BulkError>();
            var batches = items.Chunk(batchSize).ToList();
            var totalItems = batches.Sum(b => b.Count());
            var processedItems = 0;
            var progressLock = new object();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallelism
            };

            await Parallel.ForEachAsync(batches.Select((batch, index) => (batch, index)), parallelOptions, async (item, ct) =>
            {
                var (batch, batchIndex) = item;
                var batchItems = batch.ToList();

                try
                {
                    var resource = await Post<List<TItem>, List<TResult>>(endpoint, batchItems).ExecuteAsync();

                    if (resource.Value is HttpResponse<List<TResult>> typedResponse && typedResponse.Data is List<TResult> results)
                    {
                        lock (progressLock)
                        {
                            allResults.AddRange(results);
                            processedItems += batchItems.Count;
                        }
                    }
                    else if (resource.Error.Value != null)
                    {
                        lock (progressLock)
                        {
                            errors.Add(new BulkError
                            {
                                BatchIndex = batchIndex,
                                Items = batchItems.Cast<object>().ToList(),
                                Error = resource.Error.Value
                            });
                            processedItems += batchItems.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (progressLock)
                    {
                        errors.Add(new BulkError
                        {
                            BatchIndex = batchIndex,
                            Items = batchItems.Cast<object>().ToList(),
                            Error = ex
                        });
                        processedItems += batchItems.Count;
                    }
                }

                // Report progress
                if (onProgress != null)
                {
                    lock (progressLock)
                    {
                        onProgress(new BulkProgress
                        {
                            ProcessedItems = processedItems,
                            TotalItems = totalItems,
                            CurrentBatch = processedItems / batchSize,
                            TotalBatches = batches.Count,
                            PercentComplete = (double)processedItems / totalItems * 100
                        });
                    }
                }
            });

            return new BulkResult<TResult>
            {
                Results = allResults,
                Errors = errors,
                TotalProcessed = processedItems,
                SuccessCount = allResults.Count,
                ErrorCount = errors.Sum(e => e.Items.Count)
            };
        }
    }

    /// <summary>
    /// Represents a bulk request
    /// </summary>
    public class BulkRequest<T>
    {
        public List<T> Items { get; set; } = new();
        public int BatchSize { get; set; }
    }

    /// <summary>
    /// Represents the result of a bulk operation
    /// </summary>
    public class BulkResult<T>
    {
        public List<T> Results { get; set; } = new();
        public List<BulkError> Errors { get; set; } = new();
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public bool HasErrors => Errors.Count > 0;
        public double SuccessRate => TotalProcessed > 0 ? (double)SuccessCount / TotalProcessed * 100 : 0;
    }

    /// <summary>
    /// Represents an error during bulk processing
    /// </summary>
    public class BulkError
    {
        public int BatchIndex { get; set; }
        public List<object> Items { get; set; } = new();
        public Exception Error { get; set; } = null!;
    }

    /// <summary>
    /// Represents progress during bulk operations
    /// </summary>
    public class BulkProgress
    {
        public int ProcessedItems { get; set; }
        public int TotalItems { get; set; }
        public int CurrentBatch { get; set; }
        public int TotalBatches { get; set; }
        public double PercentComplete { get; set; }
    }
}