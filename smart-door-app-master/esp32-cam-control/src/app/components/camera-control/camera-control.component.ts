import { Component, ViewChild, ElementRef, OnInit } from '@angular/core';
import { WebSocketService } from '../../services/websocket.service';

@Component({
  selector: 'app-camera-control',
  templateUrl: './camera-control.component.html',
  styleUrls: ['./camera-control.component.scss'],
})
export class CameraControlComponent implements OnInit {
  @ViewChild('videoElement', { static: false }) videoElement!: ElementRef;  // Use definite assignment assertion

  constructor(private websocketService: WebSocketService) { }

  ngOnInit(): void {
    this.websocketService.registerBinaryHandler((data: ArrayBuffer) => {
      const blob = new Blob([data], { type: 'video/webm' });
      const url = URL.createObjectURL(blob);
      const video: HTMLVideoElement = this.videoElement.nativeElement;
      video.src = url;
      video.play();
    });
  }

  handleCommand(command: string): void {
    console.log(`Camera command: ${command}`);
    this.websocketService.send('ClientWantsToSeeStream', { Topic: 'camera/control', Command: command });
  }
}
