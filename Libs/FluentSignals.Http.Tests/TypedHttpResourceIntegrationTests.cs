using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentSignals.Blazor.Extensions;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Types;
using Xunit;

namespace FluentSignals.Http.Tests
{
    public class TypedHttpResourceIntegrationTests
    {
        private class TestApiHandler : HttpMessageHandler
        {
            private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _routes;

            public TestApiHandler()
            {
                _routes = new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>();
                SetupRoutes();
            }

            private void SetupRoutes()
            {
                // GET /api/products
                _routes["GET:/api/products"] = req =>
                {
                    var products = new List<Product>
                    {
                        new Product { Id = 1, Name = "Product 1", Price = 10.99m },
                        new Product { Id = 2, Name = "Product 2", Price = 20.99m }
                    };
                    return JsonResponse(products);
                };

                // GET /api/products/{id}
                _routes["GET:/api/products/1"] = req => 
                    JsonResponse(new Product { Id = 1, Name = "Product 1", Price = 10.99m });

                // POST /api/products
                _routes["POST:/api/products"] = req =>
                {
                    var content = req.Content?.ReadAsStringAsync().Result;
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var product = JsonSerializer.Deserialize<Product>(content ?? "{}", options);
                    product!.Id = 3;
                    return JsonResponse(product, HttpStatusCode.Created);
                };

                // PUT /api/products/{id}
                _routes["PUT:/api/products/1"] = req =>
                {
                    var content = req.Content?.ReadAsStringAsync().Result;
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var product = JsonSerializer.Deserialize<Product>(content ?? "{}", options);
                    return JsonResponse(product);
                };

                // DELETE /api/products/{id}
                _routes["DELETE:/api/products/1"] = req => 
                    new HttpResponseMessage(HttpStatusCode.NoContent);

                // POST /api/products/search
                _routes["POST:/api/products/search"] = req =>
                {
                    var query = req.RequestUri?.Query ?? "";
                    var results = new SearchResult<Product>
                    {
                        Items = new List<Product> 
                        { 
                            new Product { Id = 1, Name = "Search Result", Price = 15.99m } 
                        },
                        TotalCount = 1,
                        Query = query
                    };
                    return JsonResponse(results);
                };

                // Custom MERGE endpoint
                _routes["MERGE:/api/products/merge"] = req =>
                {
                    return JsonResponse(new MergeResult { Success = true, MergedId = 99 });
                };

                // Error scenarios
                _routes["GET:/api/products/notfound"] = req => 
                    new HttpResponseMessage(HttpStatusCode.NotFound);
                
                _routes["GET:/api/products/unauthorized"] = req => 
                    new HttpResponseMessage(HttpStatusCode.Unauthorized);
                
                _routes["GET:/api/products/error"] = req => 
                    new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var key = $"{request.Method}:{request.RequestUri?.AbsolutePath}";
                
                if (_routes.TryGetValue(key, out var handler))
                {
                    return Task.FromResult(handler(request));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private HttpResponseMessage JsonResponse<T>(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(data),
                        Encoding.UTF8,
                        "application/json")
                };
            }
        }

        private class Product
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
        }

        private class SearchCriteria
        {
            public string Query { get; set; } = "";
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
        }

        private class SearchResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public string Query { get; set; } = "";
        }

        private class MergeResult
        {
            public bool Success { get; set; }
            public int MergedId { get; set; }
        }

        [HttpResource("/api/products")]
        private class ProductResource : TypedHttpResource
        {
            public ProductResource() { }
            
            public ProductResource(HttpClient httpClient) 
                : base(httpClient, "/api/products") { }

            public HttpResourceRequest<List<Product>> GetAll() => 
                Get<List<Product>>(BaseUrl);

            public HttpResourceRequest<Product> GetById(int id) => 
                Get<Product>($"{BaseUrl}/{id}");

            public HttpResourceRequest<Product> Create(Product product) =>
                Post<Product, Product>(BaseUrl, product);

            public HttpResourceRequest<Product> Update(int id, Product product) =>
                Put<Product, Product>($"{BaseUrl}/{id}", product);

            public HttpResourceRequest Delete(int id) =>
                Delete($"{BaseUrl}/{id}");

            public HttpResourceRequest<SearchResult<Product>> Search(SearchCriteria criteria) =>
                Post<SearchCriteria, SearchResult<Product>>($"{BaseUrl}/search", criteria)
                    .WithQueryParam("timestamp", DateTime.UtcNow.Ticks.ToString());

            public HttpResourceRequest<MergeResult> CustomMerge() =>
                Request<MergeResult>(new HttpMethod("MERGE"), $"{BaseUrl}/merge");

            // Error test methods
            public HttpResourceRequest<Product> GetNotFound() =>
                Get<Product>($"{BaseUrl}/notfound");

            public HttpResourceRequest<Product> GetUnauthorized() =>
                Get<Product>($"{BaseUrl}/unauthorized");

            public HttpResourceRequest<Product> GetError() =>
                Get<Product>($"{BaseUrl}/error");
        }

        [Fact]
        public async Task FullCrudWorkflow_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            
            // Act & Assert - Get All
            var getAllResource = await resource.GetAll().ExecuteAsync();
            await Task.Delay(100);
            Assert.NotNull(getAllResource.Value);
            var allProducts = (getAllResource.Value as HttpResponse<List<Product>>)?.Data;
            Assert.NotNull(allProducts);
            Assert.Equal(2, allProducts.Count);

            // Act & Assert - Get By Id
            var getByIdResource = await resource.GetById(1).ExecuteAsync();
            await Task.Delay(100);
            Assert.NotNull(getByIdResource.Value);
            var product = (getByIdResource.Value as HttpResponse<Product>)?.Data;
            Assert.NotNull(product);
            Assert.Equal(1, product.Id);
            Assert.Equal("Product 1", product.Name);

            // Act & Assert - Create
            var newProduct = new Product { Name = "New Product", Price = 30.99m };
            var createResource = await resource.Create(newProduct).ExecuteAsync();
            await Task.Delay(100);
            Assert.NotNull(createResource.Value);
            var createdProduct = (createResource.Value as HttpResponse<Product>)?.Data;
            Assert.NotNull(createdProduct);
            Assert.Equal(3, createdProduct.Id);
            Assert.Equal("New Product", createdProduct.Name);

            // Act & Assert - Update
            var updateProduct = new Product { Id = 1, Name = "Updated Product", Price = 15.99m };
            var updateResource = await resource.Update(1, updateProduct).ExecuteAsync();
            await Task.Delay(100);
            Assert.NotNull(updateResource.Value);
            var updatedProduct = (updateResource.Value as HttpResponse<Product>)?.Data;
            Assert.NotNull(updatedProduct);
            Assert.Equal("Updated Product", updatedProduct.Name);

            // Act & Assert - Delete
            var deleteResource = await resource.Delete(1).ExecuteAsync();
            await Task.Delay(100);
            Assert.NotNull(deleteResource.Value);
            Assert.True(deleteResource.Value.IsSuccess);
        }

        [Fact]
        public async Task ComplexSearch_WithHeadersAndQueryParams_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            var criteria = new SearchCriteria 
            { 
                Query = "test", 
                MinPrice = 10, 
                MaxPrice = 100 
            };
            
            // Act
            var searchResource = await resource.Search(criteria).ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.NotNull(searchResource.Value);
            var results = (searchResource.Value as HttpResponse<SearchResult<Product>>)?.Data;
            Assert.NotNull(results);
            Assert.Equal(1, results.TotalCount);
            Assert.Contains("timestamp=", results.Query);
        }

        [Fact]
        public async Task CustomHttpMethod_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            
            // Act
            var mergeResource = await resource.CustomMerge().ExecuteAsync();
            await Task.Delay(100);
            
            // Assert
            Assert.NotNull(mergeResource.Value);
            var result = (mergeResource.Value as HttpResponse<MergeResult>)?.Data;
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(99, result.MergedId);
        }

        [Fact]
        public async Task ErrorHandling_NotFound_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            var notFoundHandled = false;
            
            // Act
            var notFoundResource = await resource.GetNotFound()
                .ConfigureResource(r => r.OnNotFound(response => { notFoundHandled = true; return Task.CompletedTask; }))
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            Assert.True(notFoundHandled);
            Assert.Equal(HttpStatusCode.NotFound, notFoundResource.LastStatusCode.Value);
        }

        [Fact]
        public async Task ErrorHandling_Unauthorized_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            var unauthorizedHandled = false;
            
            // Act
            var unauthorizedResource = await resource.GetUnauthorized()
                .ConfigureResource(r => r.OnUnauthorized(response => { unauthorizedHandled = true; return Task.CompletedTask; }))
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            Assert.True(unauthorizedHandled);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResource.LastStatusCode.Value);
        }

        [Fact]
        public async Task ErrorHandling_ServerError_Works()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            var errorHandled = false;
            
            // Act
            var errorResource = await resource.GetError()
                .ConfigureResource(r => r.OnServerError(response => { errorHandled = true; return Task.CompletedTask; }))
                .ExecuteAsync();
            
            await Task.Delay(100);
            
            // Assert
            Assert.True(errorHandled);
            Assert.Equal(HttpStatusCode.InternalServerError, errorResource.LastStatusCode.Value);
        }

        [Fact]
        public async Task ReactiveSubscriptions_Work()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            var loadingStates = new List<bool>();
            Product? receivedProduct = null;
            
            // Act
            var productResource = await resource.GetById(1)
                .ConfigureResource(r =>
                {
                    r.IsLoading.Subscribe(loading => loadingStates.Add(loading));
                    r.Subscribe(response =>
                    {
                        if (response is HttpResponse<Product> productResponse)
                        {
                            receivedProduct = productResponse.Data;
                        }
                    });
                })
                .ExecuteAsync();
            
            await Task.Delay(200);
            
            // Assert
            Assert.NotEmpty(loadingStates);
            Assert.Contains(true, loadingStates); // Was loading
            Assert.Contains(false, loadingStates); // Finished loading
            Assert.NotNull(receivedProduct);
            Assert.Equal(1, receivedProduct.Id);
        }

        [Fact]
        public async Task MultipleRequests_MaintainIsolation()
        {
            // Arrange
            var httpClient = new HttpClient(new TestApiHandler()) 
            { 
                BaseAddress = new Uri("https://api.test.com") 
            };
            var resource = new ProductResource(httpClient);
            
            // Act - Execute multiple requests concurrently
            var tasks = new[]
            {
                resource.GetById(1).ExecuteAsync(),
                resource.GetAll().ExecuteAsync(),
                resource.Search(new SearchCriteria { Query = "test" }).ExecuteAsync()
            };
            
            var resources = await Task.WhenAll(tasks);
            await Task.Delay(100);
            
            // Assert - Each request should complete independently
            Assert.All(resources, r => Assert.NotNull(r.Value));
            Assert.All(resources, r => Assert.True(r.Value!.IsSuccess));
        }
    }
}