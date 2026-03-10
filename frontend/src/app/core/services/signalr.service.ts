import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection: signalR.HubConnection | null = null;
  private hubUrl = environment.hubUrl;

  private messageSubject = new Subject<any>();
  public messages$ = this.messageSubject.asObservable();

  private userOnlineSubject = new Subject<string>();
  public userOnline$ = this.userOnlineSubject.asObservable();

  private userOfflineSubject = new Subject<string>();
  public userOffline$ = this.userOfflineSubject.asObservable();

  private typingSubject = new Subject<{chatId: string, userId: string}>();
  public typing$ = this.typingSubject.asObservable();

  private messageReadSubject = new Subject<{chatId: string, userId: string}>();
  public messageRead$ = this.messageReadSubject.asObservable();

  private connectionRequestSubject = new Subject<any>();
  public connectionRequest$ = this.connectionRequestSubject.asObservable();

  private chatRequestSubject = new Subject<any>();
  public chatRequest$ = this.chatRequestSubject.asObservable();

  public connectionStatus$ = new BehaviorSubject<string>('Disconnected');

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService
  ) { }

  public startConnection() {
    const token = this.authService.getToken();
    if (!token) return;

    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR connection started');
        this.connectionStatus$.next('Connected');
        this.registerHandlers();
      })
      .catch((err: any) => {
        console.error('Error while starting SignalR connection: ' + err);
        this.connectionStatus$.next('Error');
      });

    this.hubConnection.onreconnecting(() => this.connectionStatus$.next('Reconnecting'));
    this.hubConnection.onreconnected(() => this.connectionStatus$.next('Connected'));
    this.hubConnection.onclose(() => this.connectionStatus$.next('Disconnected'));
  }

  public stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  private registerHandlers() {
    if (!this.hubConnection) return;

    this.hubConnection.on('receive_message', (message: any) => {
      console.log('SignalR: Received message', message);
      this.messageSubject.next(message);
      
      // Show browser notification if not focusing this chat
      this.notificationService.showNotification(`New message from ${message.senderUsername}`, {
        body: message.content,
        data: { chatId: message.chatId }
      });
    });

    this.hubConnection.on('receive_connection_request', (request: any) => {
      console.log('SignalR: Received connection request', request);
      this.connectionRequestSubject.next(request);
      this.notificationService.showNotification('New Connection Request', {
        body: `${request.fromUsername} wants to connect with you.`
      });
    });

    this.hubConnection.on('receive_chat_request', (request: any) => {
      console.log('SignalR: Received chat request', request);
      this.chatRequestSubject.next(request);
      this.notificationService.showNotification('New Chat Request', {
        body: `${request.fromUsername} wants to start a chat.`
      });
    });

    this.hubConnection.on('user_online', (userId: string) => {
      this.userOnlineSubject.next(userId);
    });

    this.hubConnection.on('user_offline', (userId: string) => {
      this.userOfflineSubject.next(userId);
    });

    this.hubConnection.on('user_typing', (data: any) => {
      this.typingSubject.next(data);
    });

    this.hubConnection.on('message_read', (data: any) => {
      this.messageReadSubject.next(data);
    });
  }

  public sendMessage(chatId: string, content: string) {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
        return Promise.reject('No connection');
    }
    return this.hubConnection.invoke('SendMessage', { chatId, content });
  }

  public sendTyping(chatId: string) {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
        return Promise.reject('No connection');
    }
    return this.hubConnection.invoke('Typing', chatId);
  }

  public joinChat(chatId: string) {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
        return Promise.reject('No connection');
    }
    return this.hubConnection.invoke('JoinChat', chatId);
  }

  public markAsRead(chatId: string) {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
        return Promise.reject('No connection');
    }
    return this.hubConnection.invoke('MarkAsRead', chatId);
  }
}

