// Atajo global del buscador: Ctrl+K en Windows/Linux, Cmd+K en macOS.
//
// Se escucha en captura (`true`) para llegar antes que cualquier control que también use la
// tecla, y se cancela el evento porque Ctrl+K está tomado por el navegador —en Chrome y
// Firefox enfoca la barra de direcciones—.
let handler = null;

export function register(dotNetRef) {
    unregister();

    handler = (event) => {
        if (event.key !== "k" && event.key !== "K") {
            return;
        }

        if (!event.ctrlKey && !event.metaKey) {
            return;
        }

        event.preventDefault();
        dotNetRef.invokeMethodAsync("OpenAsync");
    };

    document.addEventListener("keydown", handler, true);
}

export function unregister() {
    if (handler) {
        document.removeEventListener("keydown", handler, true);
        handler = null;
    }
}
