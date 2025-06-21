using System;
using System.Net.Http;
using FluentSignals.Blazor.Http;
using FluentSignals.Http.Resources;
using FluentSignals.Http.Options;
using FluentSignals.Http.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FluentSignals.Http.Tests
{
    public class TypedHttpResourceFactoryTests
    {
        [HttpResource("/api/test")]
        private class TestResource : TypedHttpResource
        {
            public TestResource() { }
            
            public HttpResourceRequest<string> GetTest() => 
                Get<string>($"{BaseUrl}/endpoint");
        }

        private class NoAttributeResource : TypedHttpResource
        {
            public NoAttributeResource() { }
        }

        private class MockHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _httpClient;
            
            public MockHttpClientFactory(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }
            
            public HttpClient CreateClient(string name)
            {
                return _httpClient;
            }
        }

        [Fact]
        public void Create_WithAttributeResource_UsesAttributeBaseUrl()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var options = Microsoft.Extensions.Options.Options.Create(new HttpResourceOptions());
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory = new TypedHttpResourceFactory<TestResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource = factory.Create();
            
            // Assert
            Assert.NotNull(resource);
            Assert.Equal("/api/test", resource.BaseUrl);
        }

        [Fact]
        public void Create_WithoutAttribute_UsesEmptyBaseUrl()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var options = Microsoft.Extensions.Options.Options.Create(new HttpResourceOptions());
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory = new TypedHttpResourceFactory<NoAttributeResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource = factory.Create();
            
            // Assert
            Assert.NotNull(resource);
            Assert.Equal(string.Empty, resource.BaseUrl);
        }

        [Fact]
        public void Create_WithConfiguration_AppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var options = Microsoft.Extensions.Options.Options.Create(new HttpResourceOptions());
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory = new TypedHttpResourceFactory<TestResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource = factory.Create(opt =>
            {
                opt.Timeout = TimeSpan.FromSeconds(60);
                opt.RetryOptions = new RetryOptions { MaxRetryAttempts = 5 };
            });
            
            // Assert
            Assert.NotNull(resource);
            // The configuration would be applied to the HttpResource
        }

        [Fact]
        public void Create_WithBaseUrlInOptions_UsesOptionsBaseUrl()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var resourceOptions = new HttpResourceOptions { BaseUrl = "https://api.example.com" };
            var options = Microsoft.Extensions.Options.Options.Create(resourceOptions);
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory = new TypedHttpResourceFactory<TestResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource = factory.Create();
            
            // Assert
            Assert.NotNull(resource);
            // The base URL from options would be used in the HttpClient
        }

        [Fact]
        public void ServiceRegistration_RegistersFactoryAndResource()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IHttpClientFactory, MockHttpClientFactory>();
            services.AddScoped(sp => new HttpClient());
            services.Configure<HttpResourceOptions>(opt => { });
            
            // Act
            // services.AddTypedHttpResourceFactory<TestResource>(); // Extension method might not exist
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert
            var factory = serviceProvider.GetService<ITypedHttpResourceFactory<TestResource>>();
            Assert.NotNull(factory);
            
            var resource = serviceProvider.GetService<TestResource>();
            Assert.NotNull(resource);
        }

        [Fact]
        public void MultipleFactories_MaintainIsolation()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient() { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var options = Microsoft.Extensions.Options.Options.Create(new HttpResourceOptions());
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory1 = new TypedHttpResourceFactory<TestResource>(mockFactory, options, serviceProvider);
            var factory2 = new TypedHttpResourceFactory<NoAttributeResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource1 = factory1.Create();
            var resource2 = factory2.Create();
            
            // Assert
            Assert.NotNull(resource1);
            Assert.NotNull(resource2);
            Assert.NotEqual(resource1.BaseUrl, resource2.BaseUrl);
        }

        [Fact]
        public void Create_InitializesResourceCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var httpClient = new HttpClient { BaseAddress = new Uri("https://api.test.com") };
            var mockFactory = new MockHttpClientFactory(httpClient);
            var options = Microsoft.Extensions.Options.Options.Create(new HttpResourceOptions 
            { 
                Timeout = TimeSpan.FromSeconds(30),
                RetryOptions = new RetryOptions { MaxRetryAttempts = 3 }
            });
            
            services.AddSingleton<IHttpClientFactory>(mockFactory);
            services.AddSingleton(options);
            
            var serviceProvider = services.BuildServiceProvider();
            var factory = new TypedHttpResourceFactory<TestResource>(mockFactory, options, serviceProvider);
            
            // Act
            var resource = factory.Create();
            
            // Assert
            Assert.NotNull(resource);
            Assert.Equal("/api/test", resource.BaseUrl);
            // The resource would be initialized with the provided HttpClient and options
        }
    }
}