import { Component, Inject, OnInit } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { HttpClient } from "@angular/common/http";
import { FormGroup, FormControl, FormBuilder, Validator, Validators } from '@angular/forms';

@Component({
    selector: "result-edit",
    templateUrl: "./result-edit.component.html",
    styleUrls: ['./result-edit.component.css']
})
export class ResultEditComponent {
    title: string;
    result: Result;
    form: FormGroup;

    editMode: boolean;

    constructor(private activatedRoute: ActivatedRoute, private router: Router, private http: HttpClient, private fb: FormBuilder, @Inject('BASE_URL') private baseUrl: string) {
        this.result = <Result>{};
        var id = +this.activatedRoute.snapshot.params["id"];
        this.editMode = (this.activatedRoute.snapshot.url[1].path === "edit");
        this.createForm();

        if (this.editMode) {
            var url = this.baseUrl + "api/result/" + id;
            this.http.get<Result>(url).subscribe(res => {
                this.result = res;
                this.title = "Edit - " + this.result.Text;
                this.updateForm();
            }, error => console.error(error))
        }
        else {
            this.result.QuizId = id;
            this.title = "Create new Result";
        }
    }

    onSubmit(result: Result) {
        var url = this.baseUrl + "api/result";

        if (this.editMode) {
            this.http.post<Result>(url, result).subscribe(res => {
                var v = res;
                console.log("Result " + v.Id + " has been updated.");
                this.router.navigate(["quiz/edit", v.QuizId]);
            }, error => console.log(error));
        }
        else {
            this.http.put<Result>(url, result).subscribe(res => {
                var v = res;
                console.log("Result " + v.Id + " has been created");
                this.router.navigate(["quiz/edit", v.QuizId]);
            }, error => console.log(error))
        }
    }

    onBack() {
        this.router.navigate(["quiz/edit", this.result.QuizId]);
    }

    createForm() {
        this.form = this.fb.group({
            Text: ['', Validators.required],
            MinValue: ['', Validators.pattern(/^\d*$/)],
            MaxValue: ['', Validators.pattern(/^\d*$/)]
        });
    }

    updateForm() {
        this.form.setValue({
            Text: this.result.Text,
            MinValue: this.result.MinValue || '',
            MaxValue: this.result.MaxValue || ''
        });
    }

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