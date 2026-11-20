using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNest.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_Totals_NonNegative",
                table: "Orders",
                sql: "[Subtotal] >= 0 AND [ShippingCost] >= 0 AND [Total] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Books_Price_NonNegative",
                table: "Books",
                sql: "[Price] >= 0 AND ([DiscountPrice] IS NULL OR [DiscountPrice] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Books_Stock_NonNegative",
                table: "Books",
                sql: "[Stock] >= 0 AND [Reserved] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookReviews_Rating_Range",
                table: "BookReviews",
                sql: "[Rating] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_NormalizedEmail_Unique",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_Totals_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItems_Quantity_Positive",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Books_Price_NonNegative",
                table: "Books");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Books_Stock_NonNegative",
                table: "Books");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookReviews_Rating_Range",
                table: "BookReviews");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_NormalizedEmail_Unique",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
