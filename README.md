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

La página de inicio muestra el estado de la aplicación y de la conexión a PostgreSQL.

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
