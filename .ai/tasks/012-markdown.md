# Task 012 — Markdown

## Objective

Implementar soporte Markdown para descripciones y comentarios.

## Supported Syntax

Soportar:

* headings
* bold
* italic
* links
* unordered lists
* ordered lists
* code
* code blocks
* blockquotes
* tables

## Security

El Markdown debe sanitizarse correctamente antes de renderizar HTML.

Nunca permitir:

* JavaScript arbitrario
* scripts
* HTML peligroso

## Editor

Implementar un editor sencillo para:

* Issue Description
* Comments

Permitir alternar entre:

```text
Write
Preview
```

## Shortcuts

Soportar shortcuts comunes:

```text
Ctrl/Cmd + B
Ctrl/Cmd + I
```

## Acceptance Criteria

* Markdown se almacena como texto.
* Markdown se renderiza correctamente.
* HTML peligroso es sanitizado.
* Editor funciona.
* Preview funciona.
* Tests de sanitización.
