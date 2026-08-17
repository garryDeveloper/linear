# Task 001 — Project Foundation

## Objective

Crear la estructura inicial del proyecto y establecer las bases técnicas de la aplicación.

## Scope

Implementar:

* ASP.NET Core Blazor Web App
* .NET
* PostgreSQL
* Entity Framework Core
* FastEndpoints
* FluentValidation
* MudBlazor
* estructura Vertical Slice
* configuración de desarrollo
* configuración de producción básica
* Docker Compose para PostgreSQL

## Project Structure

La solución debe seguir la arquitectura definida en:

* `.ai/architecture.md`
* `.ai/coding-standards.md`
* `.ai/domain-model.md`

Estructura aproximada:

```text
src/
├── Features/
├── Domain/
├── Infrastructure/
├── Shared/
└── Web/
```

La estructura concreta puede adaptarse a las convenciones de Blazor, siempre manteniendo separación clara entre UI, dominio e infraestructura.

## Database

Configurar:

* PostgreSQL
* EF Core
* DbContext
* migrations
* configuración mediante connection string

No implementar todavía las entidades de negocio.

## UI

Crear:

* Layout principal
* Sidebar
* Header
* área principal de contenido
* página inicial
* sistema básico de navegación

La UI debe tener una estética inspirada en herramientas como Linear:

* minimalista
* compacta
* orientada a productividad
* buen uso del espacio
* soporte para dark mode

No intentar replicar exactamente la interfaz de Linear.

## Configuration

Configurar:

* appsettings.json
* appsettings.Development.json
* variables de entorno
* logging
* Docker Compose

## Acceptance Criteria

* La aplicación inicia correctamente.
* Blazor funciona correctamente.
* PostgreSQL puede iniciarse mediante Docker Compose.
* EF Core puede conectarse a PostgreSQL.
* Una migration inicial puede ejecutarse.
* La navegación básica funciona.
* El proyecto compila sin warnings relevantes.
* No existen dependencias innecesarias.

## Out of Scope

No implementar:

* usuarios
* autenticación
* equipos
* issues
* labels
* sprints
* roadmap

## Verification

Ejecutar:

```text
dotnet build
dotnet test
```

Verificar manualmente que:

1. La aplicación inicia.
2. PostgreSQL inicia.
3. La aplicación puede conectarse a PostgreSQL.
4. La navegación funciona.
