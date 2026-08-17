# Task 005 — Issues

## Objective

Implementar el núcleo principal de la aplicación: Issues.

## Entity

Implementar la entidad definida en `.ai/domain-model.md`.

Campos principales:

```text
Id
Identifier
Title
Description
Status
Priority
Estimate
TeamId
AssigneeId
CreatedById
SprintId
RoadmapItemId
CreatedAt
UpdatedAt
CompletedAt
ArchivedAt
```

## Status

```text
Backlog
Todo
InProgress
InReview
Done
Canceled
```

## Priority

```text
None
Low
Medium
High
Urgent
```

## Identifier

Generar identificadores secuenciales por Team.

Ejemplo:

```text
WEB-1
WEB-2
WEB-3
```

La generación debe ser segura ante concurrencia.

## Features

Implementar:

* crear issue
* editar issue
* eliminar issue
* archivar issue
* cambiar status
* cambiar priority
* asignar usuario
* asignar sprint
* asignar labels
* cambiar estimate
* obtener issue
* listar issues
* paginar issues

## UI

Implementar:

### Issue List

Mostrar:

* identifier
* title
* status
* priority
* assignee
* labels
* sprint

### Issue Detail

Permitir editar:

* title
* description
* status
* priority
* assignee
* labels
* sprint
* estimate

## UX

La interacción debe ser rápida.

Evitar navegar a páginas separadas para operaciones simples cuando pueda utilizarse:

* modal
* popover
* inline editing
* command menu

## Acceptance Criteria

* CRUD funcional.
* Identifier único.
* Identifier seguro ante concurrencia.
* Issues correctamente aislados por Team.
* Permisos funcionando.
* Paginación funcionando.
* Tests completos.

## Out of Scope

No implementar todavía:

* comentarios
* realtime
* búsqueda avanzada
* activity feed
