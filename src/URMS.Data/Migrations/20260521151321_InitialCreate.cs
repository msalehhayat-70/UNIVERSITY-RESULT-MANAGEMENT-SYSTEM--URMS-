using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace URMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    ProgramId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgramName = table.Column<string>(type: "text", nullable: false),
                    ProgramCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.ProgramId);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigKey = table.Column<string>(type: "text", nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    BatchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchName = table.Column<string>(type: "text", nullable: false),
                    ProgramId = table.Column<int>(type: "integer", nullable: false),
                    SemesterType = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    BatchNumber = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.BatchId);
                    table.ForeignKey(
                        name: "FK_Batches_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "ProgramId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotifId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    NotifType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotifId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BatchSemesters",
                columns: table => new
                {
                    BatchSemesterId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    SemesterNo = table.Column<int>(type: "integer", nullable: false),
                    IsConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    IsResultGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    ResultGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchSemesters", x => x.BatchSemesterId);
                    table.ForeignKey(
                        name: "FK_BatchSemesters_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationNo = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    FatherName = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    CurrentSemester = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                    table.ForeignKey(
                        name: "FK_Students_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectConfigurations",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchSemesterId = table.Column<int>(type: "integer", nullable: false),
                    SubjectName = table.Column<string>(type: "text", nullable: false),
                    SubjectCode = table.Column<string>(type: "text", nullable: false),
                    CreditHours = table.Column<int>(type: "integer", nullable: false),
                    SubjectType = table.Column<string>(type: "text", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: false),
                    MaxMarks = table.Column<int>(type: "integer", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectConfigurations", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_SubjectConfigurations_BatchSemesters_BatchSemesterId",
                        column: x => x.BatchSemesterId,
                        principalTable: "BatchSemesters",
                        principalColumn: "BatchSemesterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubjectConfigurations_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CGPAResults",
                columns: table => new
                {
                    CGPAId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    UpToSemester = table.Column<int>(type: "integer", nullable: false),
                    CGPA = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CGPAResults", x => x.CGPAId);
                    table.ForeignKey(
                        name: "FK_CGPAResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SGPAResults",
                columns: table => new
                {
                    SGPAId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    SemesterNo = table.Column<int>(type: "integer", nullable: false),
                    SGPA = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    TotalCreditHours = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SGPAResults", x => x.SGPAId);
                    table.ForeignKey(
                        name: "FK_SGPAResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Gradesheets",
                columns: table => new
                {
                    GradesheetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectConfigId = table.Column<int>(type: "integer", nullable: false),
                    TeacherId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    HodRemarks = table.Column<string>(type: "text", nullable: true),
                    ReviewedById = table.Column<int>(type: "integer", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gradesheets", x => x.GradesheetId);
                    table.ForeignKey(
                        name: "FK_Gradesheets_SubjectConfigurations_SubjectConfigId",
                        column: x => x.SubjectConfigId,
                        principalTable: "SubjectConfigurations",
                        principalColumn: "ConfigId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Gradesheets_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Gradesheets_Users_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIAlerts",
                columns: table => new
                {
                    AlertId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradesheetId = table.Column<int>(type: "integer", nullable: false),
                    AlertType = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAlerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_AIAlerts_Gradesheets_GradesheetId",
                        column: x => x.GradesheetId,
                        principalTable: "Gradesheets",
                        principalColumn: "GradesheetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentMarks",
                columns: table => new
                {
                    MarkId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    GradesheetId = table.Column<int>(type: "integer", nullable: false),
                    SubjectConfigId = table.Column<int>(type: "integer", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: false),
                    GradePoints = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMarks", x => x.MarkId);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Gradesheets_GradesheetId",
                        column: x => x.GradesheetId,
                        principalTable: "Gradesheets",
                        principalColumn: "GradesheetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentMarks_SubjectConfigurations_SubjectConfigId",
                        column: x => x.SubjectConfigId,
                        principalTable: "SubjectConfigurations",
                        principalColumn: "ConfigId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Programs",
                columns: new[] { "ProgramId", "ProgramCode", "ProgramName" },
                values: new object[,]
                {
                    { 1, "CS", "BS Computer Science" },
                    { 2, "CE", "BS Computer Engineering" }
                });

            migrationBuilder.InsertData(
                table: "SystemConfigs",
                columns: new[] { "Id", "ConfigKey", "ConfigValue" },
                values: new object[] { 1, "registered_accounts", "0" });

            migrationBuilder.CreateIndex(
                name: "IX_AIAlerts_GradesheetId",
                table: "AIAlerts",
                column: "GradesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ProgramId",
                table: "Batches",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchSemesters_BatchId_SemesterNo",
                table: "BatchSemesters",
                columns: new[] { "BatchId", "SemesterNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CGPAResults_StudentId_UpToSemester",
                table: "CGPAResults",
                columns: new[] { "StudentId", "UpToSemester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradesheets_ReviewedById",
                table: "Gradesheets",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_Gradesheets_SubjectConfigId",
                table: "Gradesheets",
                column: "SubjectConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradesheets_TeacherId",
                table: "Gradesheets",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SGPAResults_StudentId_SemesterNo",
                table: "SGPAResults",
                columns: new[] { "StudentId", "SemesterNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_GradesheetId",
                table: "StudentMarks",
                column: "GradesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_StudentId",
                table: "StudentMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMarks_SubjectConfigId",
                table: "StudentMarks",
                column: "SubjectConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_BatchId",
                table: "Students",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_RegistrationNo",
                table: "Students",
                column: "RegistrationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectConfigurations_BatchSemesterId",
                table: "SubjectConfigurations",
                column: "BatchSemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectConfigurations_TeacherId",
                table: "SubjectConfigurations",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_ConfigKey",
                table: "SystemConfigs",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedById",
                table: "Users",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIAlerts");

            migrationBuilder.DropTable(
                name: "CGPAResults");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "SGPAResults");

            migrationBuilder.DropTable(
                name: "StudentMarks");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "Gradesheets");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "SubjectConfigurations");

            migrationBuilder.DropTable(
                name: "BatchSemesters");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Batches");

            migrationBuilder.DropTable(
                name: "Programs");
        }
    }
}
