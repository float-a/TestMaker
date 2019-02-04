
The users will need to register in order to grant them the ownership of their own tests. Once done, they will be able to create a new test.
Test will have a name, a description, a list of questions and a series of possible results.
Question will have a descriptive text, a list of answers and an optional image.
Answer will have a descriptive text, an optional image, and a list of score points.
Result will have a descriptive text and a score value.

Devolpment topics:
Routing: The application properly responds to client requests, routing them according to what they are up to.

Data Model: Adopt a database engine to store the tests, questions and so on. Define data architecture by setting up data repositories
            and domain entities that will be handled by the server and hooked to Angular through the Controller class, the most suited
            ASP.Net Core interface to handle HTTP communications.
            
Controllers: From an MVC-based architectural perspective, one of the main difference between multi-page and single-page applications is that
            the former's Controllers are designed to return views, while the latter ones, also known as API Controllers, mostly return 
            serialized data. These are what I need to implement to put Angular components in charge of the presentation layer.
            
Angular components: Define a set of components to hande UI elements and state changes. Components are the most fundamental elements in
                    Angular, replacing the AngularJS controllers and scopes.
                    
Authenticaion: Added a membership context in order to be able to limit CRUD operations to authenticated users only, keeping track of each
                user action, requiring registration to access some pages/views.
