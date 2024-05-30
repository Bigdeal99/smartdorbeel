using Common;
using Fleck;

namespace Api.ConnectionsState;

public class ConnectionManager
{
    private readonly Dictionary<Guid, WebSocketWithMetaData> _connections = new();
    private readonly ILogger<ConnectionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    
    public ConnectionManager(ILogger<ConnectionManager> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

    }

    public void AddConnection(Guid id, IWebSocketConnection socket)
    {
        try
        {
            var logger = _loggerFactory.CreateLogger<WebSocketWithMetaData>();

            if (!_connections.ContainsKey(id))
            {
                _connections[id] = new WebSocketWithMetaData(socket, logger);
                _logger.LogInformation($"New connection added with GUID: {id}");
            }
            else
            {
                _logger.LogInformation($"Connection with GUID: {id} already exists. Updating socket reference.");
                _connections[id].Connection = socket;
            }
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while establishing the connection. Please try again later.");
        }
    }

    public void RemoveConnection(Guid id)
    {
        try
        {
            if (_connections.ContainsKey(id))
            {
                _connections[id].Connection.Close();
                _connections.Remove(id);
                _logger.LogInformation($"Connection and associated metadata removed: {id}");
            }
            else
            {
                _logger.LogWarning($"Attempted to remove non-existent connection with GUID: {id}");
            }
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while disconnecting. Please try again later or close the app.");
        }
    }
    
    public WebSocketWithMetaData GetConnection(Guid id)
    {
        try
        {
            _connections.TryGetValue(id, out var metaData);
            return metaData;
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while retrieving a connection. Please try again later.");
        }
    }

    public IEnumerable<WebSocketWithMetaData> GetAllConnections()
    {
        try
        {
            return _connections.Values;
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while retrieving all connections. Please try again later.");
        }
    }
    
    public bool IsAuthenticated(IWebSocketConnection socket)
    {
        try
        {
            if (_connections.TryGetValue(
                    socket.ConnectionInfo.Id, out WebSocketWithMetaData metaData))
            {
                return !string.IsNullOrEmpty(metaData.Username);
            }
            return false;
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while checking authentication. Please try again later.");
        }
    }
    
    public bool HasMetadata(Guid id)
    {
        try
        {
            return _connections.ContainsKey(id);
        }
        catch (Exception ex)
        {
            throw new AppException("An error occurred while checking metadata. Please try again later.");
        }
    }
}