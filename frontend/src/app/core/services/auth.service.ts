import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private tokenKey = 'chat_token';
  private currentUserKey = 'chat_user';

  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    this.loadUserFromStorage();
  }

  login(credentials: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => this.handleAuthentication(response))
    );
  }

  register(userData: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, userData).pipe(
      tap(response => this.handleAuthentication(response))
    );
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.currentUserKey);
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): any {
    return this.currentUserSubject.value;
  }

  updateUser(user: any) {
    if (user) {
      localStorage.setItem(this.currentUserKey, JSON.stringify(user));
      this.currentUserSubject.next(user);
    }
  }

  private handleAuthentication(response: any) {
    if (response && response.token) {
      localStorage.setItem(this.tokenKey, response.token);
      this.updateUser(response.user);
    }
  }

  private loadUserFromStorage() {
    const userStr = localStorage.getItem(this.currentUserKey);
    if (userStr) {
      this.currentUserSubject.next(JSON.parse(userStr));
    }
  }
}
