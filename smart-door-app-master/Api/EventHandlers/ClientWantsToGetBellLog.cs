using System.Text.Json;
using Api.Dtos;
using Common;
using Fleck;
using lib;
using Service;

namespace Api.EventHandlers;

public class ClientWantsToGetBellLog : BaseEventHandler<ClientWantsToGetBellLogDto>
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<ClientWantsToGetBellLog> _logger;

    public ClientWantsToGetBellLog(BlobStorageService blobStorageService, ILogger<ClientWantsToGetBellLog> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public override async Task Handle(ClientWantsToGetBellLogDto dto, IWebSocketConnection socket)
    {
        try
        {
            var images = await _blobStorageService.GetImagesAsync();
            await socket.Send(images);
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "An error occurred while fetching images.");
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = ex.Message
            }));
        }
        catch (Exception ex)
        {
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = "An unexpected error occured while fetching images, please try again later ...."
            }));
        }
    }
}