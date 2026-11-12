# InvoiceFlow — API Documentation & Frontend Integration Guide

## 1. Quick Setup & Configuration

> [!IMPORTANT]
> Configure the following in `appsettings.json` before starting the application:
> - `Smtp:SenderEmail`: Your Gmail address (App Password is preconfigured).
> - `App:BaseUrl`: Frontend base URL (e.g., `http://localhost:3000` or `https://yourdomain.com`) so emailed payment links direct to the frontend payment page (`/pay/{token}`).

---

## 2. Authentication Architecture (For Frontend Developers)

- **Authentication Method:** JWT Bearer Token.
- **Header Format:**
  ```http
  Authorization: Bearer <token>
  ```
- **Token Storage:** Store the returned JWT token securely (e.g., `localStorage`, `sessionStorage`, or in-memory state / HttpOnly cookies).
- **Per-User Isolation:**
  - All protected endpoints automatically extract the logged-in user's identity from the JWT claims.
  - Clients, Invoices, Reports, and Proof Reviews are strictly filtered so users only access their own data.
  - Public payment endpoints (`/api/public/invoices/...`) require **no token/login**; client access is validated via a cryptographically secure token embedded in the URL.

---

## 3. Enums Reference

Use these string representations in JSON requests and responses:

### `Bank`
```typescript
type Bank = "HBL" | "ASKARI" | "ALFALAH" | "UBL" | "MCB";
```

### `InvoiceStatus`
```typescript
type InvoiceStatus = "Pending" | "ProofSubmitted" | "Paid" | "Rejected";
```

### `ReviewStatus`
```typescript
type ReviewStatus = "Pending" | "Approved" | "Rejected";
```

### `PaymentFileType`
```typescript
type PaymentFileType = "jpg" | "png" | "pdf";
```

---

## 4. Endpoints Specification

### 👤 Users & Authentication (`/api/users`)

#### 1. Login
- **URL:** `POST /api/users/login`
- **Auth Required:** No
- **Request Body:**
  ```json
  {
    "email": "zain@123gmail.com",
    "password": "yourpassword"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "name": "Zain Baig",
    "email": "zain@123gmail.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
  ```
- **Error Responses:**
  - `400 Bad Request`: Email and password are required.
  - `401 Unauthorized`: Invalid email or password.

---

#### 2. Register Account
- **URL:** `POST /api/users`
- **Auth Required:** No (Open for registration / onboarding)
- **Request Body:**
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
- **Response (201 Created):**
  ```json
  {
    "id": 1,
    "name": "Zain Baig",
    "email": "zain@example.com",
    "bank": "HBL",
    "bankAccountNumber": "1234-5678-9012",
    "digitalWallet": "0300-1234567"
  }
  ```

---

#### 3. Get Current User Profile
- **URL:** `GET /api/users/me`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):**
  ```json
  {
    "id": 1,
    "name": "Zain Baig",
    "email": "zain@example.com",
    "bank": "HBL",
    "bankAccountNumber": "1234-5678-9012",
    "digitalWallet": "0300-1234567"
  }
  ```
- **Error Responses:**
  - `401 Unauthorized`
  - `404 Not Found`

---

#### 4. Update Profile & Payment Details
- **URL:** `PUT /api/users/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Note:** Updates user details such as bank name, account number, and digital wallet (JazzCash/Easypaisa). Invoices and PDFs automatically pull these instructions.
- **Request Body:**
  ```json
  {
    "name": "Zain Baig",
    "bank": "MCB",
    "bankAccountNumber": "9999-0000-1111",
    "digitalWallet": "0321-9876543"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "id": 1,
    "name": "Zain Baig",
    "email": "zain@example.com",
    "bank": "MCB",
    "bankAccountNumber": "9999-0000-1111",
    "digitalWallet": "0321-9876543"
  }
  ```

---

### 👥 Clients Management (`/api/clients`)

> All client operations are scoped to the authenticated user.

#### 1. List Clients
- **URL:** `GET /api/clients`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):**
  ```json
  [
    {
      "id": 1,
      "name": "Ahmed Ali",
      "email": "ahmed@example.com",
      "phone": "0300-1234567",
      "company": "ACME Ltd",
      "city": "Karachi",
      "isReminderEnabled": true
    }
  ]
  ```

---

#### 2. Get Single Client
- **URL:** `GET /api/clients/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):** Client object.
- **Response (404 Not Found):** If ID doesn't exist or belongs to another user.

---

#### 3. Create Client
- **URL:** `POST /api/clients`
- **Auth Required:** Yes (`Bearer <token>`)
- **Request Body:**
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
- **Response (201 Created):** Created client object with assigned `id`.

---

#### 4. Update Client
- **URL:** `PUT /api/clients/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Request Body:**
  ```json
  {
    "name": "Ahmed Ali",
    "email": "ahmed_updated@example.com",
    "phone": "0300-1234567",
    "company": "ACME Global",
    "city": "Lahore",
    "isReminderEnabled": false
  }
  ```
- **Response (200 OK):** Updated client object.

---

#### 5. Delete Client
- **URL:** `DELETE /api/clients/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (204 No Content):** Deleted successfully.
- **Response (404 Not Found):** Client not found or not owned by user.

---

#### 6. Bulk Import Clients
- **URL:** `POST /api/clients/import`
- **Auth Required:** Yes (`Bearer <token>`)
- **Content-Type:** `multipart/form-data`
- **Form Data Field:** `file` (Supports `.csv`, `.xlsx`, `.xls`)
- **CSV Headers:** `Name,Email,Phone,Company,City,IsReminderEnabled`
- **Response (200 OK):**
  ```json
  {
    "message": "2 client(s) imported successfully.",
    "clients": [
      {
        "id": 2,
        "name": "Sara Khan",
        "email": "sara@example.com",
        "phone": "0311-9876543",
        "company": null,
        "city": null,
        "isReminderEnabled": true
      }
    ]
  }
  ```

---

### 📄 Invoices Management (`/api/invoices`)

> All invoice queries and mutations are isolated to the authenticated user.

#### 1. List Invoices
- **URL:** `GET /api/invoices`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):**
  ```json
  [
    {
      "id": 1,
      "invoiceNumber": "INV-20261112-A3B9",
      "clientId": 1,
      "clientName": "Ahmed Ali",
      "clientEmail": "ahmed@example.com",
      "clientPhone": "0300-1234567",
      "clientCompany": "ACME Ltd",
      "clientCity": "Karachi",
      "description": "Web Development Services - Nov 2026",
      "amount": 75000.00,
      "issueDate": "2026-11-12",
      "dueDate": "2026-11-26",
      "status": "Pending",
      "isRecurring": false,
      "tokenExpiresAt": "2026-12-12T12:00:00Z"
    }
  ]
  ```

---

#### 2. Get Single Invoice
- **URL:** `GET /api/invoices/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):** Invoice object with client details.

---

#### 3. Create Invoice
- **URL:** `POST /api/invoices`
- **Auth Required:** Yes (`Bearer <token>`)
- **Behavior:**
  - Saves the invoice in database.
  - Automatically generates PDF with your bank/digital wallet instructions.
  - Sends initial email with PDF attachment and direct payment link to the client.
- **Request Body:**
  ```json
  {
    "clientId": 1,
    "description": "Monthly retainer - Software maintenance",
    "amount": 50000.00,
    "issueDate": "2026-11-01",
    "dueDate": "2026-11-15",
    "isRecurring": true
  }
  ```
- **Response (201 Created):** Created invoice object.

---

#### 4. Mark Invoice as Paid (Accountant Action)
- **URL:** `PATCH /api/invoices/{id}/mark-paid`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):**
  ```json
  {
    "message": "Invoice marked as paid."
  }
  ```

---

#### 5. Delete Invoice
- **URL:** `DELETE /api/invoices/{id}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (204 No Content):** Deleted successfully.

---

#### 6. Download Invoice PDF (Admin / Owner)
- **URL:** `GET /api/invoices/{id}/pdf`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):** Binary file stream (`application/pdf`).

---

#### 7. Export Monthly Excel Report
- **URL:** `GET /api/invoices/report?year=2026&month=11`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):** Binary file stream (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`).
- **File Name:** `InvoiceReport-YYYY-MM.xlsx`

---

### 💳 Public Client Payment Portal (`/api/public/invoices`)

> **Client Flow (Zero Login):**
> 1. Client receives reminder/invoice email with link: `https://frontend-domain.com/pay/{token}`.
> 2. Frontend route `/pay/:token` calls these public endpoints.
> 3. Security: Token is SHA-256 hashed on lookup; missing or expired tokens return `404` to avoid information leakage.

#### 1. Get Public Invoice Data
- **URL:** `GET /api/public/invoices/{token}`
- **Auth Required:** No
- **Response (200 OK):**
  ```json
  {
    "invoiceNumber": "INV-20261112-A3B9",
    "clientName": "Ahmed Ali",
    "description": "Monthly retainer - Software maintenance",
    "amount": 50000.00,
    "dueDate": "2026-11-15",
    "status": "Pending",
    "paymentInstructions": "HBL Bank Account: 1234-5678-9012  |  JazzCash/Easypaisa: 0300-1234567"
  }
  ```
- **Response (404 Not Found):** Token is invalid or expired. Show user-friendly error: *"This payment link is invalid or has expired."*

---

#### 2. Download Invoice PDF (Client)
- **URL:** `GET /api/public/invoices/{token}/pdf`
- **Auth Required:** No
- **Response (200 OK):** Binary PDF stream (`application/pdf`).

---

#### 3. Upload Payment Proof
- **URL:** `POST /api/public/invoices/{token}/proof`
- **Auth Required:** No
- **Content-Type:** `multipart/form-data`
- **Form Data Field:** `file` (JPG, PNG, or PDF, max **5 MB**)
- **Behavior:**
  - Validates file type and size.
  - Stores file with random GUID name (never original filename).
  - Updates invoice status to `"ProofSubmitted"`.
  - Automatically suspends further reminder emails.
- **Response (200 OK):**
  ```json
  {
    "message": "Proof of payment received. The accountant will review it shortly.",
    "proof": {
      "id": 1,
      "invoiceId": 12,
      "storedFileName": "e4f2b960...jpg",
      "fileType": "jpg",
      "fileSize": 204850,
      "uploadedAt": "2026-11-12T14:30:00Z",
      "reviewStatus": "Pending",
      "rejectReason": null,
      "reviewedAt": null
    }
  }
  ```
- **Error Responses:**
  - `400 Bad Request`: File missing, exceeds 5 MB, or wrong file extension.
  - `404 Not Found`: Invalid or expired payment token.

---

### 🔍 Payment Proof Review (`/api/payment-proofs`)

> Accountant / business owner interface to inspect and approve submitted payments.

#### 1. List Proofs for Invoice
- **URL:** `GET /api/payment-proofs/invoice/{invoiceId}`
- **Auth Required:** Yes (`Bearer <token>`)
- **Response (200 OK):**
  ```json
  [
    {
      "id": 1,
      "invoiceId": 12,
      "storedFileName": "e4f2b960...jpg",
      "fileType": "jpg",
      "fileSize": 204850,
      "uploadedAt": "2026-11-12T14:30:00Z",
      "reviewStatus": "Pending",
      "rejectReason": null,
      "reviewedAt": null
    }
  ]
  ```

---

#### 2. Approve or Reject Proof
- **URL:** `PATCH /api/payment-proofs/{id}/review`
- **Auth Required:** Yes (`Bearer <token>`)
- **Request Body (Approve):**
  ```json
  {
    "approved": true
  }
  ```
- **Request Body (Reject):**
  ```json
  {
    "approved": false,
    "rejectReason": "Transaction reference number not legible in screenshot."
  }
  ```
- **Behavior:**
  - If approved: Proof status becomes `"Approved"`, Invoice status becomes `"Paid"`.
  - If rejected: Proof status becomes `"Rejected"`, Invoice status becomes `"Rejected"` (reminders may resume).
- **Response (200 OK):** Updated payment proof object.
- **Response (400 Bad Request):** If `approved: false` is sent without `rejectReason`.

---

## 5. Background Jobs (Hangfire)

- **Dashboard:** Visit `/hangfire` in browser.
- **Schedule:** Runs every morning at **08:00 AM UTC** (`0 8 * * *`).
- **Automated Workflow:**
  1. Checks pending invoices where `Client.IsReminderEnabled == true`.
  2. Invoices due in 3 days: Sends gentle reminder + PDF attachment.
  3. Invoices 1–3 days overdue: Sends overdue reminder.
  4. Invoices 4+ days overdue: Sends urgent firmer reminder.
  5. 1st of every month: Automatically generates and emails recurring invoices based on previous month's recurring templates.
