# Task 015 — Polish and Hardening

## Objective

Revisar la aplicación completa antes de considerar terminada la V1.

## Code Quality

Revisar:

* duplicación
* arquitectura
* dependencias
* nombres
* errores
* logging
* manejo de excepciones
* cancellation tokens
* queries innecesarias

## Database

Revisar:

* índices
* foreign keys
* unique constraints
* migrations
* relaciones
* índices de búsqueda

## Security

Revisar:

* autorización
* aislamiento entre Teams
* validación de input
* XSS
* Markdown sanitization
* acceso a issues
* acceso a comentarios
* SignalR authorization

## Performance

Revisar:

* N+1 queries
* consultas sin paginación
* Include innecesarios
* rendering excesivo en Blazor
* consultas repetidas
* búsqueda

## UI/UX

Revisar:

* loading states
* empty states
* error states
* confirmaciones
* keyboard navigation
* responsive layout
* dark mode
* accesibilidad

## Tests

Agregar o completar:

* unit tests
* integration tests
* authorization tests
* validation tests

## Final Verification

Ejecutar:

```text
dotnet build
dotnet test
```

La aplicación debe funcionar correctamente desde una instalación limpia.

## Out of Scope

No agregar nuevas features.

El objetivo de esta tarea es estabilizar lo existente.
