// Motor central de atajos de teclado.
//
// Un único listener para toda la aplicación, como pide la task 013. La tabla la manda C# al
// registrarse, así que la ayuda y lo que responde el teclado no pueden divergir.
//
// El filtrado ocurre acá y no del lado de .NET a propósito: en Blazor Server, mandar cada
// tecla por el circuito para que el servidor decida si le interesa sería una ida y vuelta por
// pulsación. JavaScript solo avisa cuando un atajo realmente coincidió.

let shortcuts = [];
let dotNetRef = null;
let handler = null;

// Estado de las secuencias tipo "G luego I".
let chord = null;
let chordTimer = null;

// Cuánto espera la segunda tecla de una secuencia. Suficiente para escribirla sin apuro, y
// corto para que una "g" suelta no quede armada indefinidamente.
const CHORD_TIMEOUT_MS = 1500;

export function register(reference, table) {
    unregister();

    dotNetRef = reference;
    shortcuts = table ?? [];

    handler = (event) => onKeyDown(event);

    // En captura, para llegar antes que cualquier control que también use la tecla.
    document.addEventListener("keydown", handler, true);
}

export function unregister() {
    if (handler) {
        document.removeEventListener("keydown", handler, true);
        handler = null;
    }

    clearChord();
    dotNetRef = null;
    shortcuts = [];
}

function onKeyDown(event) {
    // Con Alt de por medio no hay atajo nuestro; suele ser del sistema o del navegador.
    if (event.altKey || event.isComposing) {
        return;
    }

    const key = event.key.toLowerCase();

    // Las teclas modificadoras solas no disparan ni cortan una secuencia en curso.
    if (key === "control" || key === "meta" || key === "shift" || key === "alt") {
        return;
    }

    const editing = isEditing(event.target);
    const ctrl = event.ctrlKey || event.metaKey;

    // Segunda tecla de una secuencia.
    if (chord) {
        const pending = chord;
        clearChord();

        if (!editing && !ctrl) {
            const match = shortcuts.find((s) => s.chord === pending && s.key === key);

            if (match) {
                trigger(event, match);
                return;
            }
        }
    }

    const match = shortcuts.find(
        (s) => !s.chord && s.key === key && Boolean(s.ctrl) === ctrl
    );

    if (match) {
        // Escribiendo, solo pasan los atajos pensados para funcionar dentro del editor.
        if (editing && !match.allowInEditor) {
            return;
        }

        trigger(event, match);
        return;
    }

    // ¿Empieza una secuencia? Nunca mientras se escribe: una "g" tiene que ser una "g".
    if (!editing && !ctrl && shortcuts.some((s) => s.chord === key)) {
        chord = key;
        chordTimer = window.setTimeout(clearChord, CHORD_TIMEOUT_MS);
    }
}

function trigger(event, shortcut) {
    // Se cancela el comportamiento por omisión: varios de estos atajos están tomados por el
    // navegador —Ctrl+K enfoca la barra de direcciones, "/" abre la búsqueda rápida—.
    event.preventDefault();
    event.stopPropagation();

    dotNetRef?.invokeMethodAsync("HandleAsync", shortcut.id);
}

/// Si el foco está en algo donde se escribe.
function isEditing(target) {
    if (!target) {
        return false;
    }

    if (target.isContentEditable) {
        return true;
    }

    const tag = target.tagName;

    return tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";
}

function clearChord() {
    chord = null;

    if (chordTimer) {
        window.clearTimeout(chordTimer);
        chordTimer = null;
    }
}
