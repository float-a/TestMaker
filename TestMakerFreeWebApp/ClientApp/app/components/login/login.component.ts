﻿import { Component, Inject } from "@angular/core";
import { FormGroup, FormControl, FormBuilder, Validators } from '@angular/forms';
import { Router } from "@angular/router";
import { AuthService } from '../../services/auth.services';

@Component({
    selector: "login",
    templateUrl: "./login.component.html",
    styleUrls: ['./login.component.css']
})
export class LoginComponent {
    title: string;
    form: FormGroup;

    constructor(private router: Router, private fb: FormBuilder, private authService: AuthService, @Inject('BASE_URL') private baseUrl: string) {
        this.title = "User Login";
        this.createForm();
    }

    createForm() {
        this.form = this.fb.group({
            Username: ['', Validators.required],
            Password: ['', Validators.required]
        });
    }

    onSubmit() {
        var url = this.baseUrl + "api/token/auth";
        var username = this.form.value.Username;
        var password = this.form.value.Password;

        this.authService.login(username, password, url)
            .subscribe(res => {
                //login successful
                //outputs the login info through a JS alert
                // Important: remove this when test is done.
                alert("Login successful! " + "Username: " + username + " Token: " + this.authService.getAuth()!.token);

                this.router.navigate(["home"]);
            },
                err => {
                    //login failed
                    console.log(err);
                    this.form.setErrors({ "auth": "Incorect username or password" });
                });
    }

    onBack() {
        this.router.navigate(["home"]);
    }

    // retrive a FormControl
    getFormControl(name: string) {
        return this.form.get(name);
    }

    isValid(name: string) {
        var e = this.getFormControl(name);
        return e && e.valid;
    }

    isChanged(name: string) {
        var e = this.getFormControl(name);
        return e && (e.dirty || e.touched);
    }

    hasError(name: string) {
        var e = this.getFormControl(name);
        return e && (e.dirty || e.touched) && !e.valid;
    }
}