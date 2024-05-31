
## Smart Doorbell Project


## Overview

Overview
This project is a smart doorbell system that enhances traditional doorbells by adding features like image capture, video streaming, and mobile notifications. It utilizes an ESP32CAM for capturing images and streaming video, Azure Blob Storage for storing images, and IFTTT for sending notifications. The backend is deployed on Heroku, while the frontend is hosted on Firebase.

## Features
Real-time Video Streaming: View live video feed from the doorbell camera.
Image Capture and Storage: Automatically captures and stores images when the doorbell button is pressed.
Mobile Notifications: Receive notifications on your phone when someone presses the doorbell.
Image Management: View, download, and delete images from the cloud storage.
Remote Access: Monitor and interact with visitors from anywhere using a web interface.

## Architecture

System Components
1. ESP32CAM: Captures images and streams video.
2. Azure Blob Storage: Stores captured images.
3. IFTTT: Sends notifications to the user's phone.
4. WebSocket: Streams video to the Angular frontend.
5. MQTT (flespi): Manages real-time messaging.
6. Backend (Heroku): Handles server-side logic.
7. Frontend (Firebase): Hosts the Angular application.
## Hardware Setup

1. ESP32CAM: Central module for capturing images and video.
2. Button: Triggers the camera to take a picture.
3. Power Management: Ensures stable operation of the ESP32CAM.
## Review the code

Clone the Repository
https://github.com/Bigdeal99/smartdorbeel.git

Note: If you clone the code the frontend would connect with the deployed backend, so no need to run the backend
## Getting Started

ESP32CAM + ESP32microcontoller module
Button and necessary connections

1. Turn on the car and ensure both ESP devices connect to the MQTT broker.
2. Visit the site https://smartdoorbell-24eb8.web.app/sign-in 
3. Enter a name 
4. Hit start or stop button and you are good to go

5. Press the doorbell button to trigger the camera.
6. The ESP32CAM captures an image and streams video to the frontend.
7. A notification is sent to your phone via IFTTT.
8. View the live video feed and manage images through the web interface.
## Contributors

The project was developed by:

Marcelo Hani

Basam Dawi