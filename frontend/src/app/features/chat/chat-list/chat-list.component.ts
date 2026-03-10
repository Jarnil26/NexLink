import { Component, OnInit, OnDestroy, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { ChatService } from '../../../core/services/chat.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-chat-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-list.component.html',
  styleUrl: './chat-list.component.scss'
})
export class ChatListComponent implements OnInit, OnDestroy {
  chats: any[] = [];
  connections: any[] = [];
  selectedChatId: string | null = null;
  selectedUserId: string | null = null;
  searchResults: any[] | null = null;
  searchQuery: string = '';
  searchLoading: boolean = false;
  private searchSubject = new Subject<string>();
  private messagesSub?: Subscription;
  private onlineSub?: Subscription;
  private offlineSub?: Subscription;
  private currentUser: any;

  @Output() chatSelected = new EventEmitter<string>();
  @Output() userSelected = new EventEmitter<string>();
  @Input() showOnlySearch: boolean = false;

  constructor(
    private chatService: ChatService,
    private signalRService: SignalrService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.loadData();
    setInterval(() => this.loadData(), 30000);

    // Instagram-style debounced search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query.trim()) {
          this.searchLoading = false;
          return of(null);
        }
        this.searchLoading = true;
        return this.chatService.searchUser(query).pipe(
          catchError((err: any) => {
            console.error('Search error:', err);
            this.searchLoading = false;
            return of([]);
          })
        );
      })
    ).subscribe({
      next: (results: any) => {
        this.searchResults = results;
        this.searchLoading = false;
      },
      error: (err: any) => {
        console.error('Fatal search error:', err);
        this.searchLoading = false;
      }
    });

    this.currentUser = this.authService.getCurrentUser();

    // Listen for incoming messages to update unread badge and preview
    this.messagesSub = this.signalRService.messages$.subscribe((msg: any) => {
      const chat = this.chats.find(c => c.id === msg.chatId);
      if (chat) {
        // Update the latest message preview
        chat.lastMessage = {
          content: msg.content,
          sentAt: msg.sentAt,
          senderId: msg.senderId
        };
        // If it's not the currently open chat, and sent by someone else, increment unread
        if (this.selectedChatId !== msg.chatId && msg.senderId !== this.currentUser.id) {
          chat.unreadMessageCount = (chat.unreadMessageCount || 0) + 1;
        }
        
        // Move chat to top of list
        this.chats = this.chats.filter(c => c.id !== msg.chatId);
        this.chats.unshift(chat);
      } else {
        // A new chat might have been started, reload the list
        this.loadChats();
      }
    });

    this.onlineSub = this.signalRService.userOnline$.subscribe(userId => {
      this.updateUserOnlineStatus(userId, true);
    });

    this.offlineSub = this.signalRService.userOffline$.subscribe(userId => {
      this.updateUserOnlineStatus(userId, false);
    });
  }

  private updateUserOnlineStatus(userId: string, isOnline: boolean) {
    // Update in chats
    this.chats.forEach(chat => {
      const other = this.getOtherParticipant(chat);
      if (other.id === userId) {
        other.isOnline = isOnline;
      }
    });

    // Update in connections
    this.connections.forEach(conn => {
      if (conn.id === userId) {
        conn.isOnline = isOnline;
      }
    });

    // Update in search results
    if (this.searchResults) {
      this.searchResults.forEach(user => {
        if (user.id === userId) {
          user.isOnline = isOnline;
        }
      });
    }
  }

  ngOnDestroy() {
    if (this.messagesSub) {
      this.messagesSub.unsubscribe();
    }
    if (this.onlineSub) {
      this.onlineSub.unsubscribe();
    }
    if (this.offlineSub) {
      this.offlineSub.unsubscribe();
    }
  }

  loadData() {
    this.loadChats();
    this.loadConnections();
  }

  loadChats() {
    this.chatService.getUserChats().subscribe({
      next: (chats) => this.chats = chats,
      error: (err: any) => console.error('Error loading chats:', err)
    });
  }

  loadConnections() {
    this.chatService.getConnections().subscribe({
      next: (conns) => {
        // Filter: Only show connections that DON'T have a chat room yet
        this.connections = conns.filter(c => !this.chats.some(chat => 
          chat.participants.some((p: any) => p.id === c.id)
        ));
      },
      error: (err: any) => console.error('Error loading connections:', err)
    });
  }

  onSearchChange() {
    this.searchSubject.next(this.searchQuery);
  }

  sendConnectionRequest(userId: string) {
    this.chatService.sendConnectionRequest(userId).subscribe({
      next: () => {
        // Update local search result UI immediately
        const user = this.searchResults?.find(u => u.id === userId);
        if (user) user.connectionStatus = 'Pending';
        
        // Show success and refresh data
        alert('Connection request sent!');
        this.clearSearch();
        this.loadData();
      },
      error: (err: any) => alert(err.error?.message || 'Failed to send request')
    });
  }

  sendChatRequest(userId: string) {
    this.chatService.sendChatRequest(userId).subscribe({
      next: () => {
        // Update local status immediately
        const user = this.searchResults?.find(u => u.id === userId);
        if (user) user.chatRequestStatus = 'Pending';
        const connUser = this.connections.find(u => u.id === userId);
        if (connUser) connUser.chatRequestStatus = 'Pending'; // For connections list too
      },
      error: (err: any) => alert(err.error?.message || 'Failed to send chat request')
    });
  }

  clearSearch() {
    this.searchQuery = '';
    this.searchResults = null;
  }

  selectChat(chatId: string) {
    this.selectedChatId = chatId;
    this.selectedUserId = null;
    
    // Clear local unread badge and API call
    const chat = this.chats.find(c => c.id === chatId);
    if (chat && chat.unreadMessageCount > 0) {
      chat.unreadMessageCount = 0;
      this.chatService.markAsSeen(chatId).subscribe({
        next: () => console.log('Messages marked as seen'),
        error: (err: any) => console.error('Error marking seen:', err)
      });
    }

    this.chatSelected.emit(chatId);
  }

  selectUser(userId: string) {
    this.selectedUserId = userId;
    this.selectedChatId = null;
    this.userSelected.emit(userId);
  }

  getOtherParticipant(chat: any) {
    const currentUser = JSON.parse(localStorage.getItem('chat_user') || '{}');
    return chat.participants.find((p: any) => p.id !== currentUser.id) || chat.participants[0];
  }

  getLatestMessageTime(chat: any) {
    if (!chat.messages || chat.messages.length === 0) return '';
    const date = new Date(chat.messages[chat.messages.length - 1].sentAt);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
}
