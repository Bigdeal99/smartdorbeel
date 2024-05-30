using System.Security.Authentication;
using System.Text;
using Common;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Microsoft.Extensions.Logging;

namespace MQTT;

public class MQTTUtility
{
   
        private readonly ILogger<MQTTUtility> _logger;
        private IMqttClient _client;
        private MqttFactory _factory;
        public event Action<string, string> MessageReceived; 

        public MQTTUtility(ILogger<MQTTUtility> logger)
        {
            _logger = logger;
            _factory = new MqttFactory();
            _client = _factory.CreateMqttClient();
        }

        public void InitializeLiveBellNotifications()
        {
            _client.ApplicationMessageReceivedAsync += HandleApplicationMessageReceivedAsync;
            try
            {
                SubscribeAsync("outside/notifications").Wait();
            }
            catch (AggregateException ex)
            {
                var innerEx = ex.InnerException;
                if (innerEx is MqttCommunicationException || innerEx is TimeoutException ||
                    innerEx is AuthenticationException || innerEx is Exception)
                {
                    _logger.LogError(innerEx, "A MQTT subscription error occured.");
                    throw new AppException("A MQTT subscription error occured, Please try again later.");
                }
            }
        }

        private async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            _logger.LogInformation("Received on {Topic}: {Message}", topic, message);
            MessageReceived?.Invoke(topic, message);
            await Task.CompletedTask;
        }
        public async Task ConnectAsync()
        {
            if (_client.IsConnected)
            {
                _logger.LogInformation("Already connected to MQTT Broker.");
                return;
            }
            var tlsOptions = new MqttClientOptionsBuilderTlsParameters
            {
                UseTls = true,
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                SslProtocol = SslProtocols.Tls12
            };

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString()) 
                .WithTcpServer(MQTTConfiguration.Server, MQTTConfiguration.Port)
                .WithCredentials(MQTTConfiguration.Username, MQTTConfiguration.Password)
                .WithCleanSession()
                .WithTls(tlsOptions)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .Build();

            try
            {
                await _client.ConnectAsync(mqttClientOptions, CancellationToken.None);
                _logger.LogInformation("Connected to MQTT Broker.");

            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError(ex, "An error occurred while connecting to the MQTT broker.");
                throw new AppException("An error occurred while connecting to the MQTT broker. Please try again later.");
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication failed while connecting to the MQTT broker.");
                throw new AppException("Authentication failed while connecting to the MQTT broker. Please check your credentials.");
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "The connection to the MQTT broker timed out.");
                throw new AppException("The connection to the MQTT broker timed out. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while connecting to the MQTT broker.");
                throw new AppException("An unexpected error occurred while connecting to the MQTT broker. Please try again later.");
            }
        }

    

        public async Task DisconnectAsync()
        {
            try
            {
                await _client.DisconnectAsync();
                _logger.LogInformation("Disconnected from MQTT Broker.");
            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError(ex, "An error occurred while disconnecting from the MQTT broker.");
                throw new AppException("An error occurred while disconnecting from the MQTT broker. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while disconnecting from the MQTT broker.");
                throw new AppException("An unexpected error occurred while disconnecting from the MQTT broker. Please try again later.");
            }
        }

        public async Task PublishAsync(string topic, string message)
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(message))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag()
                .Build();

            try
            {
                await _client.PublishAsync(mqttMessage);
                _logger.LogInformation("Published to {Topic}: {Message}", topic, message);
            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError(ex, "An error occurred while publishing to the MQTT broker.");
                throw new AppException("An error occurred while publishing to the MQTT broker. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while publishing to the MQTT broker.");
                throw new AppException("An unexpected error occurred while publishing to the MQTT broker. Please try again later.");
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f =>
                    f.WithTopic(topic)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce))
                .Build();

            try
            {
                await _client.SubscribeAsync(subscribeOptions);
                _logger.LogInformation("Subscribed to {Topic}", topic);
            }
            catch (MqttCommunicationException ex)
            {
                _logger.LogError(ex, "An error occurred while subscribing to the MQTT broker.");
                throw new AppException("An error occurred while subscribing to the MQTT broker. Please try again later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while subscribing to the MQTT broker.");
                throw new AppException("An unexpected error occurred while subscribing to the MQTT broker. Please try again later.");
            }
        }
    
} 