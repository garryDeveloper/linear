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

`RoadmapItemId` —del modelo de dominio— todavía no existe como columna: RoadmapItem es de
la task 010. Se suma cuando esa entidad exista, en vez de guardar una referencia a una
tabla que no está. `SprintId` ya existe, desde la task 007.

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
caracteres. Por ahora se muestra tal cual, respetando los saltos de línea; renderizarlo y
sanitizarlo es de la [task 012](.ai/tasks/012-markdown.md), y guardar el texto sin tocar es
justamente lo que deja implementarla después sin migrar lo ya escrito.

El listado va del comentario más viejo al más nuevo —una conversación se lee en el orden en
que se escribió, al revés que el listado de issues— y está paginado como todos los demás.

A diferencia de `Issue` o `Label`, el modelo de dominio ubica `Comment` dentro del agregado
`Issue`. Acá es raíz propia: la colección crece sin techo, y cargarla entera cada vez que se
abre un issue chocaría con la paginación obligatoria de los listados.

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
