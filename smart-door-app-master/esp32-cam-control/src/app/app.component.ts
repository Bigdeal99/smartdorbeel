import { Component, OnInit } from '@angular/core';
import { WebSocketService } from './services/websocket.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  constructor(private websocketService: WebSocketService) { }

  ngOnInit(): void {
    console.log('Initializing WebSocket connection...');
    this.websocketService.connect('ws://127.0.0.1:8181');
  }
}
