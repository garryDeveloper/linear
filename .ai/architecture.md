# Architecture

## Overview

Aplicación web monolítica inspirada en Linear.

Objetivos:

* Simplicidad de despliegue
* Alta productividad de desarrollo
* Código mantenible
* Buen rendimiento para miles de issues

## Stack

### Frontend

* ASP.NET Core Blazor Web App
* Blazor Server Interactive
* MudBlazor
* Markdown Rendering
* SignalR para tiempo real

### Backend

* ASP.NET Core
* FastEndpoints
* FluentValidation

### Persistencia

* PostgreSQL
* Entity Framework Core

## Architectural Style

Vertical Slice Architecture.

La aplicación se organiza por funcionalidades, no por capas técnicas.

Evitar estructuras como:

```text
Controllers/
Services/
Repositories/
```

Preferir:

```text
src/
├── Features/
│   ├── Teams/
│   ├── Issues/
│   ├── Labels/
│   ├── Comments/
│   ├── Sprints/
│   └── Roadmaps/
├── Domain/
├── Infrastructure/
└── Shared/
```

## Feature Structure

Cada feature debe contener:

```text
Features/
└── Issues/
    ├── Create/
    ├── Update/
    ├── Delete/
    ├── GetById/
    └── Search/
```

Dentro de cada operación:

```text
Create/
├── Endpoint.cs
├── Request.cs
├── Response.cs
├── Validator.cs
└── Handler.cs
```

## Domain Layer

La capa Domain contiene:

* Entidades
* Value Objects
* Enums
* Reglas de negocio

No debe depender de:

* EF Core
* FastEndpoints
* MudBlazor
* PostgreSQL

## Infrastructure Layer

Responsabilidades:

* EF Core
* Configuración de base de datos
* Migraciones
* Implementaciones de servicios externos

## UI Layer

Blazor será responsable de:

* Navegación
* Componentes visuales
* Formularios
* Validaciones de cliente
* Estados de carga
* Modales
* Tablas
* Filtros

La lógica de negocio no debe vivir en componentes Razor.

## State Management

Utilizar:

* Estado local de componentes cuando sea posible
* Servicios Scoped para estado compartido
* Evitar complejidad innecesaria (Redux-like)

## Communication

Los componentes Blazor consumirán endpoints internos mediante HttpClient.

Flujo:

```text
Blazor Component
      ↓
FastEndpoint
      ↓
Handler
      ↓
DbContext
```

## Realtime

SignalR se utilizará para:

* Actualización de issues
* Comentarios en tiempo real
* Activity Feed
* Cambios de sprint
* Presencia de usuarios (futuro)

## Authorization

Roles iniciales:

* Admin
* Member

Políticas basadas en claims.

## Performance Guidelines

Siempre:

* Async/Await
* CancellationToken
* AsNoTracking para consultas
* Paginación obligatoria en listados
* Proyecciones a DTOs
* Evitar Include innecesarios

## Testing

### Unit Tests

* Dominio
* Validaciones
* Casos de uso

### Integration Tests

* Endpoints
* Persistencia
* Autorización

## Deployment

Aplicación única.

```text
ASP.NET Core
    +
Blazor
    +
PostgreSQL
```

Sin microservicios.

Sin separación frontend/backend para la V1.

Priorizar simplicidad sobre escalabilidad prematura.
