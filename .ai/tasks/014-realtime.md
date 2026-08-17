# Task 014 — Realtime

## Objective

Agregar actualización en tiempo real mediante SignalR.

## Use Cases

Actualizar automáticamente cuando otro usuario:

* crea un issue
* modifica un issue
* cambia status
* cambia priority
* asigna un usuario
* agrega/remueve labels
* crea un comentario
* modifica un sprint

## Architecture

Utilizar SignalR.

Crear un hub relacionado con el contexto de Team.

Conceptualmente:

```text
Client
  ↓
SignalR Hub
  ↓
Team
```

Los usuarios solo deben recibir eventos de Teams a los que pertenecen.

## Events

Definir eventos tipados o contratos claros.

Ejemplo:

```text
IssueCreated
IssueUpdated
IssueDeleted
CommentCreated
SprintUpdated
ActivityCreated
```

## UI Behavior

Cuando llega un evento:

* actualizar el estado correspondiente
* evitar refresh completo
* mantener la posición actual del usuario cuando sea posible

## Conflict Handling

Si dos usuarios modifican el mismo issue:

* no sobrescribir silenciosamente cambios locales
* definir una estrategia simple para la V1
* priorizar consistencia sobre edición colaborativa compleja

No implementar colaboración tipo Google Docs.

## Acceptance Criteria

* Los eventos llegan a clientes conectados.
* Los eventos están aislados por Team.
* Issues se actualizan automáticamente.
* Comentarios aparecen sin refresh.
* Activity Feed se actualiza.
* Tests de autorización del Hub.
