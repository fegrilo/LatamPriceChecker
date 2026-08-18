using System.Net;

namespace LatamPriceChecker.Tests.TestHelpers;

/// <summary>
/// HttpMessageHandler de teste que devolve uma resposta fixa e captura a última requisição feita,
/// para permitir tanto simular respostas quanto inspecionar o que foi enviado (ex.: payload do Discord).
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseContent;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }
    public int CallCount { get; private set; }

    public FakeHttpMessageHandler(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent)
        };
    }
}
