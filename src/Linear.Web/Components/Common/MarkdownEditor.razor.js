// Ayuda para el editor de Markdown: envolver la selección y reponer el cursor.
//
// Vive en JavaScript porque la selección de un textarea —dónde empieza, dónde termina, dónde
// dejar el cursor— solo existe en el navegador: Blazor no la ve.

// Envuelve (o desenvuelve) la selección con un delimitador de Markdown.
//
// Devuelve el texto nuevo junto con dónde debería quedar la selección, pero NO la aplica: el
// valor todavía tiene que dar la vuelta por Blazor, y el re-render posterior pisaría
// cualquier cursor que se pusiera acá. Reponerlo es tarea de setSelection, que el componente
// llama una vez que el DOM ya se actualizó.
export function wrapSelection(textarea, delimiter) {
    if (!textarea) {
        return null;
    }

    const { selectionStart: start, selectionEnd: end, value } = textarea;

    const selected = value.slice(start, end);
    const before = value.slice(0, start);
    const after = value.slice(end);

    // Si ya estaba envuelta, se desenvuelve: el atajo alterna, como en cualquier editor.
    // Apretar Ctrl+B dos veces tiene que dejar el texto como estaba.
    if (before.endsWith(delimiter) && after.startsWith(delimiter)) {
        return {
            value: before.slice(0, -delimiter.length) + selected + after.slice(delimiter.length),
            start: start - delimiter.length,
            end: end - delimiter.length
        };
    }

    // Sin selección, el cursor queda entre los delimitadores, listo para escribir. Con
    // selección, se vuelve a marcar el mismo texto ya envuelto.
    const caret = start + delimiter.length;

    return {
        value: `${before}${delimiter}${selected}${delimiter}${after}`,
        start: caret,
        end: caret + selected.length
    };
}

export function setSelection(textarea, start, end) {
    if (!textarea) {
        return;
    }

    textarea.focus();
    textarea.setSelectionRange(start, end);
}
