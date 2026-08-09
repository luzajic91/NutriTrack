using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriTrack.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddHashedTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows hold plaintext tokens. Hashing is one-way, so they can never match
            // a lookup again, and leaving them would preserve exactly the exposure this
            // migration exists to remove. Everyone signs in again; anyone mid-confirmation
            // asks for a new mail via resend-confirmation.
            migrationBuilder.Sql("DELETE FROM EmailConfirmationTokens;");
            migrationBuilder.Sql("DELETE FROM RefreshTokens;");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token");
        }

        /// <inheritdoc />
        // The deleted rows are not restorable, and would be useless if they were: reverting
        // this migration means reverting to code that compares raw tokens.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");
        }
    }
}
