using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentSignals.Http.Interceptors;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FluentSignals.Http.Tests
{
    public class HttpResourceInterceptorTests
    {
        [Fact]
        public async Task BearerTokenInterceptor_ShouldAddAuthorizationHeader()
        {
            // Arrange
            var token = "test-bearer-token";
            var interceptor = new BearerTokenInterceptor(token);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com");

            // Act
            var modifiedRequest = await interceptor.OnRequestAsync(request);

            // Assert
            Assert.NotNull(modifiedRequest.Headers.Authorization);
            Assert.Equal("Bearer", modifiedRequest.Headers.Authorization.Scheme);
            Assert.Equal(token, modifiedRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task BearerTokenInterceptor_WithAsyncProvider_ShouldAddToken()
        {
            // Arrange
            var token = "async-token";
            var tokenProvider = new Func<Task<string>>(() => Task.FromResult(token));
            var interceptor = new BearerTokenInterceptor(tokenProvider);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com");

            // Act
            var modifiedRequest = await interceptor.OnRequestAsync(request);

            // Assert
            Assert.NotNull(modifiedRequest.Headers.Authorization);
            Assert.Equal(token, modifiedRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task LoggingInterceptor_ShouldLogRequest()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            
            var interceptor = new LoggingInterceptor(mockLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/users");

            // Act
            await interceptor.OnRequestAsync(request);

            // Assert
            mockLogger.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GET") && v.ToString()!.Contains("https://api.test.com/users")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task LoggingInterceptor_ShouldLogResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            
            var interceptor = new LoggingInterceptor(mockLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/users");
            var response = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
            
            // Simulate request first
            await interceptor.OnRequestAsync(request);

            // Act
            await interceptor.OnResponseAsync(response);

            // Assert
            mockLogger.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("200")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task LoggingInterceptor_ShouldLogException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();
            var interceptor = new LoggingInterceptor(mockLogger.Object);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com/users");
            var exception = new HttpRequestException("Network error");

            // Act
            await interceptor.OnExceptionAsync(request, exception);

            // Assert
            mockLogger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task RetryInterceptor_ShouldMarkForRetry()
        {
            // Arrange
            var interceptor = new RetryInterceptor(maxRetries: 3);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com");
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = request };

            // Act
            await interceptor.OnRequestAsync(request);
            var modifiedResponse = await interceptor.OnResponseAsync(response);

            // Assert
            Assert.True(modifiedResponse.Headers.Contains("X-Should-Retry"));
        }

        [Fact]
        public async Task RetryInterceptor_ShouldNotRetrySuccessfulRequests()
        {
            // Arrange
            var interceptor = new RetryInterceptor(maxRetries: 3);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com");
            var response = new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };

            // Act
            await interceptor.OnRequestAsync(request);
            var modifiedResponse = await interceptor.OnResponseAsync(response);

            // Assert
            Assert.False(modifiedResponse.Headers.Contains("X-Should-Retry"));
        }

        [Fact]
        public async Task RetryInterceptor_WithCustomPredicate_ShouldRespectIt()
        {
            // Arrange
            var shouldRetry = new Func<HttpResponseMessage, bool>(r => r.StatusCode == HttpStatusCode.BadRequest);
            var interceptor = new RetryInterceptor(3, TimeSpan.FromMilliseconds(100), shouldRetry);
            
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.test.com");
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest) { RequestMessage = request };

            // Act
            await interceptor.OnRequestAsync(request);
            var modifiedResponse = await interceptor.OnResponseAsync(response);

            // Assert
            Assert.True(modifiedResponse.Headers.Contains("X-Should-Retry"));
        }
    }
}