import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private apiUrl = `${environment.apiUrl}/chat`;
  private connectionUrl = `${environment.apiUrl}/connection`;
  private chatRequestUrl = `${environment.apiUrl}/chat-request`;
  private userUrl = `${environment.apiUrl}/user`;

  constructor(private http: HttpClient) { }

  // --- Chat Rooms & Messages ---
  getUserChats(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/rooms`);
  }

  getChatMessages(chatId: string, page: number = 1): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${chatId}/messages?page=${page}`);
  }

  markAsSeen(chatId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/mark-seen/${chatId}`, {});
  }

  getUserById(userId: string): Observable<any> {
    return this.http.get<any>(`${this.userUrl}/id/${userId}`);
  }

  searchUser(query: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.userUrl}/search?q=${query}`);
  }

  // --- Connection Requests ---
  getConnectionStatus(targetUserId: string): Observable<{ status: string }> {
    return this.http.get<{ status: string }>(`${this.connectionUrl}/status/${targetUserId}`);
  }

  sendConnectionRequest(targetUserId: string): Observable<any> {
    return this.http.post<any>(`${this.connectionUrl}/request`, { targetUserId });
  }

  respondToConnection(requestId: string, status: string): Observable<any> {
    return this.http.post<any>(`${this.connectionUrl}/respond`, { requestId, status });
  }

  getIncomingConnections(): Observable<any[]> {
    return this.http.get<any[]>(`${this.connectionUrl}/incoming`);
  }

  getConnections(): Observable<any[]> {
    return this.http.get<any[]>(`${this.connectionUrl}/connections`);
  }

  // --- Chat Requests ---
  getChatRequestStatus(targetUserId: string): Observable<{ status: string }> {
    return this.http.get<{ status: string }>(`${this.chatRequestUrl}/status/${targetUserId}`);
  }

  sendChatRequest(targetUserId: string): Observable<any> {
    return this.http.post<any>(`${this.chatRequestUrl}/request`, { targetUserId });
  }

  respondToChatRequest(requestId: string, status: string): Observable<any> {
    return this.http.post<any>(`${this.chatRequestUrl}/respond`, { requestId, status });
  }

  getIncomingChatRequests(): Observable<any[]> {
    return this.http.get<any[]>(`${this.chatRequestUrl}/incoming`);
  }
}
