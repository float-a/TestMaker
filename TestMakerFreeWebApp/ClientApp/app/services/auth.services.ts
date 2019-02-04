import { EventEmitter, Inject, Injectable, PLATFORM_ID } from "@angular/core";
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import 'rxjs/Rx';

@Injectable()
export class AuthService {
    authKey: string = "auth";
    clientId: string = "TestMakerFree";

    constructor(private http: HttpClient, @Inject(PLATFORM_ID) private platformId: any, @Inject('BASE_URL') private baseUrl: string) {

    }

    //performs the login
    login(username: string, password: string, url:string): Observable<boolean> {
        var url1 = url;
        var data = {
            username: username,
            password: password,
            client_id: this.clientId,
            //required when signin up with username/password
            grant_type: "password",
            //space-separated list of scopes for wich the token is issued
            scope: "offline_access profile email"
        };

        return this.getAuthFromServer(url, data);
    }

    refreshToken(): Observable<boolean> {
        const url = this.baseUrl + "api/token/auth";
        const data = {
            client_id: this.clientId,
            grant_type: "refresh_token",
            refresh_token: this.getAuth()!.refresh_token,
            scope: "offline_access profile email"
        };

        return this.getAuthFromServer(url, data);
    }

    // retrive the access & refresh tokens from the server
    getAuthFromServer(url: string, data: any): Observable<boolean> {
        return this.http.post<TokenResponse>(url, data).map((res) => {
            let token = res && res.token;
            if (token) {
                this.setAuth(res);
                return true;
            }

            // login failed
            return Observable.throw('Unauthorized');
        })
            .catch(error => {
                return new Observable<any>(error);
            })
    }

    logout(): boolean {
        this.setAuth(null);
        return true;
    }

    //Persist auth into localStorage or remove it if a NULL argument is given
    setAuth(auth: TokenResponse | null): boolean {
        if (isPlatformBrowser(this.platformId)) {
            if (auth) {
                localStorage.setItem(this.authKey, JSON.stringify(auth));
            }
            else {
                localStorage.removeItem(this.authKey);
            }
        }
        return true;
    }

    // Retrives the auth JSON object (or NULL if none)
    getAuth(): TokenResponse | null {
        if (isPlatformBrowser(this.platformId)) {
            var i = localStorage.getItem(this.authKey);
            if (i) {
                return JSON.parse(i);
            }
        }
        return null;
    }

    // Return TRUE if the user is logged in, FALSE otherwise
    isLoggedIn(): boolean {
        if (isPlatformBrowser(this.platformId)) {
            return localStorage.getItem(this.authKey) != null;
        }
        return false;
    }
}