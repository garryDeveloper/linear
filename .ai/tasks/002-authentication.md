# Task 002 — Authentication

## Objective

Implementar autenticación y autorización básica para usuarios de la aplicación.

## Scope

Implementar:

* User
* autenticación
* login
* logout
* sesión persistente
* autorización
* roles

## User

Implementar la entidad `User` definida en `.ai/domain-model.md`.

Campos:

```text
Id
Email
Name
AvatarUrl
IsActive
CreatedAt
UpdatedAt
```

## Authentication

Para la V1 utilizar una solución estándar de ASP.NET Core.

No implementar autenticación social.

## Roles

Implementar inicialmente:

```text
Admin
Member
```

La pertenencia a equipos se manejará mediante `TeamMember`.

## UI

Crear:

* Login
* perfil básico
* logout
* estado del usuario actual

## Authorization

Preparar autorización basada en policies.

Ejemplos:

```text
RequireAdmin
RequireTeamMember
```

## Acceptance Criteria

* Un usuario puede iniciar sesión.
* Un usuario puede cerrar sesión.
* Un usuario no autenticado no puede acceder a la aplicación.
* El usuario autenticado puede obtenerse desde el contexto actual.
* Los roles funcionan correctamente.
* Tests para autenticación y autorización.

## Out of Scope

No implementar:

* OAuth
* Google login
* GitHub login
* recuperación de contraseña
* 2FA
* invitaciones por email
