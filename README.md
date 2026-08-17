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

## Estructura

```text
src/
├── Linear.Domain/     Entidades, reglas y Result Pattern. Sin dependencias de infraestructura.
└── Linear.Web/
    ├── Components/    UI Blazor: layout, páginas y tema.
    ├── Features/      Vertical slices: una carpeta por operación.
    ├── Infrastructure/EF Core, DbContext y migraciones.
    └── Shared/        Paginación, mapeo de errores a HTTP y cliente del API interno.

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

Crear una migración nueva:

```bash
dotnet ef migrations add NombreDeLaMigracion --project src/Linear.Web --output-dir Infrastructure/Persistence/Migrations
```

## Configuración

| Ajuste | Descripción |
| --- | --- |
| `ConnectionStrings:Postgres` | Conexión a PostgreSQL. En producción, `ConnectionStrings__Postgres`. |
| `Api:BaseAddress` | Dirección base del API interno. Vacía: se resuelve desde las direcciones de Kestrel. Obligatoria detrás de un proxy inverso. |
