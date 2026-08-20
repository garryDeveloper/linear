// Ancho de la ventana, para decidir si la barra lateral empuja al contenido o se le
// superpone.
//
// Se mide acá y no con el servicio de breakpoints de MudBlazor porque aquel devuelve un valor
// por omisión antes de haber medido: en una pantalla de 1280px informaba "Xs", y la barra
// quedaba superpuesta y cerrada en escritorio. window.innerWidth no tiene ese estado
// intermedio.

let handler = null;

/// Si la ventana es más angosta que el límite dado.
export function isNarrow(maxWidth) {
    return window.innerWidth <= maxWidth;
}

/// Avisa a .NET cada vez que la ventana cruza el límite, para no depender de recargar.
export function observe(dotNetRef, maxWidth) {
    unobserve();

    let last = isNarrow(maxWidth);

    handler = () => {
        const narrow = isNarrow(maxWidth);

        // Solo cuando cambia de lado: redimensionar dispara decenas de eventos por segundo y
        // cada aviso cruza el circuito.
        if (narrow !== last) {
            last = narrow;
            dotNetRef.invokeMethodAsync("OnWidthChangedAsync", narrow);
        }
    };

    window.addEventListener("resize", handler);
}

export function unobserve() {
    if (handler) {
        window.removeEventListener("resize", handler);
        handler = null;
    }
}
