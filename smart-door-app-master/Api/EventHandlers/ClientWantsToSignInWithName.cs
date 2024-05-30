using System.Text.Json;
using Api.ConnectionsState;
using Api.Dtos;
using Api.Filters;
using Common;
using Fleck;
using lib;
using Service;

namespace Api.EventHandlers;

[ValidateDataAnnotations]
public class ClientWantsToSignInWithName : BaseEventHandler<ClientWantsToSignInWithNameDto>
{
    private readonly BellService _bellService;
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<ClientWantsToSignInWithName> _logger;

    public ClientWantsToSignInWithName(BellService bellService, ConnectionManager connectionManager, ILogger<ClientWantsToSignInWithName> logger)
    {
        _bellService = bellService;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public override async Task Handle(ClientWantsToSignInWithNameDto dto, IWebSocketConnection socket)
    {
        try
        {
            var metaData = _connectionManager.GetConnection(socket.ConnectionInfo.Id);
            if (metaData == null)
            {
                _logger.LogWarning("Failed to sign in: missing connection metadata for client {ClientId}.", socket.ConnectionInfo.Id);

                socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
                {
                    ErrorMessage = "Failed to sign in due to missing connection metadata."
                }));
                return;
            }

            _logger.LogInformation("Opening connection for client {ClientId}.", socket.ConnectionInfo.Id);
            await _bellService.OpenConnection();

            metaData.Username = dto.Name;
            socket.Send(JsonSerializer.Serialize(new ServerSendsInfoToClient
            {
                Message = "You have connected as " + dto.Name

            }));
            _logger.LogInformation("Client {ClientId} connected as {NickName}.", socket.ConnectionInfo.Id, dto.Name);
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "AppException occurred while signing in client {ClientId}.", socket.ConnectionInfo.Id);

            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = ex.Message
            }));
        }
        catch (Exception ex)
        {
            var errorMessage = "An unexpected error occurred. Please try again later.";

            _logger.LogError(ex, "Unexpected error occurred while signing in client {ClientId}.", socket.ConnectionInfo.Id);

            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = errorMessage
            }));
        }
    }
} 