using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingSystem.Migrations.Identity
{
    public partial class AddFieldsToUserManual : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ми залишаємо ТІЛЬКИ FullName, бо Year вже є в базі
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            /* Рядок з Year видаляємо, бо він викликає помилку 
               column "Year" already exists
            */
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");
        }
    }
}