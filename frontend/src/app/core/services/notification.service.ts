import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  constructor() { }

  async requestPermission(): Promise<boolean> {
    if (!('Notification' in window)) {
      console.warn('This browser does not support desktop notifications');
      return false;
    }

    if (Notification.permission === 'granted') {
      return true;
    }

    if (Notification.permission !== 'denied') {
      const permission = await Notification.requestPermission();
      return permission === 'granted';
    }

    return false;
  }

  showNotification(title: string, options: NotificationOptions = {}) {
    if (Notification.permission === 'granted') {
      const defaultOptions: NotificationOptions = {
        icon: '/assets/icons/icon-192x192.png', // Fallback to a default if not provided
        badge: '/assets/icons/icon-72x72.png',
        silent: false,
        ...options
      };

      const notification = new Notification(title, defaultOptions);

      notification.onclick = (event) => {
        event.preventDefault();
        window.focus();
        notification.close();
      };
    }
  }
}
