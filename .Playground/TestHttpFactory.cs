using FluentSignals.Http.Factories;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;

namespace Playground
{
    public class TestHttpFactory
    {
        public static void TestDefaultGenericParameter()
        {
            // Mock setup (in real usage, these would come from DI)
            var serviceProvider = new MockServiceProvider();
            var options = Options.Create(new FluentSignals.Http.Options.HttpResourceOptions());
            var factory = new HttpResourceFactory(serviceProvider, options);

            // Test 1: Non-generic Create should default to HttpResponseMessage
            var deleteResource = factory.Create(() =>
                new HttpRequestMessage(HttpMethod.Delete, $"/api/team-members/123")
            );
            
            // This should compile and deleteResource should be of type HttpResource<HttpResponseMessage>
            Console.WriteLine($"Type: {deleteResource.GetType()}");

            // Test 2: Generic Create should still work
            var deleteResourceGeneric = factory.Create<HttpResponseMessage>(() =>
                new HttpRequestMessage(HttpMethod.Delete, $"/api/team-members/456")
            );
            
            Console.WriteLine($"Generic Type: {deleteResourceGeneric.GetType()}");

            // Test 3: Custom type should still work
            var customResource = factory.Create<CustomResponse>(() =>
                new HttpRequestMessage(HttpMethod.Get, "/api/custom")
            );
            
            Console.WriteLine($"Custom Type: {customResource.GetType()}");
        }
    }

    public class CustomResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class MockServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(HttpClient))
            {
                return new HttpClient { BaseAddress = new Uri("https://api.example.com") };
            }
            return null;
        }
    }
}