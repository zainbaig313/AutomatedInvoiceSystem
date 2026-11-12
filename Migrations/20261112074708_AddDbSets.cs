using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvoiceFlow.Migrations
{
    /// <inheritdoc />
    public partial class AddDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailLog_Invoice_InvoiceId",
                table: "EmailLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Client_ClientId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_User_CreatedById",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentProof_Invoice_InvoiceId",
                table: "PaymentProof");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentProof",
                table: "PaymentProof");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoice",
                table: "Invoice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailLog",
                table: "EmailLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Client",
                table: "Client");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "PaymentProof",
                newName: "PaymentProofs");

            migrationBuilder.RenameTable(
                name: "Invoice",
                newName: "Invoices");

            migrationBuilder.RenameTable(
                name: "EmailLog",
                newName: "EmailLogs");

            migrationBuilder.RenameTable(
                name: "Client",
                newName: "Clients");

            migrationBuilder.RenameIndex(
                name: "IX_User_Email",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentProof_InvoiceId",
                table: "PaymentProofs",
                newName: "IX_PaymentProofs_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoice_InvoiceNumber",
                table: "Invoices",
                newName: "IX_Invoices_InvoiceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Invoice_CreatedById",
                table: "Invoices",
                newName: "IX_Invoices_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Invoice_ClientId",
                table: "Invoices",
                newName: "IX_Invoices_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailLog_InvoiceId",
                table: "EmailLogs",
                newName: "IX_EmailLogs_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Client_Email",
                table: "Clients",
                newName: "IX_Clients_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentProofs",
                table: "PaymentProofs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailLogs",
                table: "EmailLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Clients",
                table: "Clients",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_Invoices_InvoiceId",
                table: "EmailLogs",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedById",
                table: "Invoices",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentProofs_Invoices_InvoiceId",
                table: "PaymentProofs",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_Invoices_InvoiceId",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Clients_ClientId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedById",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentProofs_Invoices_InvoiceId",
                table: "PaymentProofs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentProofs",
                table: "PaymentProofs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmailLogs",
                table: "EmailLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Clients",
                table: "Clients");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "PaymentProofs",
                newName: "PaymentProof");

            migrationBuilder.RenameTable(
                name: "Invoices",
                newName: "Invoice");

            migrationBuilder.RenameTable(
                name: "EmailLogs",
                newName: "EmailLog");

            migrationBuilder.RenameTable(
                name: "Clients",
                newName: "Client");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "User",
                newName: "IX_User_Email");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentProofs_InvoiceId",
                table: "PaymentProof",
                newName: "IX_PaymentProof_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoice",
                newName: "IX_Invoice_InvoiceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_CreatedById",
                table: "Invoice",
                newName: "IX_Invoice_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_ClientId",
                table: "Invoice",
                newName: "IX_Invoice_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_EmailLogs_InvoiceId",
                table: "EmailLog",
                newName: "IX_EmailLog_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_Email",
                table: "Client",
                newName: "IX_Client_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentProof",
                table: "PaymentProof",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoice",
                table: "Invoice",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmailLog",
                table: "EmailLog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Client",
                table: "Client",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLog_Invoice_InvoiceId",
                table: "EmailLog",
                column: "InvoiceId",
                principalTable: "Invoice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Client_ClientId",
                table: "Invoice",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_User_CreatedById",
                table: "Invoice",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentProof_Invoice_InvoiceId",
                table: "PaymentProof",
                column: "InvoiceId",
                principalTable: "Invoice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
