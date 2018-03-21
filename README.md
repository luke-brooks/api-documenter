# api-documenter
A small app that gathers data about an ASP.NET MVC API and creates a living documentation.

The dll for the app is located here : https://github.com/luke-brooks/api-documenter/blob/master/ApiDocumenter/obj/Debug/

The ApiDocumenter is designed to be added to an existing .NET API. Once implemented, the ApiDocumenter will gather information about all of the ApiControllers on a given API. This information includes: Controller names, method names, method parameter names and types, method return types, and a JSON string of any complex objects that are method return types or method parameter types.

A Razor C# view set has been added to the project. The Razor view uses jQuery and Sweet Alert. A single-page application version of the Razor view set will be created and added in the future. A website that demonstrates the ApiDocumenter and it's front end capability will be added in the future.
