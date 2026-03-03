# FireArt Test Task API

.NET 8 Web API with JWT authentication, Product CRUD, and search functionality.

## Tech Stack

- **Runtime**: .NET 8
- **Database**: PostgreSQL + Entity Framework Core (Npgsql)
- **Auth**: JWT Bearer + BCrypt password hashing
- **Validation**: FluentValidation
- **Tests**: xUnit, Moq, FluentAssertions, WebApplicationFactory + EF InMemory
- **Docs**: Swagger / Swashbuckle

## Prerequisites

- .NET 8 SDK
- PostgreSQL (for running the API; tests use InMemory DB)

## Getting Started

### 1. Configure the database

Update the connection string in `src/FireArtTestTask.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=fireart_db;Username=postgres;Password=postgres"
}
```

### 2. Apply migrations (or let EF create the DB)

```bash
cd src/FireArtTestTask.Api
dotnet ef migrations add Init
dotnet ef database update
```

> Note: You need `dotnet-ef` tool installed: `dotnet tool install --global dotnet-ef`

### 3. Run the API

```bash
cd src/FireArtTestTask.Api
dotnet run
```

The API starts at `http://localhost:5000`. Swagger UI is available at `http://localhost:5000/swagger`.

### 4. Run tests

```bash
dotnet test
```

All tests run against an InMemory database — no PostgreSQL required.

## API Endpoints

### Auth (no authentication required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/signup` | Register a new user |
| POST | `/api/auth/login` | Login, returns JWT |
| POST | `/api/auth/forgot-password` | Request password reset token |
| POST | `/api/auth/reset-password` | Reset password with token |

### Products (JWT required)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/products` | Create a product |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products` | Search/list products |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product |

### Search Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `search` | string | Search in name and description |
| `category` | string | Filter by exact category |
| `minPrice` | decimal | Minimum price filter |
| `maxPrice` | decimal | Maximum price filter |
| `sortBy` | string | Sort field: `name`, `price`, `category`, `createdAt` |
| `sortDescending` | bool | Sort direction (default: `false`) |
| `page` | int | Page number (default: `1`) |
| `pageSize` | int | Items per page (default: `10`) |

## Project Structure

```
FireArtTestTask/
├── src/FireArtTestTask.Api/
│   ├── Controllers/        # AuthController, ProductsController
│   ├── Data/               # AppDbContext
│   ├── Entities/            # User, Product
│   ├── DTOs/               # Request/Response models
│   ├── Services/            # Business logic (Auth, Product, JWT, Email)
│   ├── Validators/          # FluentValidation validators
│   ├── Middleware/          # Global exception handling
│   ├── Exceptions/          # Custom exception types
│   └── Configuration/      # JwtSettings
└── tests/FireArtTestTask.Tests/
    ├── Unit/               # Service and validator tests
    └── Integration/        # Endpoint tests with WebApplicationFactory
```

## Postman Collection

Import `FireArtTestTask.postman_collection.json` into Postman. Run requests in order (Auth first, then Products). The collection auto-saves the JWT token and product ID to variables.

## Assumptions & Design Decisions

- **Entity choice**: `Product` with fields Name, Description, Price, Category — a practical and universally understandable domain entity.
- **Auth**: Custom JWT-based authentication (no ASP.NET Identity) — signup, login, forgot-password, and reset-password. Passwords are hashed with BCrypt.
- **Password reset flow**: A reset token is generated and logged to console (email sending is stubbed via `IEmailService`). In production this would be replaced with a real email provider (SendGrid, SMTP, etc.).
- **Search**: The `GET /api/products` endpoint supports full-text search (by name/description), category filtering, price range filtering, sorting, and pagination.
- **Validation**: FluentValidation is used for all request validation, invoked manually in controllers for explicit control.
- **Error handling**: A global `ExceptionHandlingMiddleware` maps custom exceptions (`NotFoundException`, `ConflictException`, `UnauthorizedException`) to proper HTTP status codes.
- **Security**: The forgot-password endpoint always returns 200 regardless of whether the email exists, to prevent user enumeration.
- **Tests**: 64 tests (unit + integration) run against InMemory EF Core — no external dependencies needed.
- **No ASP.NET Identity**: Auth is built from scratch to demonstrate understanding of JWT, password hashing, and token-based flows.
