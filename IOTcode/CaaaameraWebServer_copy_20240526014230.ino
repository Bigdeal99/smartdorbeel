#include "esp_camera.h"
#include <WiFi.h>
#include <WebSocketsClient.h>
#include <NTPClient.h>
#include <WiFiUdp.h>
#include <PubSubClient.h>
#include "time.h"


// Replace with your network credentials
const char* ssid = "Marcelo Hani";
const char* password = "12312312";


// WebSocket server details
const char* websocket_server_host = "smartdoorbell-bd95d83b892f.herokuapp.com";
const int websocket_server_port = 443;
const char* websocket_server_path = "/";


// MQTT broker details
const char* mqtt_server = "mqtt.flespi.io";
const int mqtt_port = 1883;
const char* mqtt_client_id = "ESP32Client";
const char* mqtt_user = "zomufnJ4kljspMzkeTjAf38E9gfaMAp7Qvd1u3QboArEtJnUTrfkOYke86fYSeu8";
const char* mqtt_password = "";
const char* topic_control = "camera/control";
const char* topic_notifications = "outside/notifications";
const char* topic_status = "camer/status";



// WebSocket client
WebSocketsClient webSocket;
WiFiClient espClient;
PubSubClient mqttClient(espClient);


// Camera settings
#define CAMERA_MODEL_AI_THINKER
#include "camera_pins.h"


// IoT Button
const int buttonPin = 4;
volatile bool buttonPressed = false;
bool streaming = false;


// NTP Client to get time
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 0, 60000);


void IRAM_ATTR onButtonPress() {
    buttonPressed = true;
}


void setup() {
    Serial.begin(115200);


    // Setup button pin
    pinMode(buttonPin, INPUT_PULLUP);
    attachInterrupt(buttonPin, onButtonPress, FALLING);


    // Connect to Wi-Fi
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED) {
        delay(1000);
        Serial.println("Connecting to WiFi...");
    }
    Serial.println("Connected to WiFi");


    // Initialize the NTP client
    timeClient.begin();
    while(!timeClient.update()) {
        timeClient.forceUpdate();
    }
    Serial.println("NTP Client Initialized");


    // Initialize the camera
    camera_config_t config;
    config.ledc_channel = LEDC_CHANNEL_0;
    config.ledc_timer = LEDC_TIMER_0;
    config.pin_d0 = Y2_GPIO_NUM;
    config.pin_d1 = Y3_GPIO_NUM;
    config.pin_d2 = Y4_GPIO_NUM;
    config.pin_d3 = Y5_GPIO_NUM;
    config.pin_d4 = Y6_GPIO_NUM;
    config.pin_d5 = Y7_GPIO_NUM;
    config.pin_d6 = Y8_GPIO_NUM;
    config.pin_d7 = Y9_GPIO_NUM;
    config.pin_xclk = XCLK_GPIO_NUM;
    config.pin_pclk = PCLK_GPIO_NUM;
    config.pin_vsync = VSYNC_GPIO_NUM;
    config.pin_href = HREF_GPIO_NUM;
    config.pin_sccb_sda = SIOD_GPIO_NUM;
    config.pin_sccb_scl = SIOC_GPIO_NUM;
    config.pin_pwdn = PWDN_GPIO_NUM;
    config.pin_reset = RESET_GPIO_NUM;
    config.xclk_freq_hz = 20000000;
    config.pixel_format = PIXFORMAT_JPEG;


    if (psramFound()) {
        config.frame_size = FRAMESIZE_VGA;
        config.jpeg_quality = 10;
        config.fb_count = 2;
    } else {
        config.frame_size = FRAMESIZE_QVGA;
        config.jpeg_quality = 12;
        config.fb_count = 1;
    }


    // Camera init
    esp_err_t err = esp_camera_init(&config);
    if (err != ESP_OK) {
        Serial.printf("Camera init failed with error 0x%x", err);
        return;
    }


    // Initialize WebSocket
    webSocket.beginSSL(websocket_server_host, websocket_server_port, websocket_server_path);
    webSocket.onEvent(webSocketEvent);


    // Initialize MQTT
    mqttClient.setServer(mqtt_server, mqtt_port);
    mqttClient.setCallback(mqttCallback);
    while (!mqttClient.connected()) {
        Serial.println("Connecting to MQTT...");
        if (mqttClient.connect(mqtt_client_id, mqtt_user, mqtt_password)) {
            Serial.println("Connected to MQTT");
            mqttClient.subscribe(topic_control);
        } else {
            delay(5000);
        }
    }
}


void loop() {
    webSocket.loop();
    mqttClient.loop();
    // Handle button press for picture capture and upload
    if (buttonPressed) {
        buttonPressed = false;
        captureAndSendImage();
    }
    // Regularly send frame to WebSocket if streaming
    if (streaming) {
        static unsigned long lastStreamTime = 0;
        if (millis() - lastStreamTime > 100) { // Adjust the interval as necessary
            lastStreamTime = millis();
            captureAndSendFrame();
        }
    }
}


void webSocketEvent(WStype_t type, uint8_t *payload, size_t length) {
    switch (type) {
        case WStype_DISCONNECTED:
            Serial.println("WebSocket Disconnected!");
            break;
        case WStype_CONNECTED:
            Serial.println("WebSocket Connected!");
            break;
        case WStype_TEXT:
            Serial.printf("WebSocket Text: %s\n", payload);
            break;
        case WStype_BIN:
            Serial.printf("WebSocket Binary data length: %u\n", length);
            break;
        case WStype_PING:
            Serial.println("WebSocket Ping!");
            break;
        case WStype_PONG:
            Serial.println("WebSocket Pong!");
            break;
    }
}


void mqttCallback(char* topic, byte* payload, unsigned int length) {
    String messageTemp;
    for (unsigned int i = 0; i < length; i++) {
        messageTemp += (char)payload[i];
    }


    Serial.print("Message arrived [");
    Serial.print(topic);
    Serial.print("] ");
    Serial.println(messageTemp);


    if (String(topic) == topic_control) {
        if (messageTemp == "start") {
            // Handle start streaming
            streaming = true;
            mqttClient.publish(topic_status, "Stream Started");
            Serial.println("Start streaming command received");
        } else if (messageTemp == "stop") {
            // Handle stop streaming
            streaming = false;
            mqttClient.publish(topic_status, "Stream Stoped");
            Serial.println("Stop streaming command received");
        }
    }
}


void captureAndSendFrame() {
    if (WiFi.status() == WL_CONNECTED) {
        camera_fb_t * fb = NULL;
        fb = esp_camera_fb_get();
        if (!fb) {
            Serial.println("Camera capture failed");
            return;
        }


        if (webSocket.isConnected()) {
         
          uint8_t header[4] = {'V', 'I', 'D', 'F'};
          size_t totalLength = sizeof(header) + fb->len;
            uint8_t *buffer = new uint8_t[totalLength];
            memcpy(buffer, header, sizeof(header));
            memcpy(buffer + sizeof(header), fb->buf, fb->len);
            webSocket.sendBIN(buffer, totalLength);
            delete[] buffer;
        }


        esp_camera_fb_return(fb);
    } else {
        Serial.println("WiFi not connected, cannot stream.");
    }
}


void captureAndSendImage() {
    camera_fb_t * fb = NULL;
    fb = esp_camera_fb_get();
    if (!fb) {
        Serial.println("Camera capture failed");
        return;
    }


    // Get the current time
    timeClient.update();
    time_t rawTime = timeClient.getEpochTime();
    struct tm * timeInfo = localtime(&rawTime);


    char buffer[16];
    strftime(buffer, sizeof(buffer), "%m%d%H%M", timeInfo);
    String timeString = String(buffer);


    // Send image to WebSocket
    if (webSocket.isConnected()) {
        uint8_t header[4] = {'I', 'M', 'G', 'F'};
        size_t totalLength = sizeof(header) + fb->len;
        uint8_t *buffer = new uint8_t[totalLength];
        memcpy(buffer, header, sizeof(header));
        memcpy(buffer + sizeof(header), fb->buf, fb->len);
        webSocket.sendBIN(buffer, totalLength);
        delete[] buffer;
    }


    esp_camera_fb_return(fb);


    // Send event after sending the image
    mqttClient.publish(topic_notifications, "Somebody rang the bell, Image captured and sent");
}


