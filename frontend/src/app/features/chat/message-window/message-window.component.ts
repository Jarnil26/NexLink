import { Component, Input, OnInit, OnChanges, SimpleChanges, ViewChild, ElementRef, AfterViewChecked, OnDestroy, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../../core/services/chat.service';
import { SignalrService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserService } from '../../../core/services/user.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-message-window',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './message-window.component.html',
  styleUrl: './message-window.component.scss'
})
export class MessageWindowComponent implements OnInit, OnChanges, AfterViewChecked, OnDestroy {
  @Input() chatId: string | null = null;
  @Input() userId: string | null = null; // Target User ID if no chat room yet
  @Output() closeChat = new EventEmitter<void>();
  
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;
  @ViewChild('messageInput') private messageInput!: ElementRef;

  private connectionSub?: Subscription;
  private messagesSub?: Subscription;
  private typingSub?: Subscription;
  private readSub?: Subscription;
  private onlineSub?: Subscription;
  private offlineSub?: Subscription;

  chat: any;
  messages: any[] = [];
  newMessage: string = '';
  currentUser: any;
  
  // Permission States
  isLocked: boolean = true;
  lockStatus: 'NoConnection' | 'ConnectionPending' | 'NoChatRequest' | 'ChatPending' | 'Accepted' = 'NoConnection';
  otherUser: any = null;

  constructor(
    private chatService: ChatService,
    private signalRService: SignalrService,
    private authService: AuthService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.currentUser = this.authService.getCurrentUser();
    this.messagesSub = this.signalRService.messages$.subscribe((msg: any) => {
      if (msg.chatId === this.chatId) {
        // Prevent duplicate messages
        const exists = this.messages.some(m => m.id === msg.id);
        if (!exists) {
          this.messages.push(msg);
          this.scrollToBottom();

          // If we are looking at this chat and receiving a message from someone else, mark it as seen
          if (msg.senderId !== this.currentUser.id) {
             this.chatService.markAsSeen(this.chatId!).subscribe();
          }
        }
      }
    });

    this.readSub = this.signalRService.messageRead$.subscribe((data: any) => {
      if (data.chatId === this.chatId && data.userId !== this.currentUser.id) {
        // The other person read our messages
        this.messages.forEach(m => {
          if (m.senderId === this.currentUser.id && !m.isRead) {
            m.isRead = true;
            m.readAt = data.readAt || new Date().toISOString();
          }
        });
      }
    });

    this.onlineSub = this.signalRService.userOnline$.subscribe(userId => {
      if (this.otherUser && this.otherUser.id === userId) {
        this.otherUser.isOnline = true;
      }
    });

    this.offlineSub = this.signalRService.userOffline$.subscribe(userId => {
      if (this.otherUser && this.otherUser.id === userId) {
        this.otherUser.isOnline = false;
      }
    });

    this.typingSub = this.signalRService.typing$.subscribe((data: any) => {
      if (data.chatId === this.chatId && data.userId !== this.currentUser.id) {
          // Handle typing indicator
      }
    });

    this.connectionSub = this.signalRService.connectionStatus$.subscribe(status => {
      if (status === 'Connected' && this.chatId) {
        this.signalRService.joinChat(this.chatId).catch(err => console.error('Error joining chat:', err));
      }
    });
  }

  ngOnDestroy() {
    if (this.connectionSub) {
      this.connectionSub.unsubscribe();
    }
    if (this.messagesSub) {
      this.messagesSub.unsubscribe();
    }
    if (this.typingSub) {
      this.typingSub.unsubscribe();
    }
    if (this.readSub) {
      this.readSub.unsubscribe();
    }
    if (this.onlineSub) {
      this.onlineSub.unsubscribe();
    }
    if (this.offlineSub) {
      this.offlineSub.unsubscribe();
    }
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['chatId'] || changes['userId']) {
      this.checkPermissionAndLoad();
    }
  }

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  checkPermissionAndLoad() {
    if (this.chatId) {
      this.loadChat();
      this.isLocked = false; 
      this.lockStatus = 'Accepted';
      this.focusInput();
    } else if (this.userId) {
      // If we only have userId, it's a potential chat
      this.loadOtherUser();
      this.isLocked = true;
      this.determineLockStatus();
    }
  }

  loadOtherUser() {
    this.chatService.getUserById(this.userId!).subscribe({
      next: (user) => this.otherUser = user,
      error: (err) => console.error('Error loading user:', err)
    });
  }

  determineLockStatus() {
    if (!this.userId) return;

    // 1. Check Connection Status
    this.chatService.getConnectionStatus(this.userId).subscribe({
      next: (connRes) => {
        if (connRes.status === 'Accepted') {
          // 2. Check Chat Request Status
          this.chatService.getChatRequestStatus(this.userId!).subscribe({
            next: (chatRes) => {
              if (chatRes.status === 'Accepted') {
                this.lockStatus = 'Accepted';
                this.isLocked = false;
                // If we reach here, we should actually be loading the chat room
                // This happens if the user refreshes on a connection but chat room was just created
                this.loadChatByParticipants();
              } else if (chatRes.status === 'Pending') {
                this.lockStatus = 'ChatPending';
                this.isLocked = true;
              } else {
                this.lockStatus = 'NoChatRequest';
                this.isLocked = true;
              }
            }
          });
        } else if (connRes.status === 'Pending') {
          this.lockStatus = 'ConnectionPending';
          this.isLocked = true;
        } else {
          this.lockStatus = 'NoConnection';
          this.isLocked = true;
        }
      },
      error: (err) => console.error('Error getting connection status:', err)
    });
  }

  loadChatByParticipants() {
    this.chatService.getUserChats().subscribe(chats => {
      const chat = chats.find(c => c.participants.some((p: any) => p.id === this.userId));
      if (chat) {
        this.chatId = chat.id;
        this.loadChat();
      }
    });
  }

  loadChat() {
    this.chatService.getUserChats().subscribe(chats => {
      this.chat = chats.find(c => c.id === this.chatId);
      this.otherUser = this.chat?.participants.find((p: any) => p.id !== this.currentUser.id);
      this.loadMessages();
      
      // Also join immediately if already connected. 
      // Note: connectionStatus$ subscription handles the delayed connection scenario.
      if (this.signalRService.connectionStatus$.value === 'Connected') {
        this.signalRService.joinChat(this.chatId!).catch(err => console.error('Error joining chat:', err));
      }
    });
  }

  loadMessages() {
    this.chatService.getChatMessages(this.chatId!).subscribe(msgs => {
      this.messages = msgs;
      this.scrollToBottom();
    });
  }

  sendMessage() {
    if (!this.newMessage.trim() || !this.chatId) return;

    const content = this.newMessage;
    this.signalRService.sendMessage(this.chatId, content).then(() => {
        this.newMessage = '';
    }).catch(err => {
      console.error('Failed to send message:', err);
      alert(typeof err === 'string' ? err : 'Failed to send message. Please check your connection.');
    });
  }

  sendChatRequest() {
    if (!this.userId) return;
    this.chatService.sendChatRequest(this.userId).subscribe({
      next: () => {
        alert('Chat request sent!');
        this.lockStatus = 'ChatPending';
      },
      error: (err) => alert(err.error?.message || 'Failed to send chat request')
    });
  }

  sendConnectionRequest() {
    if (!this.userId) return;
    this.chatService.sendConnectionRequest(this.userId).subscribe({
      next: () => {
        alert('Connection request sent!');
        this.lockStatus = 'ConnectionPending';
      },
      error: (err) => alert(err.error?.message || 'Failed to send request')
    });
  }

  blockUser() {
    if (!this.otherUser?.id) return;
    if (confirm(`Are you sure you want to block ${this.otherUser.username}? This will remove them from your active chats.`)) {
      this.userService.blockUser(this.otherUser.id).subscribe({
        next: () => {
          alert('User has been blocked.');
          this.close(); // Close the chat window automatically
        },
        error: (err) => alert('Failed to block user. They might already be blocked.')
      });
    }
  }

  onTyping() {
    if (this.chatId) {
      this.signalRService.sendTyping(this.chatId);
    }
  }

  scrollToBottom(): void {
    try {
      this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }

  close() {
    this.closeChat.emit();
  }

  getSeenStatus(): string {
    const lastReadMessage = [...this.messages]
      .reverse()
      .find(m => m.senderId === this.currentUser.id && m.isRead);

    if (!lastReadMessage || !lastReadMessage.readAt) return '';

    const readAt = new Date(lastReadMessage.readAt);
    const now = new Date();
    const diffMs = now.getTime() - readAt.getTime();
    const diffMin = Math.floor(diffMs / 60000);

    if (diffMin < 1) return 'Seen just now';
    if (diffMin < 60) return `Seen ${diffMin}m ago`;
    
    const diffHours = Math.floor(diffMin / 60);
    if (diffHours < 24) return `Seen ${diffHours}h ago`;

    return `Seen ${readAt.toLocaleDateString()}`;
  }

  private focusInput() {
    setTimeout(() => {
      if (this.messageInput) {
        this.messageInput.nativeElement.focus();
      }
    }, 500); // Wait for transition/animation
  }
}
