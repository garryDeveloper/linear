using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Linear.Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Diccionario de la búsqueda: 'spanish' con un paso previo de unaccent.
            //
            // El diccionario castellano reduce las palabras a su raíz pero no toca los
            // acentos, y en castellano se escribe sin ellos todo el tiempo: sin esto,
            // buscar "autenticacion" no encontraría "autenticación". Encadenar unaccent
            // normaliza las dos puntas —lo que se guarda y lo que se busca—.
            //
            // Tiene que crearse antes que las columnas: son columnas generadas que la
            // nombran, así que la configuración ya tiene que existir.
            //
            // Se hace con SQL porque no hay forma de expresar una configuración de búsqueda
            // de texto con la API de migraciones de EF.
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS unaccent;""");

            migrationBuilder.Sql(
                """
                CREATE TEXT SEARCH CONFIGURATION spanish_unaccent (COPY = spanish);
                ALTER TEXT SEARCH CONFIGURATION spanish_unaccent
                    ALTER MAPPING FOR hword, hword_part, word
                    WITH unaccent, spanish_stem;
                """);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Issues",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "setweight(to_tsvector('spanish_unaccent', coalesce(\"Title\", '')), 'A') ||\nsetweight(to_tsvector('spanish_unaccent', coalesce(\"Description\", '')), 'B')",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Comments",
                type: "tsvector",
                nullable: true,
                computedColumnSql: "to_tsvector('spanish_unaccent', coalesce(\"Content\", ''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_SearchVector",
                table: "Issues",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_SearchVector",
                table: "Comments",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            // Buscar por identificador es un LIKE por prefijo ("WEB-1%"). El índice único
            // que ya existe sobre Identifier no sirve para eso: con la colación por omisión,
            // un btree solo resuelve LIKE por prefijo si se declara con text_pattern_ops.
            // Va escrito a mano porque EF no sabe expresar una clase de operadores.
            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_Issues_Identifier_Prefix"
                    ON "Issues" ("Identifier" text_pattern_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Issues_Identifier_Prefix";""");

            migrationBuilder.DropIndex(
                name: "IX_Issues_SearchVector",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Comments_SearchVector",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Comments");

            // Después de las columnas: mientras existan, dependen de la configuración.
            // La extensión no se elimina, porque puede haberla instalado otra cosa.
            migrationBuilder.Sql("""DROP TEXT SEARCH CONFIGURATION IF EXISTS spanish_unaccent;""");
        }
    }
}
