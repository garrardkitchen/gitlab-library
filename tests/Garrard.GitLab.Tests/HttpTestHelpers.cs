using System.Net;
using Garrard.GitLab.Library.Http;
using Moq;
using Moq.Protected;

namespace Garrard.GitLab.Tests;

/// <summary>
/// Provides test helpers for creating mocked <see cref="HttpMessageHandler"/> and
/// <see cref="IGitLabHttpClientFactory"/> instances.
/// </summary>
internal static class HttpTestHelpers
{
    /// <summary>
    /// Creates a mock <see cref="HttpMessageHandler"/> that returns the specified
    /// <paramref name="content"/> with the given status code.
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
    /// Creates a <see cref="Mock{IGitLabHttpClientFactory}"/> whose <c>CreateClient()</c>
    /// returns an <see cref="HttpClient"/> backed by a mock handler with the given response.
    /// </summary>
    internal static Mock<IGitLabHttpClientFactory> CreateMockFactory(
        HttpStatusCode statusCode, string responseJson)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });

        var factoryMock = new Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient()).Returns(new HttpClient(handler.Object));
        return factoryMock;
    }

    /// <summary>
    /// Creates a <see cref="Mock{IGitLabHttpClientFactory}"/> that returns a real (unpatched)
    /// <see cref="HttpClient"/>. Use this for "invalid domain" tests where the HTTP call
    /// must actually attempt a network connection and fail.
    /// </summary>
    internal static Mock<IGitLabHttpClientFactory> CreateRealClientFactory()
    {
        var factoryMock = new Mock<IGitLabHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient()).Returns(new HttpClient());
        return factoryMock;
    }
}
