# Linear

Clon simplificado de Linear para gestión de issues, sprints y roadmaps.

La documentación funcional y técnica vive en [`.ai/`](.ai/). Las decisiones de
arquitectura están en [`.ai/architecture.md`](.ai/architecture.md) y el plan de trabajo
en [`.ai/tasks/`](.ai/tasks/).

## Requisitos

* .NET SDK 10.0
* Docker Desktop (para PostgreSQL)

## Puesta en marcha

1. Levantar PostgreSQL:

   ```bash
   docker compose up -d
   ```

2. Restaurar las herramientas de línea de comandos:

   ```bash
   dotnet tool restore
   ```

3. Aplicar las migraciones:

   ```bash
   dotnet ef database update --project src/Linear.Web
   ```

4. Ejecutar la aplicación:

   ```bash
   dotnet run --project src/Linear.Web
   ```

La aplicación arranca en `/teams`, el listado de equipos. El estado de la aplicación y de
la conexión a PostgreSQL se ve en Configuración.

### Certificado de desarrollo

Con el perfil `https`, los componentes Blazor consumen el API de la propia aplicación
por HTTPS, así que el certificado de desarrollo tiene que estar confiado:

```bash
dotnet dev-certs https --trust
```

Con el perfil `http` no hace falta.


## Acceso

La aplicación exige sesión iniciada: toda ruta que no diga explícitamente lo contrario
queda detrás de la política de autenticación.

En desarrollo se siembra una cuenta administradora la primera vez que arranca con la base
vacía. Las credenciales salen de `appsettings.Development.json`:

```text
admin@linear.local
Linear-Dev-1234
```

La siembra está desactivada por omisión (`Seed:Enabled`) y nunca se ejecuta en producción.
Todavía no hay alta de usuarios desde la interfaz: llega con la administración de equipos.


## Equipos

Todo el trabajo se organiza por equipos. Quien crea un equipo queda como su Owner, y el
equipo conserva siempre al menos uno.

| Rol | Puede |
| --- | --- |
| Owner | Todo lo de Admin, más eliminar el equipo y asignar o quitar el rol Owner. |
| Admin | Editar el equipo y administrar sus miembros (sin tocar a los Owner). |
| Member | Usar el equipo. |

La clave del equipo (`WEB`, `CORE`) no se puede cambiar después de creado: forma parte del
identificador de cada issue. Los miembros se suman por email y tienen que tener una cuenta
creada; no hay invitaciones por correo.

Un usuario que no pertenece a un equipo recibe la misma respuesta que si el equipo no
existiera, para que no sea posible averiguar qué equipos hay en la instalación.



## Labels

Cada equipo tiene su propio juego de labels para categorizar issues; no se comparten entre
equipos ni existen labels globales. El nombre es único dentro del equipo **sin distinguir
mayúsculas**, para que no convivan `bug` y `Bug`.

Leer las labels alcanza con pertenecer al equipo. Crearlas, editarlas y eliminarlas es
administrar la configuración del equipo, así que pide rol Admin u Owner.

El color se elige de una paleta y se guarda en hexadecimal. El servidor calcula si sobre
ese fondo el texto debe ir claro u oscuro —comparando ambos contrastes según WCAG— para que
cada lugar que dibuje una label no repita ese cálculo.

## Issues

El núcleo de la aplicación. Cada issue tiene un identificador legible y secuencial por
equipo —`WEB-1`, `WEB-2`— que arma el servidor combinando la clave del equipo con un
contador (`Teams.LastIssueNumber`) que se incrementa con un único `UPDATE ... RETURNING`
atómico: dos creaciones a la vez nunca reciben el mismo número, sin necesidad de un lock
explícito.

Crear, editar, cambiar estado/prioridad/responsable/estimate y archivar alcanza con ser
miembro del equipo. Eliminar un issue es definitivo y pide rol Admin u Owner, igual que
eliminar una label o el equipo mismo.

Un issue puede asignarse a cualquier miembro del equipo (no a alguien externo) y puede
tener varias labels, siempre y cuando pertenezcan al mismo equipo que el issue. Archivar
saca al issue del listado por omisión sin eliminarlo; sigue siendo alcanzable por su
identificador.

`SprintId` existe desde la task 007 y `RoadmapItemId` desde la task 010: el modelo de
dominio ya está completo, sin campos pendientes.

## Comentarios

Cada issue tiene su conversación. Comentar alcanza con pertenecer al equipo dueño del
issue: el permiso es el mismo que para verlo.

| Acción | Quién |
| --- | --- |
| Comentar | Cualquier miembro del equipo. |
| Editar | Solo el autor, sin importar el rol. |
| Eliminar | El autor, o un Admin u Owner moderando. |

Editar y eliminar no son el mismo permiso a propósito: un Admin modera **eliminando**, no
reescribiendo. Cambiar las palabras de otra persona dejaría de ser moderación.

Los permisos los calcula el servidor y viajan en cada comentario (`canEdit`, `canDelete`),
así que la interfaz no vuelve a deducir quién es el autor ni qué rol tiene; las reglas
viven en un solo lugar y no pueden desincronizarse.

La eliminación es lógica: la fila queda marcada con `DeletedAt` y desaparece del listado,
pero sobrevive para que la actividad de la task 011 —que es append-only— no termine
apuntando a una fila que ya no está. Un comentario eliminado se trata como inexistente:
no se puede editar ni volver a eliminar.

El contenido se guarda como Markdown crudo, sin interpretar, con un tope de 10.000
caracteres, y se renderiza al mostrarlo — ver [Markdown](#markdown). Guardar el texto sin
tocar fue justamente lo que permitió sumar el renderizado después sin migrar lo ya escrito.

El listado va del comentario más viejo al más nuevo —una conversación se lee en el orden en
que se escribió, al revés que el listado de issues— y está paginado como todos los demás.

A diferencia de `Issue` o `Label`, el modelo de dominio ubica `Comment` dentro del agregado
`Issue`. Acá es raíz propia: la colección crece sin techo, y cargarla entera cada vez que se
abre un issue chocaría con la paginación obligatoria de los listados.

## Markdown

Las descripciones de los issues y los comentarios se escriben en Markdown, con un editor de
dos pestañas —Escribir y Vista previa— y los atajos `Ctrl/Cmd + B` y `Ctrl/Cmd + I`. El texto
se guarda tal como se escribió: el renderizado ocurre al mostrarlo, nunca al guardarlo.

Se soporta lo que pide la task 012: títulos, negrita, itálica, enlaces, listas con y sin
orden, código en línea y en bloque, citas y tablas (con alineación).

### Seguridad: el HTML peligroso nunca llega a existir

El proyecto no incorpora dependencias nuevas (CLAUDE.md), así que no hay ni librería de
Markdown ni sanitizador. La restricción termina empujando a un diseño **más** seguro que el
habitual.

Lo normal es armar el HTML y después pasarle un sanitizador que le quite lo peligroso. Ese
enfoque recibe el daño ya hecho y trata de deshacerlo, y ahí es donde históricamente se
escapan cosas. Acá el renderizador **codifica todo el texto al salir y solo emite las
etiquetas que él mismo decide**: lo que alguien escriba como `<script>` se convierte en
texto visible, no en una etiqueta. No hay nada que sanear porque no se produce HTML del
usuario en ningún momento.

El único dato que termina dentro de un atributo es la dirección de un enlace, y ahí codificar
no alcanza —el navegador ejecuta `javascript:` aunque el texto esté bien escapado—. Por eso
el esquema se valida contra una **lista blanca** (`http`, `https`, `mailto`, y direcciones
relativas), después de quitar espacios y caracteres de control de toda la cadena: el
navegador ignora un tabulador metido en el medio, así que `java&#9;script:` se ejecutaría
igual si se comparara sin normalizar. Lo que no pasa la validación queda apuntando a `#`.

Los enlaces externos salen con `rel="noopener noreferrer"`: sin `noopener`, la página que se
abre puede redirigir a la que la abrió usando `window.opener`.

Hay 50 tests de seguridad. No buscan subcadenas en la salida sino que **inspeccionan las
etiquetas realmente emitidas**, porque `&lt;img onerror=x&gt;` contiene la palabra "onerror"
y es texto inerte, mientras que `<img onerror=x>` es un ataque: buscar la palabra confunde
las dos cosas.

### Qué no cubre

No pretende ser CommonMark completo. Lo que no reconoce queda como texto, que es la forma
segura de fallar. La limitación visible: tres delimitadores pegados —`**negrita *itálica***`—
no se desenredan, porque el cierre de la itálica y el de la negrita comparten caracteres y
resolverlo pide el algoritmo de carreras de delimitadores de CommonMark, más grande que todo
este renderizador. El anidado normal (`**fuerte con *suave* adentro**`) sí funciona.

A diferencia de CommonMark, un salto de línea simple se conserva como salto: en un gestor de
issues la gente escribe renglones cortos esperando que se respeten, no que se unan en un
párrafo.

## Actividad

Historial de lo que hace el equipo, con dos vistas: el feed del equipo
(`/teams/{clave}/activity`) y el historial de cada issue, debajo de sus comentarios.

Se registran las acciones que define la task 011: crear, editar, asignar, completar y
cancelar issues; crear y editar comentarios; agregar y quitar labels; iniciar y completar
sprints; y crear y actualizar iniciativas del roadmap. **Nada más**: crear un sprint, cambiar
una prioridad o archivar un issue no dejan registro, porque el vocabulario no define una
acción para eso. Como el historial es append-only, inventar términos sería agregar algo que
después no se puede corregir.

### Append-only, de verdad

`Activity` no expone un solo método que la modifique, ni siquiera interno, y no hay endpoint
de escritura: solo se lee. El historial sobrevive a lo que describe — eliminar un issue no
borra lo que quedó registrado sobre él, que es justamente lo que lo vuelve útil para
auditar. Solo se va con el equipo, por la clave foránea en cascada.

### Cómo se registra, sin tocar cada feature

La task pide un mecanismo común y evitar acoplar Activity a cada slice. Acá **ningún handler
menciona Activity**: no lo hacían antes de esta task y no lo hacen después.

El registro se arma en dos partes:

1. **El agregado levanta el evento**, en el método donde ocurre la acción. Es el único lugar
   donde se sabe qué significó el cambio: pasar un issue a `Done` es *completarlo*, no
   "editar el estado", y mirar la fila resultante no permite distinguirlo. Por eso la
   decisión vive en el dominio y no en un interceptor que compare columnas.
2. **Un interceptor de `SaveChanges`** convierte esos eventos en filas, completando el actor
   —que el dominio no conoce— y el equipo cuando hace falta.

La consecuencia que importa: el registro se inserta **en la misma transacción** que el
cambio. O se guardan los dos, o no se guarda ninguno; un historial que puede quedar
desfasado del dato que describe no serviría para auditar. Un interceptor lo garantiza; un
servicio llamado desde el handler, no.

Agregar una acción nueva es levantar un evento donde ocurre. No hay que acordarse de tocar
el slice, y un agregado nuevo solo tiene que implementar `IHasActivity`.

Sin sesión no se registra nada: el seeder y las migraciones crean datos sin usuario detrás, e
inventarle un actor al historial sería peor que no tenerlo.

### El historial de un issue incluye sus comentarios

Una actividad de comentario apunta al comentario, no al issue, así que no se puede pedir por
`EntityId`. El issue al que pertenece viaja dentro del payload, y la consulta lo busca ahí
con el operador de contención de `jsonb` (`@>`), que entra por un índice GIN.

Es la razón por la que `PayloadJson` es `jsonb` y no texto: permite filtrar por dentro sin
agregarle a la tabla una columna que la task no define.

El payload se guarda como JSON y no como columnas porque cada acción tiene su propia forma
—un cambio de estado lleva valor viejo y nuevo, una label lleva su identificador— y forzarlas
a un esquema común llenaría la tabla de nulos.

### La frase se arma en la interfaz

El historial guarda **qué pasó**, no cómo se cuenta. Convertir una entrada en «Ana completó
el issue · En curso → Hecho» es presentación, y dejarlo así permite reformularlo o traducirlo
sin migrar datos — importante en una tabla que no se puede reescribir. Una acción que la
interfaz no conozca se muestra igual con su nombre crudo, en vez de desaparecer del feed.

## Búsqueda

Buscador global de issues, con `Ctrl+K` (o `Cmd+K`) desde cualquier pantalla. Busca en el
identificador, el título, la descripción y los comentarios, y cruza **todos los equipos a
los que pertenece el usuario** — no solo el que está abierto.

Se recorre con el teclado: las flechas mueven la selección, Enter abre el issue y Escape
cierra. Los resultados muestran el equipo, porque al ser global pueden venir de varios.

### Cómo está armada

PostgreSQL Full Text Search, sin motores externos. `Issues` y `Comments` tienen cada uno una
columna `tsvector` **generada y guardada** (`STORED`) con un índice GIN encima. Guardarla
—en vez de calcularla en cada consulta— es lo que permite que el índice sirva, y exige que
la expresión sea IMMUTABLE: por eso la configuración de idioma va escrita como literal.

En los issues el título pesa más que la descripción (`A` contra `B`), así que un issue que
menciona el término en el título queda por encima de otro que solo lo nombra de pasada.

### Acentos

El diccionario es `spanish_unaccent`: el castellano de PostgreSQL con un paso previo de
`unaccent`. El diccionario de fábrica reduce las palabras a su raíz pero no toca los
acentos, y en castellano se escribe sin ellos todo el tiempo — sin este agregado, buscar
`autenticacion` no encontraría «autenticación». Encadenar `unaccent` normaliza las dos
puntas: da lo mismo cómo se escriba lo buscado y cómo se haya escrito lo guardado.
`unaccent` es una extensión que viene con PostgreSQL, no un motor aparte.

### Coincidencia por prefijo

El buscador responde mientras se escribe, así que cada palabra se busca como prefijo
(`auten:*` encuentra «autenticación»). Todas las palabras tienen que aparecer: al escribir
se va acotando el resultado.

El texto se reduce a letras y dígitos antes de armar la consulta, así que ningún carácter
con significado en `tsquery` —`&`, `|`, `!`, paréntesis, comillas— sobrevive; sin esa
limpieza, escribir un paréntesis suelto haría fallar la consulta con un error de sintaxis.

### Por qué son tres consultas y no una con OR

Un issue puede aparecer por su identificador, por su texto o por un comentario. Las tres
condiciones se resuelven como ramas separadas de un `UNION ALL` y recién después se juntan
y se ordenan.

Unirlas con `OR` en una sola consulta parece más simple, pero obliga a recorrer todos los
issues del usuario: con un `OR` de por medio el planificador no puede entrar por ningún
índice, y la búsqueda del comentario —que va correlacionada— termina ejecutándose una vez
por issue. Medido con 50.000 issues, esa versión recorría los 10.000 del usuario y evaluaba
10.000 veces la subconsulta de comentarios; con las ramas separadas cada una entra por su
índice y devuelve un puñado de candidatos.

Por el mismo motivo los parámetros se repiten en cada rama en lugar de calcularse una vez en
un CTE: con el CTE de por medio el planificador pierde de vista los valores y vuelve a los
recorridos secuenciales.

### Qué queda afuera

Los issues archivados, igual que en el listado. Los comentarios eliminados tampoco
coinciden. Debajo de dos caracteres no se consulta la base, y los resultados están acotados
a 20.

## Filtros

El listado de issues se filtra por estado, prioridad, responsable, label, sprint, iniciativa
del roadmap, autor y título. Las condiciones se combinan con Y, y hay como máximo una por
campo: cada campo es un parámetro de la query string, que es exactamente lo que muestra el
constructor de filtros —una fila por campo—.

### En la URL

Los filtros viven en la dirección, no en el estado de la pantalla. Así una vista se comparte
copiando la URL, y el botón de atrás del navegador deshace un filtro como cualquier otra
navegación:

```text
/teams/WEB/issues?status=not:Done&priority=High,Urgent&assignee=me
```

La expresión de cada parámetro es `[operador:]valor[,valor…]`. El operador casi nunca hace
falta escribirlo:

| Expresión | Operador | Se lee |
| --- | --- | --- |
| `status=InProgress` | `is` | es |
| `status=Todo,InProgress` | `in` | está en |
| `status=not:Done` | `isNot` | no es |
| `status=not:Done,Canceled` | `notIn` | no está en |
| `title=login` | `contains` | contiene |

Los campos filtrables son estado, prioridad, responsable, label, sprint, iniciativa del
roadmap (`roadmapItem`), autor y título.

`is`/`in` y `isNot`/`notIn` son en el fondo el mismo par —incluir y excluir— y solo se
distinguen por la cantidad de valores: "is X" es "in [X]". Por eso alcanza con el prefijo
`not:`, y la cantidad de valores decide el resto. Las formas largas (`is:`, `isNot:`, `in:`,
`notIn:`, `contains:`) se aceptan igual, para poder escribir una URL explícita a mano, y se
normalizan a la forma corta al volver a la dirección.

`contains` es solo para el título, y el título solo acepta `contains`. Es una coincidencia
parcial sin distinguir mayúsculas, no la búsqueda de la [task 009](.ai/tasks/009-search.md)
—esa recorre además descripción y comentarios con Full Text Search—.

### Valores especiales

| Valor | Campos | Significa |
| --- | --- | --- |
| `me` | responsable, autor | El usuario de la sesión. |
| `none` | responsable, sprint, iniciativa | Sin responsable / sin sprint / sin iniciativa. |

Las labels se pueden nombrar por identificador o por nombre (`label=bug`), porque el nombre
es único dentro del equipo. Un nombre que no existe devuelve un error en lugar de una lista
vacía: una vista compartida que en silencio no trae nada es más difícil de entender.

### Negar y los valores nulos

Cada condición se arma en afirmativo y, si el operador excluye, se niega entera. No es solo
para no duplicar código: es lo que da la semántica correcta con las columnas que admiten
nulos. **"Responsable no es Ana" incluye a los issues sin responsable**, que es lo que
cualquiera espera; escribir la condición negada a mano tendería a producir
`AssigneeId != ana`, y en SQL eso descarta los nulos y escondería justamente los issues sin
asignar. Lo mismo vale para labels y sprint.

El filtro por label es un `EXISTS`, no un `JOIN`: filtra sin multiplicar la fila del issue
por cada label que tenga, así que sigue siendo compatible con la paginación del listado. El
total que se devuelve es el de la consulta ya filtrada.

## Sprints

Un sprint agrupa el trabajo de un período acotado. Planificar es trabajo del día a día, no
configuración del equipo: alcanza con ser miembro, igual que para crear o mover un issue.

El ciclo de vida es `Planned` → `Active` → `Completed`, y desde cualquiera de los dos
primeros se puede cancelar. Completado y cancelado son terminales: un sprint cerrado es el
registro de lo que pasó en ese período y ya no admite cambios —ni de sus datos, ni de sus
issues—. Cancelar no marca `CompletedAt`: un sprint cancelado no se completó, y confundirlos
ensuciaría cualquier métrica que cuente sprints terminados.

### Un solo sprint activo por equipo

Es la regla central de la task y se sostiene en dos niveles.

El handler comprueba antes de iniciar, para dar un error claro en el caso normal. Pero por
sí solo no alcanza: entre leer que no hay ninguno activo y guardar el propio hay una ventana
en la que otro pedido puede leer lo mismo, y los dos terminarían activos.

Quien realmente lo impide es un **índice único parcial** en PostgreSQL:

```sql
CREATE UNIQUE INDEX "IX_Sprints_TeamId_Active"
    ON "Sprints" ("TeamId") WHERE "Status" = 'Active';
```

La base solo acepta una fila `Active` por equipo; las planificadas, completadas y canceladas
quedan fuera del índice y no compiten entre sí. Si dos pedidos concurrentes llegan a
guardar, uno gana y el otro recibe una violación de unicidad que el handler traduce al mismo
error de dominio. Es el mismo criterio que el número de issue: la garantía se apoya en el
motor, no en el orden en que corran los pedidos.

Completar o cancelar el sprint activo libera el cupo, porque el índice solo cuenta filas en
estado `Active`.

### Issues

Un issue pertenece a un único sprint, o a ninguno. La referencia vive en el issue
(`Issues.SprintId`), así que moverlo de sprint no obliga a cargar ninguno de los dos sprints
enteros; sumarlo a otro lo mueve, sin necesidad de sacarlo del anterior primero. Eliminar un
sprint —cosa que la aplicación no hace— dejaría sus issues sin sprint, no los borraría.

Las fechas son `DateOnly` y no instantes: un sprint dura días completos, y guardarlo como
`DateTimeOffset` obligaría a inventar una hora y a decidir en qué huso termina.

### Tablero y métricas

El detalle del sprint es un tablero de cuatro columnas —Todo, En curso, En revisión,
Hecho— con las métricas que pide la task: total, completados, pendientes y porcentaje.

Los issues en `Backlog` se dibujan en la columna Todo: las dos cosas significan que el
trabajo no arrancó, y el tablero de un sprint no distingue entre ambas. Los cancelados no
son una columna, así que se cuentan en una línea aparte debajo del tablero — ningún issue
del sprint desaparece sin dejar rastro. Las métricas cuentan todos los issues asignados, en
el estado que sea.

El listado calcula las métricas de todos los sprints de la página con una única consulta
agrupada. El detalle sí trae los issues completos: el tablero los necesita a todos para ser
un tablero, y un sprint es por definición un lote acotado de trabajo —no el listado abierto
del equipo, que sigue paginado—.

## Roadmap

Mientras el sprint dice en qué quincena se trabaja un issue, el roadmap dice a qué objetivo
de mediano plazo aporta. Son dos ejes distintos y compatibles: un issue puede estar en un
sprint, en una iniciativa, en ambos o en ninguno.

Un equipo puede tener varios roadmaps, y cada uno agrupa **iniciativas**: tramos de trabajo
con nombre, fechas y estado.

| Estado | Significa |
| --- | --- |
| Planificada | Todavía no arrancó. |
| En curso | Se está trabajando. |
| Completada | Terminada. |
| Cancelada | Se descartó. |

A diferencia del sprint, **no hay un recorrido obligatorio de estados**: una iniciativa puede
volver de "en curso" a "planificada" si se despriorizó, o reabrirse después de darse por
terminada. El roadmap es una intención revisable, no un proceso con pasos, y la task 010 no
define transiciones.

Las fechas son `DateOnly` y la fecha objetivo tiene que ser posterior a la de inicio, igual
que en los sprints: una iniciativa dura semanas o meses, y guardarla como `DateTimeOffset`
obligaría a inventar una hora.

### Las iniciativas viven dentro del roadmap

`Roadmap` es la raíz de su agregado y sus iniciativas viven adentro, tal como lo define el
modelo de dominio. Acá sí corresponde —y en `Comment` no correspondió— porque la cantidad
está acotada por naturaleza: un roadmap junta unas decenas de iniciativas, no una
conversación que crece sin techo. Y sobre todo porque la línea de tiempo las necesita todas
a la vez para poder dibujarse; paginarlas sería dibujar media línea de tiempo.

Los issues asociados, en cambio, **no** están en el agregado: es el issue el que guarda su
`RoadmapItemId`, porque `Issue` es raíz de su propio agregado y su cantidad no tiene techo.

### Permisos

Planificar es trabajo del día a día: crear y editar roadmaps e iniciativas, y asociar
issues, alcanza con pertenecer al equipo. **Eliminar** un roadmap o una iniciativa es
definitivo y pide rol Admin u Owner, el mismo criterio que rige para eliminar un issue, una
label o el equipo mismo.

Eliminar no arrastra los issues: la clave foránea los desasocia (`SetNull`). El trabajo
sobrevive al plan que lo agrupaba.

### Línea de tiempo

El detalle del roadmap es una línea de tiempo: una fila por iniciativa, con la barra ubicada
según sus fechas y rellena según su avance (los issues asociados que ya están en `Done`).

La escala se calcula en porcentajes, no en píxeles: el ancho real lo pone el CSS, así que se
adapta sola a la pantalla. El rango arranca el primer día del mes de la iniciativa más
temprana y termina el último del mes de la más tardía — redondear a meses completos es lo
que permite dibujar la cabecera con columnas parejas.

El ancho de cada columna y de cada barra sale de **restar dos bordes ya redondeados**, no de
redondear el ancho por separado: así el borde derecho de una columna cae exactamente sobre
el izquierdo de la siguiente y las columnas embaldosan sin huecos.

### Ver los issues de una iniciativa

Al elegir una iniciativa, la línea de tiempo lista sus issues usando el **filtro del listado
de issues** (`roadmapItem`), el mismo mecanismo de la task 008 — no hay un camino aparte
para la misma pregunta. Ese filtro acepta identificadores y `none`, igual que `sprint`.

## Atajos de teclado

| Atajo | Qué hace |
| --- | --- |
| `C` | Crear issue |
| `/` | Buscar |
| `Ctrl/⌘ K` | Búsqueda global |
| `G` `I` | Ir a Issues |
| `G` `S` | Ir a Sprints |
| `G` `R` | Ir a Roadmap |
| `?` | Ver la ayuda de atajos |
| `Esc` | Cerrar el diálogo abierto |
| `Enter` | Confirmar el diálogo, o abrir el resultado marcado en la búsqueda |
| `Ctrl/⌘ Enter` | Enviar el comentario que se está escribiendo |
| `Ctrl/⌘ B` · `Ctrl/⌘ I` | Negrita e itálica, dentro del editor |

`?` abre esa misma lista dentro de la aplicación.

### Un solo listener

Hay **un único** `keydown` en toda la aplicación, en `KeyboardShortcuts.razor.js`, montado
desde el layout. Ningún componente escucha el teclado por su cuenta: cuando un atajo coincide,
JavaScript avisa a .NET con el identificador y el componente decide qué hacer.

Esto obligó a rehacer el `Ctrl+K` de la búsqueda, que hasta la task 009 se registraba en su
propio componente. Ahora el botón de la cabecera y el atajo terminan los dos en
`SearchDialogLauncher`, un servicio con ámbito de circuito que además evita que se apilen dos
diálogos si el atajo se repite con uno ya abierto.

### El filtrado ocurre en JavaScript

En Blazor Server, mandar cada tecla al servidor para que decida si le interesa sería **una ida
y vuelta por pulsación**. El motor recibe la tabla al registrarse y solo cruza el circuito
cuando un atajo realmente coincidió.

### La tabla vive en C#

`AppShortcuts.All` es la única fuente: de ahí salen tanto lo que responde el teclado como la
pantalla de ayuda, así que **no pueden divergir**. Agregar un atajo es agregar una fila.

Al navegador viaja `AppShortcuts.Bindings`, una proyección con lo justo para comparar la
pulsación; los textos de la ayuda se quedan del lado del servidor.

Las filas marcadas `Global = false` —`Esc`, `Enter`, `Ctrl+B`— se listan en la ayuda pero no
las toma el motor: las resuelve el control que tiene el foco, que es el único que sabe qué se
está escribiendo. Un manejador global de `Enter` rompería cualquier formulario.

### No interferir con la escritura

Con el foco en un `input`, `textarea`, `select` o algo `contenteditable`, los atajos **no se
disparan**: una `c` es una `c`. La única excepción es `Ctrl/⌘ K`, marcada `AllowInEditor`,
porque buscar desde adentro de un editor es razonable y es el atajo que ya está incorporado de
otras herramientas.

Tampoco se toma nada con `Alt`, ni combinaciones con `Ctrl` que no estén en la tabla: `Ctrl+C`
sigue copiando.

### Windows, Linux y Mac

El motor acepta `ctrlKey` **o** `metaKey`, así que el mismo atajo funciona con Ctrl y con Cmd
sin detectar el sistema operativo —detectar el navegador es frágil, y quien usa un teclado
externo puede tener cualquiera de las dos—. Por eso la ayuda dice `Ctrl/⌘`.

### Las secuencias

`G` `I` es una secuencia, no una combinación. La primera tecla queda armada **1,5 segundos**:
suficiente para escribir la segunda sin apuro, y corto para que una `g` suelta no quede
esperando indefinidamente. Si la segunda tecla no completa ninguna secuencia, se evalúa como
atajo simple —`G` y después `C` crea un issue—.

## Tiempo real

Lo que hace un integrante del equipo aparece en la pantalla de los demás sin refrescar: issues
que se crean o cambian, comentarios, sprints y el feed de actividad.

### Un solo lugar produce los avisos

Ningún handler menciona el tiempo real, igual que ninguno menciona la actividad. Los avisos
salen de un `SaveChangesInterceptor` que mira lo que se está guardando, así que **una operación
nueva avisa por el solo hecho de guardar**.

La diferencia con el interceptor de actividad está en el momento:

| | Cuándo | Por qué |
| --- | --- | --- |
| Actividad | Antes de confirmar | Tiene que viajar en la misma transacción que el cambio. |
| Tiempo real | Después de confirmar | Un aviso no se puede deshacer. |

Anunciar un cambio que después se revierte deja a todos los clientes mostrando algo que nunca
pasó. Por eso los avisos se calculan en `SavingChanges` —única oportunidad de ver qué cambió— y
se emiten en `SavedChanges`.

### Qué se avisa

`RealtimeEvent` es a propósito **más pobre** que `ActivityAction`. El historial necesita saber
que pasar un issue a `Done` fue *completarlo* y no *editarlo*; un cliente conectado no: solo
necesita saber que el issue cambió para volver a pedirlo.

Por eso cambiar estado, prioridad, responsable, labels o estimación llegan todos como
`IssueUpdated`. Todas las mutaciones pasan por el agregado y todas tocan `UpdatedAt`, así que
ninguna se escapa —y no hay una lista que se desactualice con el primer método nuevo—.

`IssueDeleted` es el caso que **no** podría derivarse del historial: eliminar es definitivo y no
deja registro, porque el historial es append-only y la task 011 no definió esa acción. Sale del
propio guardado.

El aviso lleva lo justo para que quien lo recibe decida si le interesa y vuelva a pedir el dato,
nunca el issue ni el comentario en sí. Mandar el estado completo obligaría a resolver ahí los
permisos de cada destinatario y a duplicar el mapeo a DTO; avisar y dejar que el cliente pida
por el camino de siempre reusa el handler que ya aplica esas reglas.

### Aislamiento por equipo

Cada equipo es un grupo de SignalR, nombrado por **identificador** y no por clave: la clave se
puede cambiar, y una conexión suscripta con la vieja seguiría —o dejaría de— recibir según el
orden en que pasaran las cosas.

El aislamiento no depende de que el cliente filtre lo que recibe: a un grupo **solo se entra
demostrando que se pertenece al equipo**. La comprobación ocurre al suscribirse y no al emitir,
porque emitir ocurre una vez por cambio y tiene que ser barato.

Alcanza con el rol `Member`: recibir avisos es leer, y leer es lo que cualquier integrante ya
puede hacer por la interfaz. A quien no pertenece se le responde igual que si el equipo no
existiera, misma política que en el resto de la aplicación.

### Dos transportes, un solo contrato

```text
SaveChanges (confirmado)
        ↓
  ITeamNotifier
    ├──→ TeamHub  ──→ clientes de SignalR
    └──→ suscriptores en proceso ──→ componentes Blazor Server
```

Los dos reciben el mismo `TeamNotification`; lo que cambia es el transporte.

Las pantallas usan el camino en proceso. Un componente Blazor Server **ya corre en el
servidor** y ya tiene su propio canal con el navegador —el circuito—: hacerlo abrir una conexión
de SignalR contra su propia aplicación sumaría un websocket y una ronda de autenticación por
usuario para entregar un mensaje que nace a metros de distancia.

Eso no vuelve opcional el control de acceso del hub: `TeamRealtimeSubscriber` comprueba la
pertenencia con la misma regla antes de suscribir. Si la comprobación viviera solo en el hub,
el camino en proceso sería una forma de recibir los cambios de un equipo ajeno.

El hub sigue siendo el transporte real para cualquier cliente fuera del circuito, y es lo que
cubren los tests de autorización.

### En la pantalla

Un componente `<TeamRealtime Key="..." OnNotification="..." />` resuelve el alta, la baja y el
salto al hilo del renderizador —el aviso llega en el hilo de quien guardó el cambio, que es otra
persona—. Qué recargar lo decide cada pantalla, y ninguna recarga entera: el listado relee su
página con los filtros puestos, el hilo de comentarios relee las páginas que ya tenía, el
tablero de sprint no vuelve a mostrar el esqueleto de carga.

Quien hizo el cambio **no recibe su propio eco**: su pantalla ya se actualizó con la respuesta
de la operación, y recargarla le movería el scroll o le cerraría un menú abierto.

### Conflictos

La estrategia de la V1 es **optimista y explícita**: el que llega segundo no pisa al primero en
silencio, se entera.

Al guardar el título o la descripción, la interfaz manda `expectedUpdatedAt`, la versión que
tenía a la vista. Si no coincide con la guardada, el servidor responde `409` en lugar de
escribir. Se usa `UpdatedAt` como versión y no una columna nueva porque ese valor ya viaja en la
respuesta del issue.

La comparación no puede hacerse solo dentro del handler: entre que lee y guarda pasan
microsegundos, y el conflicto real ocurre en los minutos que alguien pasa escribiendo. Por eso
la versión la aporta el cliente. Omitirla equivale a "guardá igual", que es lo razonable para un
cliente de API que no mostró nada antes de escribir.

Del lado de la pantalla, si llega un cambio ajeno **mientras hay texto sin guardar**, no se
recarga: se avisa y se conserva el borrador. Tampoco se adopta la versión nueva —eso volvería
aceptable un guardado que pisa el cambio ajeno—, así que al guardar el servidor lo rechaza y la
persona decide. Un botón descarta el borrador y se queda con lo guardado.

No hay fusión de cambios ni edición colaborativa: la task 014 las excluye explícitamente, y
frente a la duda gana la consistencia.

## Datos de ejemplo

Hay un seeder que carga usuarios y equipos para poder recorrer la aplicación con contenido
en lugar de pantallas vacías. Se ejecuta al arrancar, activándolo por línea de comandos:

```bash
dotnet run --project src/Linear.Web -- --Seed:SampleData=true
```

También puede dejarse encendido cambiando `Seed:SampleData` a `true` en
`appsettings.Development.json`, o mediante la variable de entorno `Seed__SampleData=true`.

Es idempotente: volver a ejecutarlo no duplica nada y completa lo que falte. Nunca corre
en producción.

### Qué carga

Cinco cuentas, todas con la contraseña de `Seed:SamplePassword` (`Linear-Dev-1234`):

| Cuenta | Nombre | Estado |
| --- | --- | --- |
| `ana.perez@linear.dev` | Ana Pérez | Activa |
| `bruno.gimenez@linear.dev` | Bruno Giménez | Activa |
| `carla.rossi@linear.dev` | Carla Rossi | Activa (Admin de la instalación) |
| `diego.molina@linear.dev` | Diego Molina | Activa |
| `elena.vargas@linear.dev` | Elena Vargas | **Desactivada**, a propósito |

Cada equipo recibe además cinco labels (`bug`, `mejora`, `documentación`, `deuda técnica`,
`urgente`).

Y tres equipos. El reparto de roles está pensado para que la cuenta administradora quede
como Owner de uno, Admin de otro y Member del tercero, y así se puedan ver los tres niveles
de permiso sin cambiar de sesión:

| Equipo | Owner | Rol de `admin@linear.local` |
| --- | --- | --- |
| `WEB` — Web | Ana Pérez | Admin |
| `CORE` — Core Platform | admin | Owner |
| `MOBILE` — Mobile | Carla Rossi | Member |

## Estructura

```text
src/
├── Linear.Domain/     Entidades, reglas y Result Pattern. Sin dependencias de infraestructura.
└── Linear.Web/
    ├── Components/    UI Blazor: layout, páginas y tema.
    ├── Features/      Vertical slices: una carpeta por operación.
    ├── Infrastructure/EF Core, autenticación y autorización por equipo.
    └── Shared/        Paginación y mapeo de errores a HTTP.

tests/
├── Linear.UnitTests/        Dominio y primitivas compartidas.
└── Linear.IntegrationTests/ Endpoints y navegación, sobre la aplicación completa en memoria.
```

## Comandos

```bash
dotnet build
```

```bash
dotnet test
```


Los tests de integración de autenticación corren contra PostgreSQL real, sobre una base
`linear_tests` que se recrea en cada ejecución. Necesitan el contenedor levantado:

```bash
docker compose up -d
```

Se puede apuntar a otra instancia con la variable de entorno `LINEAR_TEST_POSTGRES`.

Crear una migración nueva:

```bash
dotnet ef migrations add NombreDeLaMigracion --project src/Linear.Web --output-dir Infrastructure/Persistence/Migrations
```

## Configuración

| Ajuste | Descripción |
| --- | --- |
| `ConnectionStrings:Postgres` | Conexión a PostgreSQL. En producción, `ConnectionStrings__Postgres`. |
| `Authentication:RequireHttps` | Exige que la cookie de sesión viaje solo por HTTPS. Por omisión, activo fuera de desarrollo. |
| `Seed:Enabled` | Crea la cuenta administradora inicial si no hay usuarios. Nunca se ejecuta en producción. |
| `Seed:SampleData` | Carga usuarios y equipos de ejemplo. Idempotente. Nunca se ejecuta en producción. |
