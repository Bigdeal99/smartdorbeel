using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Infrastructure;
using Microsoft.Extensions.Logging;
using MQTT;

namespace Service
{
    public class BellService
    {
        private readonly MQTTUtility _mqttUtility;
        private readonly BellRepository _bellRepository;
        private readonly ILogger<BellService> _logger;
        private readonly HttpClient _httpClient;
        private bool _isSubscribed = false;

        // IFTTT Webhook details
        private const string IftttHost = "https://maker.ifttt.com/trigger/";
        private const string IftttEvent = "button_pressed";
        private const string IftttKey = "hUAAAz0AVvc6-NW1UmqWXXv6VQWmpiGFxx3sV5rnaM9";

        public BellService(MQTTUtility mqttUtility, BellRepository bellRepository, ILogger<BellService> logger, HttpClient httpClient)
        {
            _mqttUtility = mqttUtility;
            _bellRepository = bellRepository;
            _logger = logger;
            _httpClient = httpClient;
        }

        public void HandleReceivedMessage(string topic, string message)
        {
            OnNotificationReceived?.Invoke(topic, message);
            try
            {
                _bellRepository.AddBellData(topic, null, message).Wait();
                _logger.LogInformation("Handled received message on '{Topic}': {Message}", topic, message);

                // Trigger IFTTT event when the specific message is received
                if (message == "Somebody rang the bell, Image captured and sent")
                {
                    SendIftttEvent().Wait();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while handling the received message.");
                throw new AppException("An error occurred while handling the received message, Please try again ..");
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
                _logger.LogError(e, "AppException occurred while sending camera command.");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unexpected error occurred at Bell Service.");
                throw new AppException("An unexpected error occurred while sending commands to camera, please try again later ..");
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

        private async Task SendIftttEvent()
        {
            try
            {
                var url = $"{IftttHost}{IftttEvent}/with/key/{IftttKey}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("IFTTT event triggered successfully.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred while sending the IFTTT event.");
                throw new AppException("An error occurred while sending the IFTTT event.");
            }
        }
    }
}
