# PeopleHub HRMS · ASP.NET Core + HTMX + AdminLTE

A local-first, **Keka-style HRMS application starter** built in ASP.NET Core 10, EF Core SQLite, Razor MVC + **HTMX 2**, AdminLTE 4, Bootstrap 5 and a configurable theme studio. Original layout and sample data; not affiliated with Keka. UI and theme patterns were informed by CoreApp-AI, not copied wholesale.

> **Implementation status:** An executable functional foundation with generic data-entry workflows and an authenticated REST API across 24 HR domains. **Not a feature-equivalent production clone of Keka.** Payroll rules, WPS, statutory benefits, biometric attendance, approval delegation, recruitment automations, notifications, document uploads, org switching, integrations, detailed reporting and granular RBAC require dedicated implementation and validation.

## Local quick start (Windows PowerShell)

Prerequisites: **.NET SDK 10**, **Node 22+**, **npm 10+**.

```powershell
git clone https://github.com/nasirsaikh/Keka-HRMS-Clone.git
cd Keka-HRMS-Clone
npm install
mkdir src/PeopleHub/wwwroot/vendor -Force
# Windows: copy dependencies with the PowerShell tool below
powershell -ExecutionPolicy Bypass -File tools/setup-assets.ps1
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:HRMS_SEED_DEMO = "true"
$env:HRMS_SEED_PASSWORD = "Choose-A-Unique-Strong-Password-2026!"
dotnet run --project src/PeopleHub --urls http://127.0.0.1:5000
```

Open `http://127.0.0.1:5000/auth/login`, sign in as **demo.hr** with the password you specified. Change it for any shared development system. The demo includes 12 fictional employees, attendance records, leave policies, sample leave requests and openings. The seed command runs on startup **only when** `HRMS_SEED_DEMO=true`; new real users must be provisioned by an administrator (currently via Identity tooling or custom code).

**Linux/macOS:** `bash tools/setup-assets.sh` in place of PowerShell. Static vendor assets are intentionally not committed; setup is required. Demo sample email addresses use the reserved `.invalid` domain.

## Features provided

- ASP.NET Core Identity authentication, secure cookie sessions, role groups (Admin / HR / Manager / Employee), and personal scope for employee requests.
- Company-isolated domain entities in SQLite; EF Core indexes and relational validation for common foreign keys; audit entries on writes and status decisions.
- Overview dashboard with live counts, recent leave and action links.
- HTMX CRUD modals, validation error fragments, auto-refreshing tables, pagination, page CSV export, search, status approvals and self-approval prevention for employee-owned requests.
- REST API at `/api/v1`, optional bearer token at `POST /api/v1/auth/token`, Identity-protected modules, CRUD and decision endpoints.
- AdminLTE responsive layout with sidebar, compact tables, mobile navigation, dark/light mode, configurable themes, live preview, presets, theme generator, font, radius and density controls, JSON import/export and workspace theme saving.
- Modules: People directory, departments, attendance, leave policies/requests, shifts, holidays, payroll runs, payslip records, expense claims, job openings, applicant tracking, performance reviews, learning catalog/enrollments, HR tickets, assets, projects, timesheets, onboarding, offboarding, documents (metadata), engagement surveys and themes.

## APIs

`POST /api/v1/auth/token` JSON `{ "username": "demo.hr", "password": "…" }` retrieves a bearer token. In a production client, handle expiry/refresh and use HTTPS. All other endpoints require cookie or bearer authentication. For cookie-authenticated mutations, the antiforgery header `RequestVerificationToken` must be supplied.

- `GET /api/v1/me` — authenticated account info.
- `GET /api/v1/modules` — available module registry.
- `GET /api/v1/{slug}?page=1&search=…` — paginated module data.
- `GET /api/v1/{slug}/{id}` — view record.
- `POST /api/v1/{slug}` — create record (authorized role only).
- `PUT /api/v1/{slug}/{id}` — update record.
- `DELETE /api/v1/{slug}/{id}` — HR-authorized deletion.
- `POST /api/v1/{slug}/{id}/decision` JSON `{ "status": "Approved" }` — status decision (approved roles only).
- `GET /health` — simple health response, not intended as a readiness probe.

## Architecture and limitations

`src/PeopleHub/Domain` defines records, `Data` handles EF Core/Identity, `Services` owns access, dynamic module metadata, generic validation, and themes, `Controllers` serves full Razor pages, HTMX fragments, and API JSON, and `Views` plus `wwwroot` form the frontend. SQLite database is **not committed**.

The initial development database is created with `EnsureCreated()` so local bootstrapping does not require migrations. For production, replace this with reviewed EF Core migrations **before** deployment; never use `EnsureCreated` to manage evolving schemas. Production startup is deliberately guarded. Review deployment secrets, HTTPS, CORS/CSRF, rate limits, tenancy, file privacy, backup/retention, payroll/legal requirements and observability. This repository is a development foundation, not an audited HR/payroll product.

## Checks

```powershell
dotnet build src/PeopleHub/PeopleHub.csproj
dotnet test tests/PeopleHub.Tests/PeopleHub.Tests.csproj
```

GitHub Actions builds the application and runs tests on `main` pushes. The repository was initially authored where the .NET SDK was unavailable, so CI provides the executable build verification once pushed.

## Third-party licenses

AdminLTE by ColorlibHQ (MIT), Bootstrap (MIT), Bootstrap Icons (MIT) and HTMX (Zero-Clause BSD). Dependencies are installed from npm during setup. This application uses an original PeopleHub identity and **does not reuse Keka trademarks, logos or proprietary assets**.
