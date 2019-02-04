import { Component, Inject, OnInit, NgZone, PLATFORM_ID } from "@angular/core";
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { AuthService } from '../../services/auth.services';

declare var window: any;
declare var FB: any;

@Component({
    selector: "login-facebook",
    templateUrl: "./login.facebook.component.html"
})
export class LoginFacebookComponent implements OnInit {
    constructor(private http: HttpClient, private router: Router, private authService: AuthService, private zone: NgZone,
        @Inject(PLATFORM_ID) private platformId: any, @Inject('BASE_URL') private baseUrl: string) {

    }

    ngOnInit() {
        if (!isPlatformBrowser(this.platformId)) {
            return;
        }
        if (typeof (FB) === 'undefined') {
            // if the FB object is undefinded it means that it's the first time we visit this page
            window.fbAsyncInit = () =>
                this.zone.run(() => {
                    FB.init({
                        appId: '513088735831135',
                        xfbml: true,
                        version: 'v2.10'
                    });
                    FB.AppEvents.logPageView();

                    FB.Event.subscribe('auth.statusChange', (
                        (result: any) => {
                            console.log("FB auth status changed");
                            console.log(result);
                            if (result.status === 'connected') {
                                // login successful
                                console.log("Connected to Facebook");
                                this.onConnect(result.authResponse.accessToken);
                            }
                        })
                    );
                });

            // Load the SDK js library
            (function (d, s, id) {
                var js, fjs = d.getElementsByTagName(s)[0];
                if (d.getElementById(id)) { return; }
                js = d.createElement(s); js.id = id;
                (<any>js).src = "//connect.facebook.net/en_US/sdk.js";
                fjs.parentNode!.insertBefore(js, fjs);
            }(document, 'script', 'facebook-jssdk'));
        }
        else {
            //Reload the FB login button
            window.FB.XFBML.parse();
            // if the user is still connected log him off
            FB.getLoginStatus(function (response: any) {
                if (response.status === 'connected') {
                    FB.logout(function (res: any) {
                        //do nothing
                    });
                }
            })
        }
    }

    // this method will be executed upon the user FB SDK Auth roundtrip completion
    onConnect(accessToken: string) {
        // call TokenController and register/login
        var url = this.baseUrl + "api/token/facebook";
        var data = {
            access_token: accessToken,
            client_id: this.authService.clientId
        };
        this.http.post<TokenResponse>(url, data).subscribe(res => {
            if (res) {
                console.log("Login successful");
                console.log(res);
                this.authService.setAuth(res);
                this.router.navigate(["home"]);
            } else {
                console.log("Authentication failed");
            }
        }, error => console.log(error))
    }
}
