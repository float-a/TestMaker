import { Injectable, Injector } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "./auth.services";
import { Observable } from "rxjs/Observable";
import { HttpClient, HttpHandler, HttpEvent, HttpInterceptor, HttpRequest, HttpResponse, HttpErrorResponse } from "@angular/common/http";

@Injectable()
export class AuthResponseInterceptor implements HttpInterceptor {
    currentRequest: HttpRequest<any>;
    auth: AuthService;

    constructor(private injector: Injector, private router: Router) { }

    intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        this.auth = this.injector.get(AuthService);
        var token = (this.auth.isLoggedIn()) ? this.auth.getAuth()!.token : null;

        if (token) {
            this.currentRequest = request;

            return next.handle(request).do((event: HttpEvent<any>) => {
                if (event instanceof HttpResponse) {
                    //do nothing
                }
            })
                .catch(error => {
                    return this.handleError(error, next)
                });
        }
        else {
            return next.handle(request);
        }
    }

    handleError(err: any, next: HttpHandler) {
        if (err instanceof HttpErrorResponse) {
            if (err.status == 401) {
                var previousRequest = this.currentRequest;
                return this.auth.refreshToken()
                    .flatMap((refresh) => {
                        const token = (this.auth.isLoggedIn()) ? this.auth.getAuth()!.token : null;
                        if (token) {
                            previousRequest = previousRequest.clone({
                                setHeaders: { Authorization: `Bearer ${token}` }
                            });
                        }
                        return next.handle(previousRequest);
                    })
            }
        }
        return Observable.throw(err);
    }
}