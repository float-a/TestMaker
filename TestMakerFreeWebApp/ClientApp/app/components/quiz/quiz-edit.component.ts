import { Component, Inject, OnInit } from "@angular/core";
import { FormGroup, FormControl, FormBuilder, Validator, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from "@angular/router";
import { HttpClient } from "@angular/common/http";

@Component({
    selector: "quiz-edit",
    templateUrl: './quiz-edit.component.html',
    styleUrls: ['./quiz-edit.component.css']
})
export class QuizEditComponent {
    title: string = "";
    quiz: Quiz;
    form: FormGroup;

    //this will be true when editing an existing quiz, false when creating a new one
    editMode: boolean;

    constructor(private activatedRoute: ActivatedRoute, private router: Router, private http: HttpClient, private fb: FormBuilder, @Inject('BASE_URL') private baseUrl: string) {
        this.quiz = <Quiz>{};

        this.createForm();

        var id = +this.activatedRoute.snapshot.params["id"];
        if (id) {
            this.editMode = true;

            var url = this.baseUrl + "api/quiz/" + id;
            this.http.get<Quiz>(url).subscribe(res => {
                this.quiz = res;
                this.title = "Edit - " + this.quiz.Title;
                this.updateForm();
            }, error => console.error(error));
        }
        else {
            this.editMode = false;
            this.title = "Create a new quiz";
        }
    }

    onSubmit() {
        var tempQuiz = <Quiz>{};
        tempQuiz.Title = this.form.value.Title;
        tempQuiz.Description = this.form.value.Description;
        tempQuiz.Text = this.form.value.Text;

        var url = this.baseUrl + "api/quiz";

        if (this.editMode) {
            tempQuiz.Id = this.quiz.Id;

            this.http.post<Quiz>(url, tempQuiz).subscribe(res => {
                var v = res;
                console.log("Quiz " + v.Id + " has been created.");
                this.router.navigate(["home"])
            }, error => console.log(error));
        }
        else {
            this.http.put<Quiz>(url, tempQuiz).subscribe(res => {
                var q = res;
                console.log("Quiz " + q.Id + " has been created.");
                this.router.navigate(["home"]);
            }, error => console.log(error));
        }
    }

    createForm() {
        this.form = this.fb.group({
            Title: ['', Validators.required],
            Description: '',
            Text: ''
        })
    }

    updateForm() {
        this.form.setValue({
            Title: this.quiz.Title,
            Description: this.quiz.Description || '',
            Text: this.quiz.Text || ''
        })
    }

    onBack() {
        this.router.navigate(["home"]);
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