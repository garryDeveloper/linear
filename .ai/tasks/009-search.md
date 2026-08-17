# Task 009 — Search

## Objective

Implementar búsqueda global de issues.

## Search Fields

Buscar inicialmente en:

* Issue identifier
* title
* description
* comments

## Technology

Utilizar PostgreSQL Full Text Search.

No introducir Elasticsearch, OpenSearch u otro motor externo.

## UI

Crear Command/Search dialog.

Shortcut:

```text
Ctrl+K
```

o

```text
Cmd+K
```

Debe permitir:

```text
Search issues...
```

Mostrar resultados:

```text
WEB-123
Fix authentication bug

WEB-124
Implement login screen
```

## Performance

* utilizar índices apropiados
* limitar resultados
* debounce en búsqueda
* evitar consultas innecesarias

## Acceptance Criteria

* búsqueda global funcional.
* resultados relevantes.
* búsqueda por identifier.
* búsqueda por título.
* búsqueda por descripción.
* búsqueda por comentarios.
* tests.
