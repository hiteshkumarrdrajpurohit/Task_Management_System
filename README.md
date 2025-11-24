# TaskManagement_02

Simple task management web application built with ASP.NET Core (MVC) and Entity Framework Core.

- .NET: .NET 8
- C#: 12
- Project type: ASP.NET Core MVC (Controllers + Views)
- Database: SQL Server (LocalDB by default)

## Summary

This project provides user registration and authentication (session-based), categories and task CRUD features. The app stores users, categories and tasks using EF Core. The default route opens the sign-in page.

Key files:
- `Program.cs` — app startup, DB registration, session configuration, routing
- `Data/ApplicationDbContext.cs` — EF Core DbContext
- `Controllers/UserController.cs`, `Controllers/TasksController.cs`, `Controllers/CategoryController.cs`
- `Views/*` — Razor views for Users, Tasks and Categories
- `appsettings.json` — connection string and configuration

## Features
- User sign-up, sign-in, sign-out (session)
- Create / Read / Update / Delete tasks
- Manage categories
- Server-side validation attributes on models
- Uses EF Core for persistence

## Prerequisites
- .NET 8 SDK
- Visual Studio 2022 (or later) or VS Code
- SQL Server LocalDB (or a SQL Server instance)
- (Optional) `dotnet-ef` CLI tool for migrations:
  - `dotnet tool install --global dotnet-ef`

If using Visual Studio, open the solution and use the built-in tooling.

## Configuration

Default connection string is in `appsettings.json`:
