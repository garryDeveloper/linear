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
