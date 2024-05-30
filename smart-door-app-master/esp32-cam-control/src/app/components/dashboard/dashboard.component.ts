import { Component, ViewChild, ElementRef, OnInit } from '@angular/core';
import { WebSocketService } from '../../services/websocket.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
  @ViewChild('imageElement', { static: false }) imageElement!: ElementRef;

  constructor(private websocketService: WebSocketService) { }

  ngOnInit(): void {
    this.websocketService.registerBinaryHandler((data: ArrayBuffer) => {
      const blob = new Blob([data], { type: 'image/jpeg' });
      const url = URL.createObjectURL(blob);
      const img: HTMLImageElement = this.imageElement.nativeElement;
      img.src = url;
    });
  }

  handleCommand(command: string): void {
    console.log(`Camera command: ${command}`);
    this.websocketService.send('ClientWantsToSeeStream', { Topic: 'camera/control', Command: command });
  }
}
