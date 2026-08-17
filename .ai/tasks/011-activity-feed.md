# Task 011 — Activity Feed

## Objective

Implementar un historial de actividad para Teams e Issues.

## Entity

Implementar `Activity`.

Campos:

```text
Id
TeamId
UserId
EntityType
EntityId
Action
PayloadJson
CreatedAt
```

## Rules

Activity es append-only.

No debe:

* actualizarse
* eliminarse

## Events

Implementar inicialmente:

```text
IssueCreated
IssueUpdated
IssueAssigned
IssueCompleted
IssueCanceled

CommentCreated
CommentUpdated

LabelAdded
LabelRemoved

SprintStarted
SprintCompleted

RoadmapItemCreated
RoadmapItemUpdated
```

## Architecture

Evitar acoplar Activity directamente a cada feature.

Crear un mecanismo común para registrar actividades.

Por ejemplo:

```text
ActivityService
```

o un mecanismo basado en domain events.

La implementación debe ser consistente con la arquitectura existente.

## UI

Implementar:

### Issue Activity

Mostrar:

```text
Dario moved issue from Todo to In Progress

5 minutes ago

Dario assigned the issue to Juan

10 minutes ago

Dario added label "bug"

15 minutes ago
```

### Team Activity Feed

Mostrar actividad reciente del Team.

## Acceptance Criteria

* Todas las acciones importantes generan Activity.
* Activity contiene actor.
* Activity contiene fecha.
* Activity contiene entidad afectada.
* Activity puede renderizarse correctamente.
* Tests.
