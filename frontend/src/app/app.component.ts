import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { SignalrService } from './core/services/signalr.service';
import { Subscription } from 'rxjs';
import { PushNotificationService } from './services/push-notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'ChatPlatform';
  private authSubscription!: Subscription;

  constructor(
    private authService: AuthService,
    private signalrService: SignalrService,
    private pushNotificationService: PushNotificationService
  ) {}

  ngOnInit() {
    this.authSubscription = this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.signalrService.startConnection();
        this.applyTheme(user.theme || 'system');
        this.pushNotificationService.subscribeToNotifications();
      } else {
        this.signalrService.stopConnection();
        this.applyTheme('system');
      }
    });
  }

  applyTheme(theme: string) {
    const body = document.body;
    body.classList.remove('light-theme', 'dark-theme');
    
    if (theme === 'system') {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      body.classList.add(prefersDark ? 'dark-theme' : 'light-theme');
    } else {
      body.classList.add(`${theme}-theme`);
    }
  }

  ngOnDestroy() {
    if (this.authSubscription) {
      this.authSubscription.unsubscribe();
    }
  }
}
