# InvoiceFlow Backend Architecture & Complete API Flow Documentation

This document provides an end-to-end walkthrough of the **InvoiceFlow** ASP.NET Core backend. It covers every file, domain entity, data transfer object (DTO), service, controller endpoint, lifecycle flow, business rule, and database relationship.

---

## 1. High-Level Architecture Overview

InvoiceFlow is a multi-tenant invoicing, customer payment verification, and automated reminder platform built with **.NET 10**, **Entity Framework Core (SQL Server)**, **QuestPDF**, **MailKit/MimeKit**, and **Hangfire**.

```
                           ┌───────────────────────────────┐
                           │      HTTP Request Client      │
                           │   (Frontend / Email Recipient)│
                           └───────────────┬───────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ ASP.NET Core Middleware Pipeline (Program.cs)                                          │
│   1. CORS ("AllowAll")                                                                 │
│   2. Routing / Controller Endpoint Dispatch                                            │
│   3. Authentication (JwtBearer Token Validation: sub, email, name)                     │
│   4. Authorization Middleware                                                          │
│   5. Hangfire Dashboard (/hangfire)                                                    │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Controllers                                                                            │
│   ├── UserController           -> /api/users                                           │
│   ├── ClientController         -> /api/clients                                         │
│   ├── InvoiceController        -> /api/invoices                                        │
│   ├── PaymentProofController   -> /api/payment-proofs                                  │
│   └── PublicInvoiceController  -> /api/public/invoices                                 │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Service Layer                                                                          │
│   ├── UserService (IUserService)               -> User registration, BCrypt, Auth      │
│   ├── JwtService (IJwtService)                 -> JWT token signing (HMAC-SHA256)      │
│   ├── ClientService (IClientService)           -> Client CRUD, CSV/Excel Parser        │
│   ├── InvoiceService (IInvoiceService)         -> Token generation, Invoices, PDF, XLS │
│   ├── EmailService (IEmailService)             -> MailKit SMTP, HTML email, PDF attach │
│   ├── PaymentProofService (IPaymentProofServ.) -> Disk storage, File types, Reviews    │
│   └── ReminderJob                              -> Hangfire cron job for email alerts   │
└──────────────────────────────────────────┬─────────────────────────────────────────────┘
                                           │
                                           ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Data Access & Storage Layer                                                            │
│   ├── ApplicationDbContext (EF Core)           -> Database context                     │
│   ├── Entity Configurations (Fluent API)       -> Schema, primary keys, relationships  │
│   ├── SQL Server (InvoiceManagement DB)        -> Users, Clients, Invoices, Proofs     │
│   └── Disk Storage (uploads/proofs/)           -> Payment receipts (JPG, PNG, PDF)     │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Domain Models, Enums & Database Entities

### 2.1 Enums (`InvoiceFlow/Models/Enums/`)

| Enum File | Values | Purpose |
| :--- | :--- | :--- |
| **`Bank.cs`** | `HBL`, `ASKARI`, `ALFALAH`, `UBL`, `MCB` | Designated receiving banks for accountant payout instructions. |
| **`InvoiceStatus.cs`** | `Pending`, `ProofSubmitted`, `Paid`, `Rejected` | Invoice lifecycle state. |
| **`ReviewStatus.cs`** | `Pending`, `Approved`, `Rejected` | Accountant decision on submitted proof. |
| **`PaymentFileType.cs`**| `jpg`, `png`, `pdf` | Validated receipt formats uploaded by clients. |
| **`EmailStatus.cs`** | `Pending`, `Sent`, `Failed` | Delivery status for audit records. |
| **`EmailTypes.cs`** | `NewInvoice`, `Reminder`, `ProofRejected` | Category of dispatched email. |

---

### 2.2 Entities (`InvoiceFlow/Models/Entities/`)

#### 1. `User.cs`
* **Table**: `Users`
* **Fields**:
  * `int Id` (Primary Key, Identity)
  * `string Name` (Accountant name)
  * `string Email` (Accountant login email, unique)
  * `string PasswordHash` (BCrypt password hash)
  * `Bank Bank` (Designated payout bank)
  * `string? BankAccountNumber` (IBAN / Account Number)
  * `string? DigitalWallet` (EasyPaisa / JazzCash / Mobile number)
* **Relationships**:
  * Owns multiple `Clients` (`CreatedById`).
  * Owns multiple `Invoices` (`CreatedById`).

#### 2. `Client.cs`
* **Table**: `Clients`
* **Fields**:
  * `int Id` (Primary Key)
  * `string Name` (Customer name)
  * `string Email` (Customer email)
  * `string Phone` (Customer phone)
  * `string? Company` (Company / business name)
  * `string? City` (City name)
  * `bool IsReminderEnabled` (Toggles automated reminder emails)
  * `int CreatedById` (Foreign Key referencing `Users.Id`)
* **Relationships**:
  * Belongs to `User` (`CreatedBy`).
  * Has many `Invoices`.

#### 3. `Invoice.cs`
* **Table**: `Invoices`
* **Fields**:
  * `int Id` (Primary Key)
  * `string InvoiceNumber` (Unique formatted identifier: `INV-YYYYMMDD-XXXX`)
  * `int ClientId` (Foreign Key referencing `Clients.Id`)
  * `int CreatedById` (Foreign Key referencing `Users.Id`)
  * `string Description` (Services / line items)
  * `decimal Amount` (Invoice amount)
  * `DateOnly IssueDate` (Date issued)
  * `DateOnly DueDate` (Payment deadline)
  * `InvoiceStatus Status` (`Pending`, `ProofSubmitted`, `Paid`, `Rejected`)
  * `bool IsRecurring` (Toggles monthly auto-regeneration)
  * `string PaymentTokenHash` (SHA-256 hash of the 32-byte public access token)
  * `DateTime TokenExpiresAt` (Token validity cutoff, typically 30 days)
* **Relationships**:
  * Belongs to `Client` (`Client`).
  * Belongs to `User` (`CreatedBy`).
  * Has many `PaymentProofs`.
  * Has many `EmailLogs`.

#### 4. `PaymentProof.cs`
* **Table**: `PaymentProofs`
* **Fields**:
  * `int Id` (Primary Key)
  * `int InvoiceId` (Foreign Key referencing `Invoices.Id`)
  * `string StoredFileName` (UUID filename on disk: `{guid}.{ext}`)
  * `PaymentFileType FileType` (`jpg`, `png`, `pdf`)
  * `long FileSize` (Bytes, max 5MB)
  * `DateTime UploadedAt` (Timestamp)
  * `ReviewStatus ReviewStatus` (`Pending`, `Approved`, `Rejected`)
  * `string? RejectReason` (Mandatory if rejected)
  * `DateTime? ReviewedAt` (Timestamp of review)
* **Relationships**:
  * Belongs to `Invoice`.

#### 5. `EmailLog.cs`
* **Table**: `EmailLogs`
* **Fields**:
  * `int Id` (Primary Key)
  * `int InvoiceId` (Foreign Key referencing `Invoices.Id`)
  * `string RecipientEmail` (Customer email address)
  * `EmailTypes EmailType` (`NewInvoice`, `Reminder`, `ProofRejected`)
  * `EmailStatus Status` (`Sent`, `Failed`)
  * `int Attempts` (Number of send attempts)
  * `string? ErrorMessage` (Null or empty on success; error text on failure)
  * `DateTime OccurredAt` (Timestamp)

---

## 3. Data Transfer Objects (DTOs)

Located in `InvoiceFlow/Models/DTOs/`:

### User DTOs (`UserDtos.cs`)
* `LoginRequestDto(string Email, string Password)`
* `LoginResponse(string Name, string Email, string Token)`
* `CreateUserDto(string Name, string Email, string Password, Bank Bank, string? BankAccountNumber, string? DigitalWallet)`
* `UpdateUserDto(string Name, Bank Bank, string? BankAccountNumber, string? DigitalWallet)`
* `UserResponseDto(int Id, string Name, string Email, string Bank, string? BankAccountNumber, string? DigitalWallet)`

### Client DTOs (`ClientDtos.cs`)
* `CreateClientDto(string Name, string Email, string Phone, string? Company, string? City, bool IsReminderEnabled = true)`
* `UpdateClientDto(string Name, string Email, string Phone, string? Company, string? City, bool IsReminderEnabled)`
* `ClientResponseDto(int Id, string Name, string Email, string Phone, string? Company, string? City, bool IsReminderEnabled)`

### Invoice DTOs (`InvoiceDtos.cs`)
* `CreateInvoiceDto(int ClientId, string Description, decimal Amount, DateOnly IssueDate, DateOnly DueDate, bool IsRecurring)`
* `UpdateInvoiceStatusDto(InvoiceStatus Status)`
* `InvoiceResponseDto(int Id, string InvoiceNumber, int ClientId, string ClientName, string ClientEmail, string ClientPhone, string? ClientCompany, string? ClientCity, string Description, decimal Amount, DateOnly IssueDate, DateOnly DueDate, string Status, bool IsRecurring, DateTime TokenExpiresAt)`
* `PublicInvoiceDto(string InvoiceNumber, string ClientName, string Description, decimal Amount, DateOnly DueDate, string Status, string PaymentInstructions)`

### Payment Proof DTOs (`PaymentProofDtos.cs`)
* `PaymentProofResponseDto(int Id, int InvoiceId, string StoredFileName, string FileType, long FileSize, DateTime UploadedAt, string ReviewStatus, string? RejectReason, DateTime? ReviewedAt)`
* `ReviewProofDto(bool Approved, string? RejectReason)`

---

## 4. End-to-End API Workflows & Request Processing

---

### Workflow 1: Authentication & User Registration

```
[Client POST /api/users/login]
        │
        ▼
UserController.Login(LoginRequestDto)
        │
        ├── 1. Validates Email & Password are non-empty
        ├── 2. Calls IUserService.LoginAsync(dto)
        │       ├── Queries User by Email in Users table
        │       ├── Verifies password hash using BCrypt.Verify
        │       └── Calls IJwtService.GenerateToken(user)
        │               └── Signs claims (Sub=UserId, Email, Name) using HMAC-SHA256
        ▼
Returns HTTP 200 OK: { name, email, token }
```

```
[Client POST /api/users]
        │
        ▼
UserController.Create(CreateUserDto)
        │
        ├── 1. Validates Name, Email, Password presence
        ├── 2. Calls IUserService.CreateAsync(dto)
        │       ├── Hashes password using BCrypt.HashPassword
        │       ├── Creates User entity with Bank, Account, Wallet
        │       ├── Saves to Users table (ApplicationDbContext)
        │       └── Maps to UserResponseDto
        ▼
Returns HTTP 201 CreatedAtAction (pointing to /api/users/me)
```

```
[Client GET /api/users/me] (Requires Bearer Token)
        │
        ▼
UserController.GetMe()
        │
        ├── 1. Reads 'sub' claim from ClaimsPrincipal
        ├── 2. Calls IUserService.GetByIdAsync(userId)
        ▼
Returns HTTP 200 OK (UserResponseDto)
```

```
[Client PUT /api/users/me or /api/users/{id}] (Requires Bearer Token)
        │
        ▼
UserController.Update(id?, UpdateUserDto)
        │
        ├── 1. Parses current userId from token
        ├── 2. Resolves targetId (uses id parameter or falls back to authenticated user)
        ├── 3. Calls IUserService.UpdateAsync(targetId, dto)
        │       ├── Updates Name, Bank, BankAccountNumber, DigitalWallet
        │       └── Saves changes to DB
        ▼
Returns HTTP 200 OK (Updated UserResponseDto)
```

---

### Workflow 2: Client Management & Bulk Import

```
[Client POST /api/clients] (Requires Bearer Token)
        │
        ▼
ClientController.Create(CreateClientDto)
        │
        ├── 1. Extracts logged-in userId (AuthorizedControllerBase.GetCurrentUserId())
        ├── 2. Validates Name, Email, Phone
        ├── 3. Calls IClientService.CreateAsync(userId, dto)
        │       ├── Builds Client entity with CreatedById = userId
        │       ├── Saves to Clients table
        │       └── Maps to ClientResponseDto
        ▼
Returns HTTP 201 CreatedAtAction (/api/clients/{id})
```

```
[Client POST /api/clients/import] (Requires Bearer Token, multipart/form-data)
        │
        ▼
ClientController.BulkImport(IFormFile file)
        │
        ├── 1. Extracts logged-in userId
        ├── 2. Validates file presence and extension (.csv, .xlsx, .xls)
        ├── 3. Calls IClientService.BulkImportAsync(userId, stream, fileName)
        │       ├── If CSV:
        │       │    Reads lines, skips header, parses Name, Email, Phone, Company, City, IsReminderEnabled
        │       ├── If Excel:
        │       │    Uses ClosedXML (XLWorkbook) to iterate rows starting at row 2
        │       ├── Checks existing emails for this specific userId
        │       ├── Filters out duplicates (leaves other users' clients unaffected)
        │       ├── Inserts new records into Clients table
        │       └── Maps to List<ClientResponseDto>
        ▼
Returns HTTP 200 OK: { message: "N client(s) imported successfully.", clients: [...] }
```

---

### Workflow 3: Invoice Creation, PDF Generation & Email Dispatch

This is the central process of the application:

```
[Client POST /api/invoices] (Requires Bearer Token)
        │
        ▼
InvoiceController.Create(CreateInvoiceDto)
        │
        ├── 1. Extracts logged-in userId
        ├── 2. Calls IInvoiceService.CreateAsync(userId, dto)
        │       │
        │       ├── A. Generates Unique Invoice Number:
        │       │      Format: "INV-YYYYMMDD-XXXX" (random uppercase hex suffix)
        │       │
        │       ├── B. Generates Cryptographically Secure Token:
        │       │      32 random bytes via RandomNumberGenerator
        │       │      Converted to URL-safe base64 string (rawToken)
        │       │      Computes SHA-256 hash: HashToken(rawToken) -> PaymentTokenHash
        │       │
        │       ├── C. Creates Invoice Entity:
        │       │      ClientId = dto.ClientId, CreatedById = userId
        │       │      Status = InvoiceStatus.Pending
        │       │      TokenExpiresAt = DateTime.UtcNow.AddDays(30)
        │       │      Saves Invoice to Invoices table
        │       │
        │       ├── D. Dispatches Invoice Email (IEmailService.SendInvoiceEmailAsync):
        │       │      ├── Resolves scoped InvoiceService to avoid circular DI
        │       │      ├── Calls InvoiceService.GeneratePdfInternalAsync(invoiceId)
        │       │      │     └── Uses QuestPDF to compile official invoice PDF document
        │       │      ├── Builds Customer Public URL: {BaseUrl}/pay/{rawToken}
        │       │      ├── Builds HTML Email with bank details, line items, and pay button
        │       │      ├── Connects to SMTP (MailKit, StartTls, Gmail credentials)
        │       │      ├── Attaches compiled PDF: "{InvoiceNumber}.pdf"
        │       │      ├── Sends email message
        │       │      └── Inserts record into EmailLogs table:
        │       │            Status = Sent, ErrorMessage = ""
        │       │            (Catches any SMTP network exceptions gracefully so creation succeeds)
        │       │
        │       └── E. Loads navigation property (Client) and returns InvoiceResponseDto
        ▼
Returns HTTP 201 CreatedAtAction (/api/invoices/{id})
```

---

### Workflow 4: Public Customer Invoice View & PDF Download

When the customer receives their billing email, they click the embedded link:

```
[Customer opens /pay/{token}]
        │
        ▼
Frontend calls: GET /api/public/invoices/{token}
        │
        ▼
PublicInvoiceController.GetByToken(token)
        │
        ├── 1. Computes SHA-256 hash of provided token: HashToken(rawToken)
        ├── 2. Queries Invoices table matching:
        │       PaymentTokenHash == hash AND TokenExpiresAt > DateTime.UtcNow
        │       (Includes Client and CreatedBy/User navigation properties)
        ├── 3. If missing or expired -> Returns HTTP 404 Not Found
        ├── 4. Builds payment instructions from the accountant's profile:
        │       - Bank name
        │       - Account / IBAN number
        │       - Digital wallet (EasyPaisa / JazzCash)
        └── 5. Maps to PublicInvoiceDto (excludes internal database IDs)
        ▼
Returns HTTP 200 OK:
{
  invoiceNumber: "INV-20261113-EBCC",
  clientName: "Acme Corp",
  description: "Web development services",
  amount: 50000,
  dueDate: "2026-11-27",
  status: "Pending",
  paymentInstructions: "Bank: HBL\nAccount: PK36HABB...\nWallet: 0300-1234567"
}
```

```
[Customer clicks Download PDF]
        │
        ▼
PublicInvoiceController.DownloadPdf(token)
        │
        ├── 1. Verifies token validity and expiration
        ├── 2. Generates PDF via QuestPDF
        ▼
Returns HTTP 200 FileStreamResult: application/pdf ("INV-20261113-EBCC.pdf")
```

---

### Workflow 5: Proof of Payment Upload & Verification

```
[Customer uploads payment receipt file on /pay/{token}]
        │
        ▼
PublicInvoiceController.UploadProof(token, IFormFile file)
        │
        ├── 1. Calls IPaymentProofService.UploadAsync(token, file)
        │       ├── A. Validates MIME type:
        │       │      "image/jpeg" -> PaymentFileType.jpg
        │       │      "image/png"  -> PaymentFileType.png
        │       │      "application/pdf" -> PaymentFileType.pdf
        │       │      Throws error if invalid extension
        │       ├── B. Validates file size: Max 5 MB (5 * 1024 * 1024 bytes)
        │       ├── C. Validates payment token hash and expiration
        │       ├── D. Generates safe unique filename: $"{Guid.NewGuid():N}{extension}"
        │       ├── E. Saves physical file to disk directory: "uploads/proofs/"
        │       ├── F. Inserts PaymentProof record:
        │       │      ReviewStatus = ReviewStatus.Pending
        │       │      UploadedAt = DateTime.UtcNow
        │       ├── G. Updates Invoice Status:
        │       │      Invoice.Status = InvoiceStatus.ProofSubmitted
        │       │      (Crucial: This automatically halts automated reminder emails)
        │       └── H. Saves DB context changes
        ▼
Returns HTTP 200 OK:
{
  message: "Proof of payment received. The accountant will review it shortly.",
  proof: PaymentProofResponseDto
}
```

```
[Accountant reviews proof in Invoices management dashboard]
        │
        ▼
1. Fetch Proofs: GET /api/payment-proofs/invoice/{invoiceId}
   - Verifies invoice belongs to logged-in user.
   - Returns list of PaymentProofResponseDto.

2. Inspect File: GET /api/payment-proofs/{proofId}/file
   - Verifies invoice ownership.
   - Reads bytes from uploads/proofs/ and streams file with proper Content-Type.

3. Submit Decision: PATCH /api/payment-proofs/{proofId}/review
   Body: { approved: true } OR { approved: false, rejectReason: "Blurry screenshot" }
        │
        ▼
PaymentProofController.Review(id, ReviewProofDto)
        │
        ├── Validates rejectReason is provided if approved == false
        ├── Updates PaymentProof:
        │    ReviewStatus = Approved or Rejected
        │    RejectReason = null or rejectReason
        │    ReviewedAt = DateTime.UtcNow
        ├── Updates Invoice:
        │    Invoice.Status = Approved ? InvoiceStatus.Paid : InvoiceStatus.Rejected
        └── Saves changes to DB
        ▼
Returns HTTP 200 OK (PaymentProofResponseDto)
```

---

### Workflow 6: Automated Background Reminders & Recurring Invoices (Hangfire)

Configured in `Program.cs` and executed by `ReminderJob.cs` daily at 8:00 AM UTC (`0 8 * * *`):

```
                       ┌────────────────────────────┐
                       │ Hangfire Recurring Trigger │
                       │    (Daily at 8:00 AM UTC)  │
                       └──────────────┬─────────────┘
                                      │
                                      ▼
                             ReminderJob.RunAsync()
                                      │
        ┌─────────────────────────────┴─────────────────────────────┐
        │                                                           │
        ▼                                                           ▼
[1. Check Pending Invoices]                             [2. Auto-Create Recurring]
  Filters Invoices where:                                 Runs ONLY if today is the 1st
  Status == Pending AND                                   of the month (today.Day == 1):
  Client.IsReminderEnabled == true                                  │
        │                                                           ▼
  Calculates days relative to DueDate:                    Queries recurring invoices from
                                                          last month (IsRecurring == true)
  Case A: DueDate is in exactly 3 days                              │
    -> Sends gentle reminder email                                  ▼
                                                          Creates new invoice for today:
  Case B: Overdue by 1 to 3 days                           - IssueDate = today
    -> Sends first overdue reminder                        - DueDate = today + 30 days
                                                           - Dispatches new invoice email
  Case C: Overdue by 4+ days                               - Attaches fresh PDF
    -> Sends firm final overdue notice
```

---

### Workflow 7: Accounting Excel Export

```
[Accountant requests Monthly Report: GET /api/invoices/report?year=2026&month=11]
        │
        ▼
InvoiceController.MonthlyReport(year, month)
        │
        ├── 1. Validates year >= 2000 and month between 1 and 12
        ├── 2. Extracts authenticated userId
        ├── 3. Calls IInvoiceService.ExportMonthlyReportAsync(userId, year, month)
        │       ├── Calculates date range: 1st of month to last day of month
        │       ├── Queries invoices matching userId and IssueDate in range
        │       ├── Builds Excel spreadsheet using ClosedXML:
        │       │     - Adds worksheet: "{year}-{month:D2} Report"
        │       │     - Formats header row: Dark slate gray background, white bold text
        │       │     - Populates columns:
        │       │         1. Invoice #
        │       │         2. Client Name
        │       │         3. Client Email
        │       │         4. Client Phone
        │       │         5. Description
        │       │         6. Amount (PKR)
        │       │         7. Issue Date
        │       │         8. Due Date
        │       │         9. Status (flags as "Overdue" if past due and still Pending)
        │       └── Writes workbook to byte array
        ▼
Returns HTTP 200 File:
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Filename: "InvoiceReport-2026-11.xlsx"
```

---

## 5. Security & Multi-Tenancy Architecture

| Security Domain | Mechanism | Implementation Details |
| :--- | :--- | :--- |
| **Password Storage** | BCrypt | `BCrypt.Net.BCrypt.HashPassword` / `Verify`. Salt generated per user; cleartext passwords are never stored. |
| **Authentication** | JWT Bearer Tokens | Validates issuer, audience, and symmetric security key. Tokens carry user `sub`, `email`, and `name`. |
| **Data Isolation** | Multi-Tenant Scoping | Every `Client` and `Invoice` has a foreign key `CreatedById`. Controllers inherit from `AuthorizedControllerBase` and scope every SELECT, UPDATE, and DELETE query to `CreatedById == currentUserId`. |
| **Public Portal Auth**| Opaque SHA-256 Tokens | Public customer links do not expose database integer IDs. They use 32-byte cryptographically secure random tokens. Only the SHA-256 hash is persisted in the database. |
| **File Upload Safety** | MIME Verification & GUIDs | Uploads strictly validate MIME types (`image/jpeg`, `image/png`, `application/pdf`) and enforce a 5 MB ceiling. Files are renamed to random GUIDs on disk to prevent path traversal or overwrites. |

---

## 6. Directory File Map

```
InvoiceFlow/
├── Controllers/
│   ├── AuthorizedControllerBase.cs  # Shared helper extracting current user ID from JWT claims
│   ├── UserController.cs            # Auth (login, register, me, update profile)
│   ├── ClientController.cs          # Client CRUD and CSV/Excel bulk import
│   ├── InvoiceController.cs         # Invoice CRUD, mark-paid, PDF download, Excel reports
│   ├── PaymentProofController.cs    # Proof review and file download
│   └── PublicInvoiceController.cs   # Public token-authenticated invoice view & proof upload
├── Services/
│   ├── Interfaces/                  # Service contracts (IUserService, IInvoiceService, etc.)
│   ├── UserService.cs               # Authentication, user creation and profile updates
│   ├── JwtService.cs                # JWT signing and claim construction
│   ├── ClientService.cs             # Client management, CSV reader, ClosedXML parser
│   ├── InvoiceService.cs            # Invoice numbers, SHA-256 token hashing, QuestPDF engine
│   ├── EmailService.cs              # MailKit SMTP client, email templates, PDF attachment
│   ├── PaymentProofService.cs       # File storage, validation, review transitions
│   └── ReminderJob.cs               # Hangfire cron task for daily reminders & recurring invoices
├── Models/
│   ├── Entities/                    # User, Client, Invoice, PaymentProof, EmailLog
│   ├── DTOs/                        # Request / response records
│   └── Enums/                       # Bank, InvoiceStatus, ReviewStatus, PaymentFileType, etc.
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core DbContext with DbSets
│   └── Configurations/              # Fluent API entity configurations
├── Migrations/                      # EF Core schema migration history
├── uploads/proofs/                  # Storage directory for uploaded customer receipts
├── appsettings.json                 # Connection strings, JWT secrets, SMTP credentials
└── Program.cs                       # DI registration, CORS, JWT config, Hangfire pipeline
```
