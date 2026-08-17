# Task 003 — Teams

## Objective

Implementar equipos y membresías.

## Entities

Implementar:

* Team
* TeamMember

Según `.ai/domain-model.md`.

## Features

Implementar:

* crear equipo
* editar equipo
* eliminar equipo
* obtener equipo
* listar equipos del usuario
* agregar miembros
* eliminar miembros
* cambiar rol de miembro

## Team Key

Cada equipo debe tener una key única.

Ejemplos:

```text
WEB
CORE
MOBILE
```

Los issues utilizarán esta key.

## Permissions

Owner:

* administrar equipo
* administrar miembros
* administrar configuración

Admin:

* administrar miembros
* administrar configuración

Member:

* utilizar el equipo
* crear issues
* modificar issues permitidos

## UI

Crear:

```text
Team Settings
Team Members
Team Selector
```

El Team Selector debe permitir cambiar rápidamente entre equipos.

## Acceptance Criteria

* Un usuario puede crear un equipo.
* Un usuario puede ver sus equipos.
* Los miembros pueden administrarse.
* Los permisos funcionan.
* La Team Key es única.
* Tests de dominio, endpoints y autorización.

## Out of Scope

No implementar todavía:

* proyectos
* integraciones
* invitaciones por email
