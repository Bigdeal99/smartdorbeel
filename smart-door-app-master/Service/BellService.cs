using System.Collections;
using System.Text.Json;
using Common;
using Infrastructure;
using Microsoft.Extensions.Logging;
using MQTT;

namespace Service;

public class BellService
{
    private readonly MQTTUtility _mqttUtility;
    private readonly BellRepository _bellRepository;
    private ILogger<BellService> _logger;
    private bool _isSubscribed = false;

    public BellService(MQTTUtility mqttUtility, BellRepository bellRepository, ILogger<BellService> logger)
    {
        _mqttUtility = mqttUtility;
        _bellRepository = bellRepository;
        _logger = logger;
    }

    public void HandleReceivedMessage(string topic, string message)
    {
        OnNotificationReceived?.Invoke(topic, message);
        try
        {
            _bellRepository.AddBellData(topic, null, message).Wait();
            _logger.LogInformation("handled received message on '{Topic}': {Message}", topic, message);
            using (var httpClient = new HttpClient())
            {var payload = new Dictionary<string, string>
                {
                    { "value1", topic },
                    { "value2", message }
                };
                var messageToiffft = JsonSerializer.Serialize(payload);
                var requestUri = $"https://maker.ifttt.com/trigger/{messageToiffft}/json/with/key/nYTXhTPJg0LkIgtxP45lX8OmITfAhV_3zEN7LGMCTEz";
                
                var content = new FormUrlEncodedContent(payload);
                var response = httpClient.PostAsync(requestUri, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent IFTTT request for topic '{Topic}' with message '{Message}'", topic, message);
                }
                else
                {
                    _logger.LogError("Failed to send IFTTT request. Status code: {StatusCode}", response.StatusCode);
                }
            }
        }
        
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured while handling the received message.");
            throw new AppException("An error occured while handling the received message, Please try again ..");
        }
    }

    public async Task ControlCamera(string topic, string command)
    {
        try
        {
            await _mqttUtility.PublishAsync(topic, command);
            //await _bellRepository.AddBellData(null, topic, command);
            _logger.LogInformation("Command '{Command}' to '{Topic}' has been sent", command, topic);
        }
        catch (AppException e)
        {
            _logger.LogError(e, "AppException occured while sending camera command.");
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occured at Bell Service.");
            throw new AppException(
                "An unexpected error occured while sending commands to camera, please try again later ..");
        }
    }
    
    public async Task OpenConnection()
    {
        try
        {
            await _mqttUtility.ConnectAsync();
            _mqttUtility.InitializeLiveBellNotifications();
            if (!_isSubscribed)
            {
                _mqttUtility.MessageReceived += HandleReceivedMessage;
                _isSubscribed = true;
            }
            _logger.LogInformation("Opened connection and initialized subscriptions.");
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "AppException occurred while opening the connection.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while opening the connection.");
            throw new AppException("An unexpected error occurred while opening the connection. Please try again later.");
        }
    }

    public async Task CloseConnection()
    {
        try
        {
            if (_isSubscribed)
            {
                _mqttUtility.MessageReceived -= HandleReceivedMessage;
                _isSubscribed = false;
            }
            await _mqttUtility.DisconnectAsync();
            _logger.LogInformation("Closed connection.");
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "AppException occurred while closing the connection.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while closing the connection.");
            throw new AppException("An unexpected error occurred while closing the connection. Please try again later.");
        }
    }

    public event Action<string, string> OnNotificationReceived;

    public async Task<IEnumerable> GetBellLog()
    {
        try
        {
            var bellLog = await _bellRepository.GetBellData();
            _logger.LogInformation("Bell log has been successfully fetched.");
            return bellLog;
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "AppException occurred while fetching bell log.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while fetching bell log.");
            throw new AppException("An unexpected error occurred while fetching bell log. Please try again later.");
        }
    }
} 