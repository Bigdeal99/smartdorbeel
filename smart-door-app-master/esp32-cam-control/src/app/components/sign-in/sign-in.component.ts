import { Component } from '@angular/core';
import { WebSocketService } from '../../services/websocket.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sign-in',
  templateUrl: './sign-in.component.html',
  styleUrls: ['./sign-in.component.scss'],
})
export class SignInComponent {
  name: string = '';

  constructor(private websocketService: WebSocketService, private router: Router) { }

  handleSignIn(): void {
    console.log('Sign In button clicked');
    this.websocketService.send('ClientWantsToSignInWithName', { Name: this.name });
    this.websocketService.registerHandler('ServerSendsInfoToClient', (data) => {
      if (data.Message) {
        this.router.navigate(['/dashboard']);
      }
    });
  }
}
