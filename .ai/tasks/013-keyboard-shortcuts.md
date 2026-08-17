# Task 013 — Keyboard Shortcuts

## Objective

Implementar shortcuts para acelerar la navegación y gestión de issues.

## Shortcuts

Implementar inicialmente:

```text
C              Create issue
/
                Focus search

Ctrl/Cmd + K   Global search

G then I       Issues

G then S       Sprints

G then R       Roadmap

Esc            Close modal/dialog

Enter          Confirm

Ctrl/Cmd + Enter
               Submit comment
```

## Rules

Los shortcuts no deben ejecutarse cuando el usuario está escribiendo en:

* input
* textarea
* editor
* select

Excepto shortcuts explícitamente diseñados para funcionar dentro del editor.

## Architecture

Centralizar el manejo de shortcuts.

No implementar listeners independientes en cada componente.

## UI

Crear una pantalla o modal de ayuda:

```text
Keyboard Shortcuts

Create issue             C
Search                   /
Global search            Cmd K
Issues                   G I
Sprints                  G S
Roadmap                  G R
```

## Acceptance Criteria

* shortcuts funcionan.
* no interfieren con inputs.
* funcionan correctamente en Windows/Linux.
* Mac utiliza Cmd cuando corresponda.
* ayuda de shortcuts disponible.
