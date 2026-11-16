using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colabs.Migrations
{
    /// <inheritdoc />
    public partial class CreateInitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DB_CATEGORY",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_CATEGORY_ID_PK", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DB_INSTITUTION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    CNPJ = table.Column<string>(type: "VARCHAR2(18)", maxLength: 18, nullable: false),
                    ADDRESS = table.Column<string>(type: "VARCHAR2(400)", maxLength: 400, nullable: false),
                    LATITUDE = table.Column<decimal>(type: "DECIMAL(9,6)", precision: 9, scale: 6, nullable: false),
                    LONGITUDE = table.Column<decimal>(type: "DECIMAL(9,6)", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_INSTITUTION_ID_PK", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DB_USER",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    CPF = table.Column<string>(type: "VARCHAR2(15)", maxLength: 15, nullable: false),
                    PHONE = table.Column<string>(type: "VARCHAR2(15)", maxLength: 15, nullable: false),
                    EMAIL = table.Column<string>(type: "VARCHAR2(150)", maxLength: 150, nullable: false),
                    PASSWORD = table.Column<string>(type: "VARCHAR2(300)", maxLength: 300, nullable: false),
                    ROLE = table.Column<string>(type: "VARCHAR2(20)", nullable: false, defaultValue: "USER"),
                    ADDRESS = table.Column<string>(type: "VARCHAR2(400)", maxLength: 400, nullable: false),
                    LATITUDE = table.Column<decimal>(type: "DECIMAL(9,6)", precision: 9, scale: 6, nullable: true),
                    LONGITUDE = table.Column<decimal>(type: "DECIMAL(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_USER_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_USER_ROLE_CK", "\"ROLE\" IN (''USER'', ''ADMIN'')");
                });

            migrationBuilder.CreateTable(
                name: "DB_HELP_REQUEST",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_CATEGORY = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ID_USER = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ID_INSTITUTION = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", nullable: false, defaultValue: "OPEN"),
                    CREATION_DATE = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    COMPLETION_DATE = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_HELP_REQUEST_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_HELP_REQUEST_STATUS_CK", "\"STATUS\" IN (''OPEN'', ''IN_PROGRESS'', ''COMPLETED'', ''CANCELED'')");
                    table.ForeignKey(
                        name: "DB_HELP_REQUEST_ID_CATEGORY_FK",
                        column: x => x.ID_CATEGORY,
                        principalTable: "DB_CATEGORY",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DB_HELP_REQUEST_ID_INSTITUTION_FK",
                        column: x => x.ID_INSTITUTION,
                        principalTable: "DB_INSTITUTION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DB_HELP_REQUEST_ID_USER_FK",
                        column: x => x.ID_USER,
                        principalTable: "DB_USER",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DB_DONATION",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_CATEGORY = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ID_USER = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    ID_INSTITUTION = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    ID_HELP_REQUEST = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: false),
                    STATUS = table.Column<string>(type: "VARCHAR2(20)", nullable: false, defaultValue: "AVAILABLE"),
                    CREATION_DATE = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false),
                    COMPLETION_DATE = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_DONATION_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_DONATION_DONOR_CK", "\"ID_USER\" IS NOT NULL OR \"ID_INSTITUTION\" IS NOT NULL");
                    table.CheckConstraint("DB_DONATION_STATUS_CK", "\"STATUS\" IN (''AVAILABLE'', ''MATCHED'', ''DELIVERED'', ''CANCELED'')");
                    table.ForeignKey(
                        name: "DB_DONATION_ID_CATEGORY_FK",
                        column: x => x.ID_CATEGORY,
                        principalTable: "DB_CATEGORY",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DB_DONATION_ID_HELP_REQUEST_FK",
                        column: x => x.ID_HELP_REQUEST,
                        principalTable: "DB_HELP_REQUEST",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DB_DONATION_ID_INSTITUTION_FK",
                        column: x => x.ID_INSTITUTION,
                        principalTable: "DB_INSTITUTION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DB_DONATION_ID_USER_FK",
                        column: x => x.ID_USER,
                        principalTable: "DB_USER",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DB_HELP_REQUEST_ITEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_HELP_REQUEST = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    QUANTITY = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_HELP_REQUEST_ITEM_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_HELP_REQUEST_ITEM_QUANTITY_CK", "\"QUANTITY\" > 0");
                    table.ForeignKey(
                        name: "DB_HELP_REQUEST_ITEM_ID_HELP_REQUEST_FK",
                        column: x => x.ID_HELP_REQUEST,
                        principalTable: "DB_HELP_REQUEST",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DB_DONATION_ITEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_DONATION = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NAME = table.Column<string>(type: "VARCHAR2(100)", maxLength: 100, nullable: false),
                    QUANTITY = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_DONATION_ITEM_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_DONATION_ITEM_QUANTITY_CK", "\"QUANTITY\" > 0");
                    table.ForeignKey(
                        name: "DB_DONATION_ITEM_ID_DONATION_FK",
                        column: x => x.ID_DONATION,
                        principalTable: "DB_DONATION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DB_REVIEW",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_DONATION = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    RATING = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "VARCHAR2(500)", maxLength: 500, nullable: true),
                    REVIEW_DATE = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("DB_REVIEW_ID_PK", x => x.ID);
                    table.CheckConstraint("DB_REVIEW_RATING_CK", "\"RATING\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "DB_REVIEW_ID_DONATION_FK",
                        column: x => x.ID_DONATION,
                        principalTable: "DB_DONATION",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "DB_CATEGORY_NAME_UN",
                table: "DB_CATEGORY",
                column: "NAME",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DB_DONATION_ID_CATEGORY",
                table: "DB_DONATION",
                column: "ID_CATEGORY");

            migrationBuilder.CreateIndex(
                name: "IX_DB_DONATION_ID_HELP_REQUEST",
                table: "DB_DONATION",
                column: "ID_HELP_REQUEST");

            migrationBuilder.CreateIndex(
                name: "IX_DB_DONATION_ID_INSTITUTION",
                table: "DB_DONATION",
                column: "ID_INSTITUTION");

            migrationBuilder.CreateIndex(
                name: "IX_DB_DONATION_ID_USER",
                table: "DB_DONATION",
                column: "ID_USER");

            migrationBuilder.CreateIndex(
                name: "IX_DB_DONATION_ITEM_ID_DONATION",
                table: "DB_DONATION_ITEM",
                column: "ID_DONATION");

            migrationBuilder.CreateIndex(
                name: "IX_DB_HELP_REQUEST_ID_CATEGORY",
                table: "DB_HELP_REQUEST",
                column: "ID_CATEGORY");

            migrationBuilder.CreateIndex(
                name: "IX_DB_HELP_REQUEST_ID_INSTITUTION",
                table: "DB_HELP_REQUEST",
                column: "ID_INSTITUTION");

            migrationBuilder.CreateIndex(
                name: "IX_DB_HELP_REQUEST_ID_USER",
                table: "DB_HELP_REQUEST",
                column: "ID_USER");

            migrationBuilder.CreateIndex(
                name: "IX_DB_HELP_REQUEST_ITEM_ID_HELP_REQUEST",
                table: "DB_HELP_REQUEST_ITEM",
                column: "ID_HELP_REQUEST");

            migrationBuilder.CreateIndex(
                name: "DB_INSTITUTION_CNPJ_UN",
                table: "DB_INSTITUTION",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DB_REVIEW_ID_DONATION",
                table: "DB_REVIEW",
                column: "ID_DONATION");

            migrationBuilder.CreateIndex(
                name: "DB_USER_CPF_UN",
                table: "DB_USER",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "DB_USER_EMAIL_UN",
                table: "DB_USER",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DB_DONATION_ITEM");

            migrationBuilder.DropTable(
                name: "DB_HELP_REQUEST_ITEM");

            migrationBuilder.DropTable(
                name: "DB_REVIEW");

            migrationBuilder.DropTable(
                name: "DB_DONATION");

            migrationBuilder.DropTable(
                name: "DB_HELP_REQUEST");

            migrationBuilder.DropTable(
                name: "DB_CATEGORY");

            migrationBuilder.DropTable(
                name: "DB_INSTITUTION");

            migrationBuilder.DropTable(
                name: "DB_USER");
        }
    }
}
