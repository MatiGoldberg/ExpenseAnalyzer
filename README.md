# ExpenseAnalyzer

ExpenseAnalyzer is an ASP.NET Core MVC web application targeting .NET 8.0. It features a modern and sleek UI, and is ready for integration with Bootstrap or Tailwind CSS for a clean, responsive design.

## Build the Project

To build the solution, run:

```sh
dotnet build
```

## Run the Project

To run the web application locally:

```sh
dotnet run --project ExpenseAnalyzer/ExpenseAnalyzer.csproj
```

Then open your browser and navigate to the URL shown in the terminal (usually https://localhost:5001 or similar).


## Run Tests

To execute all tests, run:

```sh
dotnet test ExpenseAnalyzer.Tests/ExpenseAnalyzer.Tests.csproj
```

This ensures only the test project is run and avoids errors from non-test projects.

## Project Structure

- `Controllers/` - MVC controllers
- `Models/` - Application models
- `Views/` - Razor views
- `wwwroot/` - Static files (CSS, JS, images)
- `ExpenseAnalyzer.Tests/` - Test project

## Frontend Framework

- To use Bootstrap, add it via CDN or npm and update your layout file.
- To use Tailwind CSS, follow the official Tailwind setup for ASP.NET Core projects.

---

For more details, see the official [ASP.NET Core documentation](https://docs.microsoft.com/aspnet/core/).