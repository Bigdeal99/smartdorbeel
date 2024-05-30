using System.Text.Json;
using Api.Dtos;
using Common;
using Fleck;
using lib;
using Service;

namespace Api.EventHandlers;

public class ClientWantsToDeleteSingleLog : BaseEventHandler<ClientWantsToDeleteSingleLogDto>
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<ClientWantsToDeleteSingleLog> _logger;

    public ClientWantsToDeleteSingleLog(BlobStorageService blobStorageService, ILogger<ClientWantsToDeleteSingleLog> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public override async Task Handle(ClientWantsToDeleteSingleLogDto dto, IWebSocketConnection socket)
    {
        try
        {
            await _blobStorageService.DeleteImageAsync(dto.FileName);
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "An error occurred while deleting image.");
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = ex.Message
            }));
        }
        catch (Exception ex)
        {
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = "An unexpected error occured while deleting image, please try again later ...."
            }));
        }
    }
}