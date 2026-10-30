# EcommerceApi

An ASP.NET Core REST API for products, categories, and customer orders. It uses SQL Server with EF Core migrations, ASP.NET Core Identity, JWT bearer authentication, and role-based authorization.

## Requirements

- .NET 10 SDK
- SQL Server (SQL Server LocalDB is the default development connection on Windows)

## Configure and run locally

The development connection string is in `appsettings.Development.json`. To use another SQL Server, set `ConnectionStrings:DefaultConnection` through user secrets.

Configure a private signing key and an administrator account. Do not commit these values:

```powershell
dotnet user-secrets set "Jwt:Key" "<a random secret of at least 32 bytes>" --project .\StoreApi.csproj
dotnet user-secrets set "Jwt:Issuer" "EcommerceApi" --project .\StoreApi.csproj
dotnet user-secrets set "Jwt:Audience" "EcommerceApi.Client" --project .\StoreApi.csproj
dotnet user-secrets set "Admin:Email" "admin@example.com" --project .\StoreApi.csproj
dotnet user-secrets set "Admin:Password" "<a unique password of at least 12 characters>" --project .\StoreApi.csproj
```

Generate a signing key locally with:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Copy the output into the `Jwt:Key` user-secret. The app applies checked-in EF Core migrations and seeds the `Admin` and `Customer` roles at startup. When both administrator secrets are configured, it creates that administrator if needed.

```powershell
dotnet restore .\EcommerceApi.slnx
dotnet run --project .\StoreApi.csproj
```

In Development, the OpenAPI document is available at `/openapi/v1.json`. Example HTTP requests are in `StoreApi.http`.

## Endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public; creates a Customer account and returns a JWT |
| POST | `/api/auth/login` | Public |
| GET | `/api/products`, `/api/products/{id}` | Public |
| POST, PUT, DELETE | `/api/products` | Admin |
| GET | `/api/categories`, `/api/categories/{id}` | Public |
| POST, PUT, DELETE | `/api/categories` | Admin |
| GET, POST | `/api/orders`, `/api/orders/{id}` | Authenticated; customers can access only their own orders |
| PATCH | `/api/orders/{id}/status` | Admin |

New orders take customer identity from the authenticated account, check and decrement stock in a serializable transaction, and snapshot each product's purchase price.

## Database migrations

Migrations live in `Data/Migrations`. To add a migration after changing the data model:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.10
dotnet ef migrations add <MigrationName> --project .\StoreApi.csproj
```

The API applies migrations during startup. Configure a production database connection string and JWT/admin settings in the hosting service's environment/app settings; never put deployment credentials or production secrets in this repository.

## Tests

```powershell
dotnet test .\EcommerceApi.slnx
```

The xUnit tests use EF Core InMemory for controller behavior and Moq for token-service role claims.

## GitHub Actions deployment

`.github/workflows/ci-cd.yml` runs restore, tests, and publish on pushes and pull requests. A push to `main` deploys to Azure App Service only after the repository has both:

- Variable `AZURE_WEBAPP_NAME`: the App Service name.
- Secret `AZURE_WEBAPP_PUBLISH_PROFILE`: the App Service publish profile XML.

Set the SQL Server connection string, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, and (if needed) `Admin__Email` / `Admin__Password` in the App Service configuration before deployment. The deployment job skips safely while the Azure name or publish profile is not configured.
