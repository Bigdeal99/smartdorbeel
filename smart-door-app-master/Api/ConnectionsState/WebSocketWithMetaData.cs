using Fleck;

namespace Api.ConnectionsState;

public class WebSocketWithMetaData
{
    private readonly ILogger<WebSocketWithMetaData> _logger;

    public IWebSocketConnection Connection { get; set; }
    public string Username { get; set; }
    public WebSocketWithMetaData(IWebSocketConnection connection, ILogger<WebSocketWithMetaData> logger)
    {
        Connection = connection;
        Username = string.Empty;
        _logger = logger;

    }
    
}