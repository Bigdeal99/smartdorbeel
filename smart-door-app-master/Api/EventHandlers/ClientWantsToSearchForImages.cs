using System.Text.Json;
using Api.Dtos;
using Common;
using Fleck;
using lib;
using Service;

namespace Api.EventHandlers;

public class ClientWantsToSearchForImages : BaseEventHandler<ClientWantsToSearchForImagesDto>
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILogger<ClientWantsToGetBellLog> _logger;

    public ClientWantsToSearchForImages(BlobStorageService blobStorageService, ILogger<ClientWantsToGetBellLog> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public override async Task Handle(ClientWantsToSearchForImagesDto dto, IWebSocketConnection socket)
    {
        try
        {
            Console.WriteLine("The dagra we are looking for is: " + dto.DateTime);
            var imageResult = await _blobStorageService.SearchImagesByDateAsync(dto.DateTime);
            await socket.Send(imageResult);
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