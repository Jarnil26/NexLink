import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChatListComponent } from '../chat-list/chat-list.component';
import { MessageWindowComponent } from '../message-window/message-window.component';
import { NotificationPanelComponent } from '../notification-panel/notification-panel.component';
import { AuthService } from '../../../core/services/auth.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { ChatService } from '../../../core/services/chat.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-chat-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, ChatListComponent, MessageWindowComponent, NotificationPanelComponent],
  templateUrl: './chat-layout.component.html',
  styleUrl: './chat-layout.component.scss'
})
export class ChatLayoutComponent implements OnInit, OnDestroy {
  selectedChatId: string | null = null;
  selectedUserId: string | null = null;
  currentUser: any;
  showNotifications: boolean = false;
  notificationCount: number = 0;
  currentView: 'chats' | 'search' | 'notifications' | 'profile' | 'settings' = 'chats';

  constructor(
    public authService: AuthService,
    private signalRService: SignalrService,
    private chatService: ChatService,
    private notificationService: NotificationService
  ) {}

  ngOnInit() {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
    this.signalRService.startConnection();
    this.updateNotificationCount();
    
    // Request permission for browser notifications
    this.notificationService.requestPermission();
  }

  ngOnDestroy() {
    this.signalRService.stopConnection();
  }

  onChatSelected(chatId: string) {
    this.selectedChatId = chatId;
    this.selectedUserId = null;
    this.showNotifications = false;
  }

  onUserSelected(userId: string) {
    this.selectedUserId = userId;
    this.selectedChatId = null;
    this.showNotifications = false;
  }

  onChatClosed() {
    this.selectedChatId = null;
    this.selectedUserId = null;
  }

  toggleNotifications() {
    this.currentView = this.currentView === 'notifications' ? 'chats' : 'notifications';
    if (this.currentView !== 'notifications') {
      this.updateNotificationCount();
    }
  }

  switchView(view: 'chats' | 'search' | 'notifications' | 'profile' | 'settings') {
    this.currentView = view;
    // On mobile, if switching view, close active chat for clarity
    if (window.innerWidth < 768) {
      this.selectedChatId = null;
      this.selectedUserId = null;
    }
  }

  private updateNotificationCount() {
    this.chatService.getIncomingConnections().subscribe(conns => {
      this.chatService.getIncomingChatRequests().subscribe(chats => {
        this.notificationCount = conns.length + chats.length;
      });
    });
  }

  logout() {
    this.signalRService.stopConnection();
    this.authService.logout();
  }
}
