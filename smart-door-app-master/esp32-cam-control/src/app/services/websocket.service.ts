import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket!: WebSocket;
  private messageQueue: any[] = [];
  private isConnected = false;
  private messageHandlers: { [key: string]: (data: any) => void } = {};

  private messagesSubject = new Subject<any>();
  public messages$ = this.messagesSubject.asObservable();

  constructor() { }

  connect(url: string): void {
    if (this.isConnected) {
      return;
    }

    this.socket = new WebSocket(url);

    this.socket.onopen = () => {
      console.log("WebSocket connected");
      this.isConnected = true;
      this.flushMessageQueue();
    };

    this.socket.onmessage = (event) => {
      if (typeof event.data === "string") {
        const data = JSON.parse(event.data);
        console.log("Message received:", data);
        if (data instanceof Array) {
          this.messagesSubject.next(data);
        } else if (this.messageHandlers[data.eventType]) {
          this.messageHandlers[data.eventType](data);
        }
      } else {
        this.handleBinaryMessage(event.data);
      }
    };

    this.socket.onclose = () => {
      console.log("WebSocket disconnected");
      this.isConnected = false;
    };

    this.socket.onerror = (error) => {
      console.log("WebSocket error:", error);
      this.isConnected = false;
    };
  }

  send(eventType: string, payload: any): void {
    const message = { eventType, ...payload };
    console.log('Sending message:', message);
    if (this.isConnected && this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(message));
    } else {
      console.log('Queueing message:', message);
      this.messageQueue.push(message);
    }
  }

  flushMessageQueue(): void {
    while (this.messageQueue.length > 0) {
      const message = this.messageQueue.shift();
      this.send(message.eventType, message);
    }
  }

  registerHandler(eventType: string, handler: (data: any) => void): void {
    this.messageHandlers[eventType] = handler;
  }

  private handleBinaryMessage(data: ArrayBuffer): void {
    if (this.binaryHandler) {
      this.binaryHandler(data);
    }
  }

  private binaryHandler: ((data: ArrayBuffer) => void) | null = null;

  registerBinaryHandler(handler: (data: ArrayBuffer) => void): void {
    this.binaryHandler = handler;
  }
}
