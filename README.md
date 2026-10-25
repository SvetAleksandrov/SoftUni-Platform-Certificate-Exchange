# SecureCert Portal

**SecureCert Portal is my response to a real business problem in the organization where I work.** As part of our day-to-day operations, we need to obtain public SSL/TLS certificates from clients so that they can be checked, tracked, and used in the configuration of internal business systems. I am working on a more structured way to handle this process instead of relying on emails, attachments, and manual follow-ups.

The goal is to give clients a dedicated place to submit certificates and give our internal teams a clear view of **which organization submitted which certificate, for what hostname, when it was received, and what decision was made**. This should make the exchange more consistent, traceable, and easier to manage as the service grows.

**The same business problem became the foundation of my SoftUni ASP.NET Fundamentals coursework project (September 2026).** The current repository contains the first working version developed within the course's scope. It implements the core organization, user, certificate submission, validation, and review workflows, but **does not yet contain every integration required for deployment in our corporate environment**.

> **Implementation status:** Core portal functionality is implemented as a coursework version. Integration with the corporate **Active Directory (AD)**, **automatic registration emails through the corporate mail gateway**, and **possible support for additional certificate file types** are planned enhancements, not features available in the current version.

## 1. Business workflow

1. A visitor opens the public Welcome page and can sign in; public self-registration is not available.
2. An **Admin** creates a client organization and one or more associated **Client** accounts.
3. The portal generates a temporary password for each new Client account. The Admin receives it once and must deliver it to the intended user through an appropriate secure channel.
4. On first sign-in, the Client must change the temporary password before accessing protected application features.
5. The Client uploads a public certificate and specifies the target hostname.
6. The application performs technical validation and records the submission.
7. An Admin reviews the submission and either approves or rejects it, with a review comment when rejecting.
8. Authorized users can view status, inspect metadata, and download the public certificate.

## 2. Technologies

| Component | Technology |
| --- | --- |
| Application framework | ASP.NET Core MVC, .NET 8 |
| Language | C# |
| Views and UI | Razor Views, Bootstrap, HTML, CSS, JavaScript |
| Authentication | ASP.NET Core Identity |
| Authorization | Admin and Client roles |
| Data access | Entity Framework Core 8 |
| Database | Microsoft SQL Server / SQL Server LocalDB |
| Certificate parsing | .NET X.509 certificate APIs |
| Development IDE | Visual Studio 2022 |

## 3. Main features

### Public pages

- Welcome page explaining the submission and review process.
- Login for administrator-created accounts.
- Security & Privacy guidance accessible from the footer.

### Admin

- Create, view, edit, and manage organizations.
- View the users assigned to a specific organization.
- Create Client accounts with an automatically generated temporary password.
- View, edit, and conditionally delete Client accounts; users with certificate submission or review history cannot be deleted through the current flow.
- View certificate submissions from all organizations.
- Review submissions and record approval/rejection decisions.
- View dashboard statistics and recent certificate submissions.

### Client

- Sign in and complete mandatory initial password change.
- View certificates belonging **only to the Client's assigned active organization**.
- Upload and track certificate submissions for that organization.
- Edit the description or delete an eligible unreviewed submission.
- Download public certificates that the Client is authorized to access.
- View an organization-scoped dashboard.

### Tables and navigation

- Per-column search and filtering for Organizations, Users, and Certificates.
- Sortable table headings and selectable grouping.
- Live filtering through JavaScript requests without full-page reloads.
- Certificate date filters for **Valid From**, **Valid To**, and **Submitted At**.
- Responsive navigation and adjustable content width.

## 4. User roles and data isolation

| Operation | Admin | Client |
| --- | --- | --- |
| Manage organizations | Yes | No |
| Create and manage Client accounts | Yes | No |
| View organization certificates | All organizations | Assigned organization only |
| Upload a public certificate | Yes | Assigned organization only |
| Review / approve / reject | Yes | No |
| Download an authorized certificate | Yes | Yes |

Organization isolation is enforced in controller/database queries, not just by hiding buttons in the UI. A Client cannot choose another organization's ID to gain access to its submissions.

## 5. Data model

The primary domain entities are:

- **Organization** — name, registration number, active status.
- **ApplicationUser** — Identity user with first name, last name, contact information, department, job title, optional `OrganizationId`, and `MustChangePassword`.
- **CertificateSubmission** — organization, uploader, hostname, file reference, extracted certificate metadata, timestamps, and status.
- **CertificateReview** — certificate submission, reviewer, decision, comment, and review timestamp.

Relationships include an organization-to-users association and an organization-to-certificate-submissions association. A certificate submission can have at most one review record (enforced by a unique index).

The application also uses the standard ASP.NET Core Identity tables for users, roles, and related authentication data.

### Certificate statuses

`Draft`, `Submitted`, `UnderReview`, `Approved`, `Rejected`.

Uploaded certificates are initially recorded as `Submitted`. The review workflow records an `Approved` or `Rejected` result; not every defined status necessarily has a separate UI transition.

## 6. Certificate validation and storage

Accepted public certificate file extensions:

- `.cer`
- `.crt`
- `.pem`

Maximum upload size: **256 KB**.

The current validation service checks supported file format, parseability as a public X.509 certificate, certificate validity dates, the match between the certificate and requested hostname, and server-authentication usage when the relevant extension is present. It extracts metadata such as subject, issuer, thumbprint, and validity dates.

**Important limitations:**

- The application does **not** perform full certificate-chain trust verification.
- It does **not** perform CRL/OCSP certificate-revocation checking.
- A successful upload or an Admin approval is **not** proof that a certificate is trusted by browsers or operating systems.
- Private-key files and certificate packages containing private keys must **never** be submitted. The upload workflow is intended for public certificates only.

Uploaded files are stored **outside `wwwroot`**, under the running Windows account's local application data directory:

```text
%LOCALAPPDATA%\SecureCertPortal\Certificates\
```

The database stores file references and metadata; downloads are served through an authorized MVC action rather than a public static-file URL.

## 7. Requirements

- Windows with **Visual Studio 2022**, supporting ASP.NET Core development.
- **.NET 8 SDK** and runtime.
- **SQL Server LocalDB** (or another configured SQL Server instance).
- Internet access for the initial NuGet package restore, if packages are not already cached.

The default development configuration uses Windows Authentication with SQL Server LocalDB. On a different machine or SQL Server instance, update the `DefaultConnection` configuration accordingly.

## 8. Running locally

### Step 1 — Get the source

Clone the repository or download its source code, then open the `.sln` file in Visual Studio 2022.

### Step 2 — Check the connection string

The development connection string is configured under `ConnectionStrings:DefaultConnection` in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SoftUni_PlatformCertificateExchange;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

This uses SQL Server LocalDB and Windows Authentication. **Do not commit database passwords or other secrets** to configuration files.

### Step 3 — Configure the development administrator

In Visual Studio, right-click the web project and choose **Manage User Secrets**. Add the two keys below to the local secrets file, using your own development values:

```json
{
  "SeedAdmin": {
    "Email": "your-admin@example.test",
    "Password": "REPLACE_WITH_A_STRONG_LOCAL_DEVELOPMENT_PASSWORD"
  }
}
```

The initial Admin role/account is seeded when the application runs in the **Development** environment. Do not commit the secrets file or publish real credentials in this README. Existing Admin passwords are not reset on every startup.

### Step 4 — Create or update the database

In Visual Studio, open **Tools → NuGet Package Manager → Package Manager Console**, select the web project as **Default project**, and run:

```powershell
Update-Database
```

This applies the Entity Framework Core migrations to the configured database. The `Migrations` directory is included in source control.

### Step 5 — Start the application

Use **F5** or **Ctrl+F5** in Visual Studio. Visual Studio will open the locally configured HTTPS address (for example, `https://localhost:7229`). The exact port may differ between development environments.

Open the Welcome page, then sign in with the development Admin credentials configured through User Secrets.

## 9. Suggested demonstration

1. Log in as Admin and create two test organizations.
2. Create a Client account for each organization; record each newly generated temporary password securely.
3. Log out and sign in as a Client; complete the mandatory password change.
4. Upload a public certificate whose hostname matches the value entered in the form.
5. Verify that certificate details and status are visible only to the assigned organization.
6. Log in as Admin, review the submission, and approve or reject it.
7. Log in as the Client again and verify status and authorized download.
8. Test table search, grouping, sorting, and date filtering.
9. Verify that a Client cannot access another organization's certificate by changing an ID in the URL.

Do not use production credentials, private keys, or real client certificate data in the demonstration.

## 10. Project structure

```text
Controllers/        MVC controllers and authorized actions
Data/               Entity Framework DbContext and development Admin seeder
Migrations/         EF Core database migrations
Models/             Domain entities and ApplicationUser
Services/           Certificate validation service and password generator
ViewModels/         Form and dashboard presentation models
Views/              Razor views and shared layout
wwwroot/            CSS, JavaScript, and static UI assets
Program.cs          Service configuration and HTTP middleware pipeline
appsettings.json    Non-secret application settings
```

## 11. Security measures and limitations

Implemented safeguards include:

- Cookie-based authentication through ASP.NET Core Identity.
- Role-based authorization and server-side organization access checks.
- Mandatory temporary-password change before normal access.
- Cryptographically random generation of initial Client passwords.
- ASP.NET Core model validation and anti-forgery protection on state-changing forms.
- Restricting uploaded file types and size; keeping files outside the public web directory.
- Limited modification/deletion of certificate submissions after review.
- Protection of review history from ordinary deletion workflows.

The current project is an **educational prototype**. Before production deployment it would need, at minimum, a security assessment, HTTPS and hosting hardening, audit logging, a defined backup/retention policy, more robust file lifecycle management, appropriate account recovery and email verification, and full certificate-chain/revocation validation where required by the business process.

The Security & Privacy page describes current functionality and guidance; it is **not a complete legal privacy notice**.

## 12. Development and source control

The application follows the MVC pattern, uses dependency injection for application services, and persists data through EF Core migrations.

Local build outputs (`bin/`, `obj/`, `.vs/`), development secrets, private keys, and user-uploaded certificate files should not be committed. Refer to the repository's `.gitignore` for excluded files.
