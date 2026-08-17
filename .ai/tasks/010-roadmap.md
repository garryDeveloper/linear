# Task 010 — Roadmap

## Objective

Implementar Roadmaps para planificación de trabajo.

## Entities

Implementar:

* Roadmap
* RoadmapItem

Según `.ai/domain-model.md`.

## RoadmapItem

Campos:

```text
Id
RoadmapId
Name
Description
Status
StartDate
TargetDate
CreatedAt
UpdatedAt
```

## Status

```text
Planned
InProgress
Completed
Canceled
```

## Features

Implementar:

* crear roadmap
* editar roadmap
* eliminar roadmap
* crear roadmap item
* editar roadmap item
* eliminar roadmap item
* asociar issues

## UI

Crear una vista temporal tipo timeline.

Ejemplo:

```text
Roadmap

Aug       Sep       Oct       Nov

Authentication
████████████████

New Dashboard
          ███████████████

Mobile App
                    █████████████
```

La V1 no necesita ser un clon visual exacto de Linear.

Priorizar:

* claridad
* fechas
* estado
* asociación con issues

## Acceptance Criteria

* CRUD funcional.
* Roadmap items funcionando.
* Issues pueden asociarse.
* Vista timeline funcional.
* Tests completos.
