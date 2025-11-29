using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ilvi.Asana.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastSuccessfulSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DownloadAttachments = table.Column<bool>(type: "bit", nullable: false),
                    GenerateThumbnails = table.Column<bool>(type: "bit", nullable: false),
                    ThumbnailMaxWidth = table.Column<int>(type: "int", nullable: false),
                    AttachmentBasePath = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    UsersCount = table.Column<int>(type: "int", nullable: false),
                    WorkspacesCount = table.Column<int>(type: "int", nullable: false),
                    ProjectsCount = table.Column<int>(type: "int", nullable: false),
                    TasksCount = table.Column<int>(type: "int", nullable: false),
                    StoriesCount = table.Column<int>(type: "int", nullable: false),
                    AttachmentsCount = table.Column<int>(type: "int", nullable: false),
                    DownloadedAttachmentsCount = table.Column<int>(type: "int", nullable: false),
                    ApiCallsCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorStackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", nullable: false),
                    IsOrganization = table.Column<bool>(type: "bit", nullable: false),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    WorkspaceId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    Archived = table.Column<bool>(type: "bit", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsanaCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsanaModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    AssigneeId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(1000)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HtmlNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedById = table.Column<long>(type: "bigint", nullable: true),
                    DueOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsanaCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsanaModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumSubtasks = table.Column<int>(type: "int", nullable: false),
                    ParentTaskId = table.Column<long>(type: "bigint", nullable: true),
                    ResourceSubtype = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tasks_Tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    DownloadUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    ViewUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    PermanentUrl = table.Column<string>(type: "nvarchar(2000)", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(50)", nullable: true),
                    LocalPath = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    IsDownloaded = table.Column<bool>(type: "bit", nullable: false),
                    DownloadError = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    AsanaCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedById = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ResourceSubtype = table.Column<string>(type: "nvarchar(100)", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AsanaCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stories_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Stories_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<long>(type: "bigint", nullable: false),
                    DependsOnTaskId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_DependsOnTaskId",
                        column: x => x.DependsOnTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskDependencies_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_IsDownloaded",
                table: "Attachments",
                column: "IsDownloaded");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_TaskId",
                table: "Attachments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Archived",
                table: "Projects",
                column: "Archived");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerId",
                table: "Projects",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_CreatedById",
                table: "Stories",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_TaskId",
                table: "Stories",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Type",
                table: "Stories",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_StartedAt",
                table: "SyncLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_Status",
                table: "SyncLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_DependsOnTaskId",
                table: "TaskDependencies",
                column: "DependsOnTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_TaskId",
                table: "TaskDependencies",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependencies_TaskId_DependsOnTaskId",
                table: "TaskDependencies",
                columns: new[] { "TaskId", "DependsOnTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssigneeId",
                table: "Tasks",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Completed",
                table: "Tasks",
                column: "Completed");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CompletedById",
                table: "Tasks",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueOn",
                table: "Tasks",
                column: "DueOn");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "SyncConfigurations");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "TaskDependencies");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Workspaces");
        }
    }
}
