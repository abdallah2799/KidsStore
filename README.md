# KidsStore Management System

## Architecture Overview

The system is built using ASP.NET Core and follows a layered architecture:

- **Domain Layer** ([Domain/](Domain/)):  
  Contains core business entities such as [`Domain.Entities.User`](Domain/Entities/User.cs), [`Domain.Entities.Vendor`](Domain/Entities/Vendor.cs), [`Domain.Entities.Product`](Domain/Entities/Product.cs), etc.

- **Application Layer** ([Application/](Application/)):  
  Defines business logic and service interfaces, e.g. [`Application.Interfaces.Services.IVendorService`](Application/Interfaces/Services/IVendorService.cs), [`Application.Services.VendorService`](Application/Services/VendorService.cs), and repository interfaces like [`Application.Interfaces.Repositories.IUnitOfWork`](Application/Interfaces/Repositories/IUnitOfWork%20.cs).

- **Infrastructure Layer** ([Infrastructure/](Infrastructure/)):  
  Implements data access using Entity Framework Core. Contains [`Infrastructure.Persistence.AppDbContext`](Infrastructure/Persistence/AppDbContext.cs), repository implementations ([`Infrastructure.Repositories.UnitOfWork`](Infrastructure/Repositories/UnitOfWork.cs)), and database migrations ([Infrastructure/Migrations/](Infrastructure/Migrations/)).

- **WebUI Layer** ([WebUI/](WebUI/)):  
  ASP.NET Core MVC frontend. Contains controllers (e.g. [`WebUI.Controllers.VendorsController`](WebUI/Controllers/VendorsController.cs)), views ([WebUI/Views/](WebUI/Views/)), static assets ([WebUI/wwwroot/](WebUI/wwwroot/)), and configuration files.

## Coding Patterns

- **Dependency Injection:**  
  Services and repositories are registered in [`WebUI/Program.cs`](WebUI/Program.cs) for DI.

- **Repository & Unit of Work:**  
  Data access is abstracted via repository interfaces ([`Application.Interfaces.Repositories.IRepository`](Application/Interfaces/Repositories/IRepository.cs)) and [`UnitOfWork`](Infrastructure/Repositories/UnitOfWork.cs).

- **Service Layer:**  
  Business logic is encapsulated in service classes, e.g. [`Application.Services.AccountService`](Application/Services/AccountService.cs), [`Application.Services.VendorService`](Application/Services/VendorService.cs).

- **MVC Pattern:**  
  Controllers handle HTTP requests, interact with services, and return views or JSON.

- **Entity Framework Core:**  
  Used for ORM, migrations, and database seeding.

- **Frontend:**  
  Uses Bootstrap and Font Awesome for UI, with custom JS helpers ([WebUI/wwwroot/js/](WebUI/wwwroot/js/)) for modals and toasts.

## Getting Started

1. **Restore NuGet Packages:**  
   Open the solution and restore dependencies.

2. **Database Migration:**  
   On first run, the app will apply migrations and seed default data (see [`WebUI/Program.cs`](WebUI/Program.cs)).

3. **Run the Application:**  
   Start the project from Visual Studio or via `dotnet run`.

## Main Features

- User authentication and role-based authorization (Admin, Cashier)
- Vendor, Product, Purchase, Sales, and Return management
- Dashboard with statistics and reports
- Responsive UI with Bootstrap and Font Awesome

## Folder Structure

- `Domain/` – Business entities
- `Application/` – Service and repository interfaces, business logic
- `Infrastructure/` – Data access, EF Core, migrations
- `WebUI/` – MVC frontend, controllers, views, static files

## Contributing

See `.github/workflows/` for CI setup.

---

*This README reflects the architecture and coding patterns found in your workspace. For more details, see the referenced files and folders.*