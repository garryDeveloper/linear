# Task 008 — Filters

## Objective

Implementar filtrado avanzado de issues.

## Filters

Soportar inicialmente:

```text
Status
Priority
Assignee
Label
Sprint
CreatedBy
```

## Operators

Soportar:

```text
is
is not
in
not in
```

Para texto:

```text
contains
```

## Examples

```text
Status = InProgress
```

```text
Priority in [High, Urgent]
```

```text
Assignee = me
```

```text
Label = bug
```

## UI

Crear un Filter Builder.

Ejemplo:

```text
Filter

Status      is       In Progress
Assignee    is       Me
Priority    in       High, Urgent
```

Permitir combinar condiciones.

## URL State

Los filtros deberían poder representarse en la URL.

Ejemplo conceptual:

```text
/issues?status=in_progress&assignee=me
```

Esto permitirá compartir vistas.

## Acceptance Criteria

* Los filtros funcionan correctamente.
* Se pueden combinar múltiples filtros.
* Los filtros se reflejan en la URL.
* La UI permite agregar y eliminar filtros.
* Tests para cada operador.
