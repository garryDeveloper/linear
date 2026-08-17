# Task 004 — Labels

## Objective

Implementar labels para categorizar issues.

## Scope

Implementar:

* Label
* IssueLabel
* CRUD
* asignación de labels
* eliminación de labels

## Label

Campos:

```text
Id
TeamId
Name
Description
Color
CreatedAt
```

## Rules

* El nombre es obligatorio.
* El nombre debe ser único dentro del Team.
* Una label pertenece a un único Team.

## UI

Crear:

* listado de labels
* crear label
* editar label
* eliminar label
* selector de labels

El selector debe permitir seleccionar múltiples labels.

## Acceptance Criteria

* CRUD funcional.
* Labels aisladas por Team.
* Un issue puede tener múltiples labels.
* Una label puede pertenecer a múltiples issues.
* Tests completos.

## Out of Scope

No implementar:

* labels globales
* jerarquía de labels
* automatizaciones
