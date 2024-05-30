using System.Text.Json;
using Api.Dtos;
using Common;
using Fleck;
using lib;
using Service;

namespace Api.EventHandlers;

public class ClientWantsToSeeStream : BaseEventHandler<ClientWantsToSeeStreamDto>
{
    private readonly BellService _bellService;
    private readonly ILogger<ClientWantsToSeeStream> _logger;

    public ClientWantsToSeeStream(BellService bellService, ILogger<ClientWantsToSeeStream> logger)
    {
        _bellService = bellService;
        _logger = logger;
    }

    public override async Task Handle(ClientWantsToSeeStreamDto dto, IWebSocketConnection socket)
    {
        try
        {
            _logger.LogInformation("Client {ClientId} wants to control camera with command {Command} on topic {Topic}.", 
                socket.ConnectionInfo.Id
                , dto.Command, dto.Topic);

            await _bellService.ControlCamera(dto.Topic, dto.Command);
            var successMessage = $"Command '{dto.Command}' sent to topic '{dto.Topic}'.";
            _logger.LogInformation(successMessage);

            await socket.Send(successMessage);
        }
        catch (AppException ex)
        {
            var errorMessage = ex.Message;
            _logger.LogError(ex, "An application error occurred while processing sending commands to camera for client {ClientId}.", 
                socket.ConnectionInfo.Id
            );

            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = errorMessage
            }));
        }
        catch (Exception ex)
        {
            var errorMessage = "An unexpected error occurred. Please try again later.";
            _logger.LogError(ex, errorMessage);

            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = errorMessage
            }));
        }
    }
}