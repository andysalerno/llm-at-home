using System.Net;
using System.Text.Json;
using Autofac;
using Autofac.Builder;
using Moq;
using Moq.Protected;

namespace AgentFlow.ExampleRunner.Tests.Extensions;

public static class MoqExtensions
{
    public static void SetupMockedHttpClient<TClient, TResponse>(
        this Mock<IHttpClientFactory> factory,
        TResponse responsePayload)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responsePayload)),
            });

        factory
            .Setup(f => f.CreateClient(It.Is<string>(s => s == typeof(TClient).Name)))
            .Returns(() => new HttpClient(handler.Object));
    }

    public static IRegistrationBuilder<T, SimpleActivatorData, SingleRegistrationStyle> RegisterMockInstance<T>(
        this ContainerBuilder builder,
        Mock<T> mock)
    where T : class
        => builder.RegisterInstance(mock.Object);
}
