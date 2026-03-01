using System.Net;
using System.Net.Http.Headers;
using Moq;
using Moq.Protected;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Provides test helpers for creating mocked <see cref="HttpMessageHandler"/> instances.
/// </summary>
internal static class HttpTestHelpers
{
    /// <summary>
    /// Creates a mock <see cref="HttpMessageHandler"/> that returns the specified
    /// <paramref name="content"/> with HTTP 200 OK.
    /// </summary>
    internal static Mock<HttpMessageHandler> MockHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        return handlerMock;
    }

    /// <summary>
    /// Creates a <see cref="HttpClient"/> backed by the provided mock handler.
    /// </summary>
    internal static HttpClient CreateClient(Mock<HttpMessageHandler> handlerMock) =>
        new HttpClient(handlerMock.Object);
}
