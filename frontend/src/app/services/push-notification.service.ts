import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  constructor(private http: HttpClient, private swPush: SwPush) {}

  subscribeToNotifications() {
    this.http.get<{publicKey: string}>(`${environment.apiUrl}/api/push/vapidPublicKey`).subscribe(res => {
      this.swPush.requestSubscription({
        serverPublicKey: res.publicKey
      })
      .then(sub => this.sendSubscriptionToBackend(sub))
      .catch(err => console.error('Could not subscribe to notifications', err));
    });
  }

  private sendSubscriptionToBackend(subscription: PushSubscription) {
    this.http.post(`${environment.apiUrl}/api/push/subscribe`, subscription).subscribe();
  }
}
