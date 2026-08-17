# Task 007 — Sprints

## Objective

Implementar Sprints para organizar el trabajo temporal de un Team.

## Entity

Implementar `Sprint`.

Campos:

```text
Id
TeamId
Name
Goal
StartDate
EndDate
Status
CreatedAt
UpdatedAt
CompletedAt
```

## Status

```text
Planned
Active
Completed
Canceled
```

## Rules

* Solo puede existir un Sprint Active por Team.
* EndDate debe ser posterior a StartDate.
* Un Sprint pertenece a un Team.
* Un Issue puede pertenecer a un único Sprint.
* Un Issue puede no tener Sprint.

## Features

Implementar:

* crear sprint
* editar sprint
* iniciar sprint
* completar sprint
* cancelar sprint
* listar sprints
* obtener sprint
* asignar issues
* remover issues

## UI

Crear:

```text
Sprint List
Sprint Detail
Sprint Board
```

Sprint Board:

```text
Todo
In Progress
In Review
Done
```

Mostrar métricas:

* total issues
* completed
* remaining
* completion percentage

## Acceptance Criteria

* Solo un sprint activo por Team.
* Issues pueden moverse entre sprints.
* Sprint puede iniciarse y completarse.
* Board funcional.
* Tests completos.
