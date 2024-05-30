using System.Text.Json;
using Api.ConnectionsState;
using Api.Dtos;
using Common;
using Fleck;
using lib;

namespace Api.Filters;

public class RequireAuthenticationAttribute : BaseEventFilter
{

    public override Task Handle<T>(IWebSocketConnection socket, T dto)
    {

        try
        {
            var connectionManager = ServiceLocator.ServiceProvider.GetService<ConnectionManager>();
            if (connectionManager == null)
            {
                throw new AppException(
                    "Internal server error: ConnectionManager service is not available. Please try again later.");
            }

            if (!connectionManager.IsAuthenticated(socket))
            {
                socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
                {
                    ErrorMessage = "You must sign in before you connect."
                }));
                throw new AppException("Client must be authenticated to use this feature.");
            }

            return Task.CompletedTask;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AppException("Client must be authenticated to use this feature.");
        }
        catch (InvalidOperationException ex)
        {
            throw new AppException(
                "Internal server error: ConnectionManager service is not available. Please try again later.");
        }
        catch (Exception ex)
        {
            throw new AppException("An unexpected error occurred. Please try again later.");
        }
    }
}

public static class ServiceLocator
{
    public static IServiceProvider ServiceProvider { get; set; }
}