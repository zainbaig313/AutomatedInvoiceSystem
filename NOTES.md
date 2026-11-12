# InvoiceFlow — Project Notes & Setup Guide

## What I Need From You

> [!IMPORTANT]
> Please update the following values before running the app.

### 1. SMTP — Sender Email
In `appsettings.json`, set your Gmail address as `Smtp:SenderEmail`:
```json
"Smtp": {
  "SenderEmail": "your-actual-gmail@gmail.com"
}
```
The App Password (`msaaqkwlhdebqwmw`) is already filled in.

### 2. Payment Details (stored on your User profile, not in config)
Payment instructions shown on every invoice PDF and email are pulled directly from your user account. After registering, update your bank account number and JazzCash/Easypaisa number via:

```
PUT /api/users/{id}
```
```json
{
  "name": "Zain Baig",
  "bank": "HBL",
  "bankAccountNumber": "1234-5678-9012",
  "digitalWallet": "0300-1234567"
}
```
Supported banks: `HBL`, `ASKARI`, `ALFALAH`, `UBL`, `MCB`

### 3. App Base URL
When you deploy, set:
```json
"App": {
  "BaseUrl": "https://yourdomain.com"
}
```
During local development `https://localhost:5001` is fine.

---

## Project Structure

```
InvoiceFlow/
├── Controllers/
│   ├── UserController.cs          — Auth (login) + User CRUD
│   ├── ClientController.cs        — CRUD + bulk import
│   ├── InvoiceController.cs       — CRUD + PDF + monthly report
│   ├── PaymentProofController.cs  — List + review proofs
│   └── PublicInvoiceController.cs — No-auth client endpoints
│
├── Services/
│   ├── Interfaces/
│   │   ├── IJwtService.cs
│   │   ├── IUserService.cs        — Auth + user management (merged)
│   │   ├── IClientService.cs
│   │   ├── IInvoiceService.cs
│   │   ├── IEmailService.cs
│   │   └── IPaymentProofService.cs
│   ├── JwtService.cs
│   ├── UserService.cs             — Auth + user management (merged)
│   ├── ClientService.cs
│   ├── InvoiceService.cs
│   ├── EmailService.cs
│   ├── PaymentProofService.cs
│   └── ReminderJob.cs             — Hangfire daily job
│
├── Models/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Client.cs
│   │   ├── Invoice.cs
│   │   ├── PaymentProof.cs
│   │   └── EmailLogs.cs
│   ├── Enums/
│   │   ├── Bank.cs                — HBL, ASKARI, ALFALAH, UBL, MCB
│   │   ├── InvoiceStatus.cs
│   │   ├── EmailStatus.cs
│   │   ├── EmailTypes.cs
│   │   ├── PaymentFileType.cs
│   │   └── ReviewStatus.cs
│   └── DTOs/
│       ├── UserDtos.cs            — Auth DTOs + User DTOs (merged)
│       ├── ClientDtos.cs
│       ├── InvoiceDtos.cs
│       └── PaymentProofDtos.cs
│
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Configurations/
│
├── appsettings.json
└── Program.cs
```

---

## API Endpoints

### Users & Auth  _(merged into one controller at `/api/users`)_

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | `/api/users/login` | No | Login — returns JWT token |
| POST | `/api/users` | No* | Register a new accountant / owner |
| GET | `/api/users/me` | JWT | Get the currently logged-in user's profile |
| PUT | `/api/users/{id}` | JWT | Update name, bank account, digital wallet |

> \* `POST /api/users` is open for first-time setup. Restrict it in production once your account is created.

**Login body:**
```json
{
  "email": "zain@123gmail.com",
  "password": "yourpassword"
}
```

**Login response:**
```json
{
  "name": "Zain Baig",
  "email": "zain@123gmail.com",
  "token": "<JWT>"
}
```

**Create / Register body:**
```json
{
  "name": "Zain Baig",
  "email": "zain@example.com",
  "password": "StrongPassword123",
  "bank": "HBL",
  "bankAccountNumber": "1234-5678-9012",
  "digitalWallet": "0300-1234567"
}
```

**Update User body** (`PUT /api/users/{id}` — does **not** change password):
```json
{
  "name": "Zain Baig",
  "bank": "MCB",
  "bankAccountNumber": "9999-0000-1111",
  "digitalWallet": "0321-9876543"
}
```

---

### Clients

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| GET | `/api/clients` | JWT | List all clients |
| GET | `/api/clients/{id}` | JWT | Get client by ID |
| POST | `/api/clients` | JWT | Add a single client |
| PUT | `/api/clients/{id}` | JWT | Update client |
| DELETE | `/api/clients/{id}` | JWT | Delete client |
| POST | `/api/clients/import` | JWT | Bulk import from CSV or Excel file |

**Add Client body:**
```json
{
  "name": "Ahmed Ali",
  "email": "ahmed@example.com",
  "phone": "0300-1234567",
  "company": "ACME Ltd",
  "city": "Karachi",
  "isReminderEnabled": true
}
```

**Bulk Import** — `POST /api/clients/import`
- Send as `multipart/form-data`, key: `file`
- Accepted formats: `.csv`, `.xlsx`, `.xls`

**CSV format** (header row required):
```
Name,Email,Phone,Company,City,IsReminderEnabled
Ahmed Ali,ahmed@gmail.com,0300-1234567,ACME Ltd,Karachi,true
Sara Khan,sara@gmail.com,0311-9876543,,,true
```

---

### Invoices

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| GET | `/api/invoices` | JWT | List all invoices |
| GET | `/api/invoices/{id}` | JWT | Get invoice by ID |
| POST | `/api/invoices` | JWT | Create invoice (auto-generates PDF + sends email) |
| PATCH | `/api/invoices/{id}/mark-paid` | JWT | Mark invoice as paid |
| DELETE | `/api/invoices/{id}` | JWT | Delete invoice |
| GET | `/api/invoices/{id}/pdf` | JWT | Download invoice PDF |
| GET | `/api/invoices/report?year=2024&month=11` | JWT | Download monthly Excel report |

**Create Invoice body:**
```json
{
  "clientId": 1,
  "description": "Web development services - November 2024",
  "amount": 50000,
  "issueDate": "2024-11-01",
  "dueDate": "2024-11-15",
  "isRecurring": false
}
```

> [!NOTE]
> Payment instructions on the PDF and email are built automatically from the **invoice creator's** `bank`, `bankAccountNumber`, and `digitalWallet` fields — no manual config needed.

---

### Public Endpoints _(Client-facing — no login required)_

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| GET | `/api/public/invoices/{token}` | No | View invoice details (safe fields only) |
| GET | `/api/public/invoices/{token}/pdf` | No | Download invoice PDF |
| POST | `/api/public/invoices/{token}/proof` | No | Upload proof of payment |

The `{token}` is the URL-safe Base64 value embedded in the client's email link (e.g. `https://yourapp.com/pay/Xk3v9...`).

**Upload Proof** — `POST /api/public/invoices/{token}/proof`
- Send as `multipart/form-data`, key: `file`
- Accepted formats: `.jpg`, `.png`, `.pdf`
- Max size: **5 MB**
- On success: invoice status changes to `ProofSubmitted`, reminders stop.

> [!IMPORTANT]
> The API always returns `404` for both invalid **and** expired tokens — attackers learn nothing.

---

### Payment Proofs _(Accountant)_

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| GET | `/api/payment-proofs/invoice/{invoiceId}` | JWT | List all proofs for an invoice |
| PATCH | `/api/payment-proofs/{id}/review` | JWT | Approve or reject a proof |

**Review body:**
```json
{ "approved": true }
```
```json
{ "approved": false, "rejectReason": "Screenshot is blurry, please resubmit." }
```
- Approving → invoice status becomes `Paid`
- Rejecting → invoice status becomes `Rejected`, reminders can resume

---

## How the Payment Token Works

1. When an invoice is created, the backend generates **32 cryptographically random bytes** (`RandomNumberGenerator.GetBytes`).
2. The raw token (URL-safe Base64) is **never stored** in the database.
3. A **SHA-256 hash** of the token is stored in `Invoice.PaymentTokenHash`.
4. The raw token is embedded in the email link: `https://yourapp.com/pay/{token}`.
5. When the client opens the link, the page calls `GET /api/public/invoices/{token}`.
6. The API hashes the incoming token, finds the matching row, checks expiry, and returns only safe fields (no internal IDs).
7. A database breach does **not** expose working links — only useless hashes.
8. Token expiry: **30 days** after invoice creation (`TokenExpiresAt`).

---

## Hangfire Reminder Schedule

The `ReminderJob` runs every day at **8:00 AM UTC** (`0 8 * * *`).

| Condition | Action |
|-----------|--------|
| Due in exactly 3 days | Gentle reminder email + PDF |
| 1–3 days overdue | First reminder email + PDF |
| 4+ days overdue | Firm second reminder email + PDF |
| 1st of the month | Auto-create new invoices for all recurring templates |

> [!NOTE]
> Reminders are skipped if `Client.IsReminderEnabled = false` or if the invoice status is `Paid` or `ProofSubmitted`.

Access the Hangfire dashboard at: **`/hangfire`** (no auth in dev — add an `IDashboardAuthorizationFilter` before going live).

---

## Running Locally

```bash
cd InvoiceFlow
dotnet run
```

> [!NOTE]
> Hangfire creates its own schema tables in SQL Server automatically on first run. No EF migration needed for Hangfire.

> [!IMPORTANT]
> You **do** need to run an EF migration for the new `Bank`, `BankAccountNumber`, and `DigitalWallet` columns added to the `Users` table:
> ```bash
> dotnet ef migrations add AddUserBankFields
> dotnet ef database update
> ```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `BCrypt.Net-Next` | Password hashing |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT auth middleware |
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core + SQL Server |
| `Hangfire.AspNetCore` | Background jobs + dashboard |
| `Hangfire.SqlServer` | SQL Server job storage |
| `MailKit` | SMTP email sending (Gmail) |
| `QuestPDF` | Invoice PDF generation |
| `ClosedXML` | Monthly Excel report export |
| `ExcelDataReader` + `ExcelDataReader.DataSet` | Bulk client import from Excel |
