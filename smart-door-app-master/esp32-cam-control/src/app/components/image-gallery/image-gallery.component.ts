import { Component, OnInit } from '@angular/core';
import { WebSocketService } from '../../services/websocket.service';
import { BellLog } from "../../interfaces/bell-log";

@Component({
  selector: 'app-image-gallery',
  templateUrl: './image-gallery.component.html',
  styleUrls: ['./image-gallery.component.scss'],
})
export class ImageGalleryComponent implements OnInit {
  images: BellLog[] = [];

  constructor(private websocketService: WebSocketService) { }

  ngOnInit(): void {
    console.log('WebSocket connecting...');
    this.websocketService.connect('ws://localhost:8181');

    this.websocketService.registerHandler('BellLogData', (data: BellLog[]) => {
      console.log('BellLogData received:', data);
      this.images = data;
    });

    this.websocketService.messages$.subscribe((data: BellLog[]) => {
      console.log('Data received:', data);
      this.images = data;
    });

    console.log('Requesting BellLogData...');
    this.websocketService.send('ClientWantsToGetBellLog', {});
  }

  handleDelete(fileName: string): void {
    console.log(`Delete image: ${fileName}`);
    this.websocketService.send('ClientWantsToDeleteSingleLog', { FileName: fileName });
  }

  formatTimestamp(fileName: string): string {
    // Assuming the fileName format is yyyyMMddHHmmss.jpg
    const timestamp = fileName.replace('.jpg', '');
    const year = timestamp.substring(0, 4);
    const month = timestamp.substring(4, 6);
    const day = timestamp.substring(6, 8);
    const hour = timestamp.substring(8, 10);
    const minute = timestamp.substring(10, 12);
    const second = timestamp.substring(12, 14);

    return `${year}-${month}-${day} ${hour}:${minute}:${second}`;
  }
}
