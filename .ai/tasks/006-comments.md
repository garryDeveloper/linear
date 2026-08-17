# Task 006 — Comments

## Objective

Permitir conversaciones dentro de los issues.

## Entity

Implementar `Comment`.

Campos:

```text
Id
IssueId
AuthorId
Content
CreatedAt
UpdatedAt
DeletedAt
```

## Features

Implementar:

* crear comentario
* editar comentario
* eliminar comentario
* listar comentarios
* ordenar cronológicamente

## Markdown

El contenido debe almacenarse como Markdown.

La implementación completa de Markdown se realizará en Task 012.

Por ahora preparar el modelo para soportarlo.

## Permissions

Un usuario puede:

* crear comentarios en issues a los que tiene acceso
* editar sus propios comentarios
* eliminar sus propios comentarios

Admins pueden moderar comentarios según las reglas definidas.

## UI

En Issue Detail:

```text
Issue
──────
Description

Comments
────────
User A
Comentario...

User B
Otro comentario...

[ Add comment ]
```

## Acceptance Criteria

* CRUD funcional.
* Comentarios asociados correctamente al Issue.
* Autor identificado.
* Permisos funcionando.
* Tests completos.

## Out of Scope

No implementar todavía:

* reactions
* mentions
* attachments
* realtime
