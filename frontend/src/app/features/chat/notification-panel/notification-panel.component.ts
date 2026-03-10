import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService } from '../../../core/services/chat.service';

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss'
})
export class NotificationPanelComponent implements OnInit {
  connectionRequests: any[] = [];
  chatRequests: any[] = [];

  constructor(private chatService: ChatService) {}

  ngOnInit() {
    this.refreshRequests();
  }

  refreshRequests() {
    this.chatService.getIncomingConnections().subscribe(res => this.connectionRequests = res);
    this.chatService.getIncomingChatRequests().subscribe(res => this.chatRequests = res);
  }

  respondToConnection(requestId: string, status: string) {
    this.chatService.respondToConnection(requestId, status).subscribe({
      next: () => {
        alert(`Connection ${status.toLowerCase()}`);
        this.refreshRequests();
      },
      error: (err) => alert(err.error?.message || 'Action failed')
    });
  }

  respondToChat(requestId: string, status: string) {
    this.chatService.respondToChatRequest(requestId, status).subscribe({
      next: () => {
        alert(`Chat Request ${status.toLowerCase()}`);
        this.refreshRequests();
      },
      error: (err) => alert(err.error?.message || 'Action failed')
    });
  }
}
