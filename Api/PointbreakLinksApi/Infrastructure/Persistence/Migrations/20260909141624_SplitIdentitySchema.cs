using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitIdentitySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Not owned by business anymore — Identity.Infrastructure's InitialCreate migration
            // creates its own copies of these two in the "identity" schema (no data worth
            // preserving: refresh tokens are one-per-user upserts, reset tokens are short-lived
            // and single-use).
            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            // The actual bounded-context split: users/roles/role_user physically relocate into
            // their own schema, preserving every row (Postgres FK constraints survive
            // `ALTER TABLE ... SET SCHEMA` — they track the referenced table by OID, not by
            // schema-qualified name — so every existing FK from projects/sites/purchased_sites/
            // etc. into "users" keeps working with zero further changes). This business context
            // stops owning DDL for these three tables from here on (see UserConfiguration's/
            // RoleConfiguration's ExcludeFromMigrations) — Identity.Infrastructure's own
            // migration history takes over their schema ownership after this point, which is
            // exactly why the auto-generated diff above (before this hand-edit) tried to delete
            // the seeded role rows as "no longer wanted" — they're not gone, just relocated.
            migrationBuilder.Sql(
                """
                CREATE SCHEMA IF NOT EXISTS identity;
                ALTER TABLE users SET SCHEMA identity;
                ALTER TABLE roles SET SCHEMA identity;
                ALTER TABLE role_user SET SCHEMA identity;
                """);

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallets_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallets_UserId",
                table: "wallets",
                column: "UserId",
                unique: true);

            // Balance used to live on users (added this session, before the Identity split) —
            // backfill every existing balance into its own Wallet row, then drop the now-unused
            // column from the Identity-owned table (harmless to leave, but tidy to remove: no
            // mapped C# property references it anymore on either side of the split).
            migrationBuilder.Sql(
                """
                INSERT INTO wallets ("UserId", "Balance", "CreatedAt", "UpdatedAt")
                SELECT "Id", "Balance", now(), now() FROM identity.users;
                ALTER TABLE identity.users DROP COLUMN "Balance";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the Balance column on identity.users from wallets before dropping it, then
            // move users/roles/role_user back to public — mirrors Up()'s ordering in reverse.
            migrationBuilder.Sql(
                """
                ALTER TABLE identity.users ADD COLUMN "Balance" numeric(10,2) NOT NULL DEFAULT 0;
                UPDATE identity.users u SET "Balance" = w."Balance" FROM wallets w WHERE w."UserId" = u."Id";
                ALTER TABLE identity.users SET SCHEMA public;
                ALTER TABLE identity.roles SET SCHEMA public;
                ALTER TABLE identity.role_user SET SCHEMA public;
                """);

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TokenHash",
                table: "password_reset_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");
        }
    }
}
