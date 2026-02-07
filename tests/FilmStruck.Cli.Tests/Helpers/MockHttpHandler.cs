using System.Net;

namespace FilmStruck.Cli.Tests.Helpers;

public class MockHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();
    private Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

    public void SetResponse(string urlPattern, HttpResponseMessage response)
    {
        _responses[urlPattern] = response;
    }

    public void SetResponse(string urlPattern, string jsonContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses[urlPattern] = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };
    }

    public void SetResponseFactory(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        _responseFactory = factory;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_responseFactory != null)
        {
            return Task.FromResult(_responseFactory(request));
        }

        var url = request.RequestUri?.ToString() ?? "";

        foreach (var kvp in _responses)
        {
            if (url.Contains(kvp.Key))
            {
                return Task.FromResult(kvp.Value);
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No mock response configured for: {url}")
        });
    }
}
