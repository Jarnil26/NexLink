import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `http://localhost:5142/api/user`;

  constructor(private http: HttpClient) {}

  getProfile(): Observable<any> {
    return this.http.get(`${this.apiUrl}/me`);
  }

  updateProfile(data: { username: string }): Observable<any> {
    return this.http.put(`${this.apiUrl}/update-profile`, data);
  }

  uploadAvatar(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/upload-avatar`, formData);
  }

  updateTheme(theme: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/theme`, { theme });
  }

  getRequestStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/request-stats`);
  }

  changePassword(data: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/change-password`, data);
  }
}
