using System.Reflection;
using System.Text;
using System.Text.Json;
using Api.ConnectionsState;
using Api.Dtos;
using Api.EventHandlers;
using Api.Filters;
using Azure.Storage.Blobs;
using Common;
using Fleck;
using Infrastructure;
using lib;
using MQTT;
using Service;


var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSingleton<BellService>();
builder.Services.AddSingleton<MQTTUtility>();
builder.Services.AddSingleton<BellRepository>();
builder.Services.AddSingleton<BaseEventHandler<ClientWantsToSignInWithNameDto>, ClientWantsToSignInWithName>();
builder.Services.AddSingleton<BaseEventHandler<ClientWantsToSeeStreamDto>, ClientWantsToSeeStream>();


builder.Services.AddNpgsqlDataSource(Utilities.ProperlyFormattedConnectionString, 
    dataSourceBuilder => dataSourceBuilder.EnableParameterLogging());
builder.Services.AddLogging(configure => configure.AddConsole());
builder.Services.AddSingleton<BlobServiceClient>(provider =>
{
    var connectionString = "DefaultEndpointsProtocol=https;AccountName=iotproject1;AccountKey=iJQDm/Tu7HqLpFhNSDzt9klE/khgaU0FOuAUelNvPUdeFHf2fKW6X1paCbr4Z0rdFCPD+d6Iu6Ng+AStZO2g8A==;EndpointSuffix=core.windows.net";
    return new BlobServiceClient(connectionString);
});
builder.Services.AddTransient<BlobStorageService>();
builder.Services.AddTransient<ValidateDataAnnotations>();
builder.Services.AddTransient<RequireAuthenticationAttribute>();

var clientEventHandlers = builder.FindAndInjectClientEventHandlers(Assembly.GetExecutingAssembly());

var app = builder.Build();

var connectionManager = app.Services.GetRequiredService<ConnectionManager>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var blobStorageService = app.Services.GetRequiredService<BlobStorageService>();

builder.WebHost.UseUrls("http://*:9999");
var port = Environment.GetEnvironmentVariable("PORT") ?? "8181";
var server = new WebSocketServer("ws://0.0.0.0:" + port);
bool subscribe = false;
ServiceLocator.ServiceProvider = app.Services;

server.Start(socket =>
{
    if (!subscribe)
    {
        var bellService = app.Services.GetRequiredService <BellService >();
        bellService.OpenConnection();
        subscribe = true;
    }
   
    var keepAliveInterval = TimeSpan.FromSeconds(30);
    var keepAliveTimer = new System.Timers.Timer(keepAliveInterval.TotalMilliseconds)
    {
        AutoReset = true, 
        Enabled = true 
    };
    keepAliveTimer.Elapsed += (sender, e) => {
        try
        {
            socket.Send("ping"); 
        }
        catch (Exception ex) 
        {
            Console.WriteLine("Exception in keep-alive timer: " + ex.Message);
            keepAliveTimer.Stop(); 
        } 
    };
    socket.OnOpen = () =>
    {
        try
        {
            connectionManager.AddConnection(socket.ConnectionInfo.Id, socket);
            logger.LogInformation($"New connection added with GUID: {socket.ConnectionInfo.Id}");
        }
        catch (AppException ex)
        {
            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = ex.Message
            }));
            
            logger.LogError(ex, $"AppException: {ex.Message}");
        }
        catch (Exception ex)
        {
            var errorMessage = "An unexpected error occurred during the connection process. Please try again later.";
            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto
            {
                ErrorMessage = errorMessage
            }));
            logger.LogError(ex, $"Exception: {ex.Message}");
        }
    };
    
    socket.OnClose = async () =>
    {
        try
        {
            logger.LogInformation("Connection closed.");
            keepAliveTimer.Stop();
            
            connectionManager.RemoveConnection(socket.ConnectionInfo.Id);
            if (!connectionManager.HasMetadata(socket.ConnectionInfo.Id))
            {
                logger.LogInformation($"Metadata successfully removed for GUID: {socket.ConnectionInfo.Id}");
            }
            else
            {
                logger.LogWarning($"Failed to remove metadata for GUID: {socket.ConnectionInfo.Id}");
            }
        }
        catch (AppException ex)
        {
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = ex.Message
            }));
            logger.LogError(ex, $"AppException: {ex.Message}");
        }
        catch (Exception ex)
        {
            var errorMessage = "An unexpected error occurred while closing the connection. Please try again later.";
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = errorMessage
            }));
            logger.LogError(ex, $"Exception: {ex.Message}");
        }
    };

    socket.OnMessage = async message =>
    {
        logger.LogInformation("Message Received: {Message}", message);
        await app.InvokeClientEventHandler(clientEventHandlers, socket, message);
    };
    
    socket.OnBinary = async data =>
    {
        logger.LogInformation("Binary Data Received, length: {Length}", data.Length);
        
        if (data.Length < 4)
        {
            logger.LogWarning("Binary data too short to contain header.");
            socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = "Error: Data was corrupted."
            }));
            return;
        }
        
        var header = Encoding.ASCII.GetString(data, 0, 4);
        var contentData = data.Skip(4).ToArray();

        if (header == "IMGF")
        {
            logger.LogInformation("Received an image file.");
            await ProcessImageAsync(contentData, blobStorageService);
        }
        else if (header == "VIDF")
        {
            logger.LogInformation("Received a video frame.");
            foreach (var client in connectionManager.GetAllConnections())
            {
                if(!string.IsNullOrEmpty(client.Username))
                    await client.Connection.Send(contentData);
            }
        }
        else
        {
            logger.LogWarning("Unknown file type received.");
            await socket.Send(JsonSerializer.Serialize(new ServerSendsErrorMessageDto()
            {
                ErrorMessage = "Error: Unknown file type."
            }));
        }
    };
    
    socket.OnError = exception =>
    {
        logger.LogError("Error: {Message}", exception.Message); 
    };
});

async Task<string> ProcessImageAsync(byte[] data, BlobStorageService blobStorageService)
{
    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    return await blobStorageService.UploadImageAsync(timestamp, data);
}

app.Run(); 