import { Component, Inject, Input, OnChanges, SimpleChanges } from "@angular/core";
import { Router } from "@angular/router";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { RequestOptions } from "@angular/http/src";

@Component({
    selector: "question-list",
    templateUrl: "./question-list.component.html",
    styleUrls: ["./question-list.component.css"]
})

export class QuestionListComponent implements OnChanges {
    @Input() quiz: Quiz;
    questions: Question[];
    title: string;

    constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private router: Router) {
        this.questions = [];
    }

    ngOnChanges(changes: SimpleChanges) {
        if (typeof changes['quiz'] !== "undefined") {
            var change = changes['quiz'];
            if (!change.isFirstChange()) {
                this.loadData();
            }
        }
    }
    loadData() {
        var url = this.baseUrl + "api/question/All/" + this.quiz.Id;
        let head = new HttpHeaders({ 'Accept': 'text/html' });
        this.http.get<Question[]>(url, { headers: head } ).subscribe(res => {
            this.questions = res;
            console.log(res);
        }, error => console.log(error));
    }

    onCreate() {
        this.router.navigate(["/question/create/", this.quiz.Id]);
    }

    onEdit(question: Question) {
        this.router.navigate(["/question/edit/", question.Id]);
    }

    onDelete(question: Question) {
        if (confirm("Do you really want to delete this question?")) {
            var url = this.baseUrl + "api/question" + question.Id;
            this.http.delete(url).subscribe(res => {
                console.log("Question " + question.Id + " has been deleted.");
                this.loadData();
            }, error => console.log(error));
        }
    }

}