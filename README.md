# E‑Recruitment Platform

A multi‑tenant SaaS recruitment / Applicant Tracking System (ATS). Built with **ASP.NET Core 8** + **Entity Framework Core** on the backend and **Angular 21** + **Angular Material** on the frontend.
Use the right door
To manage tenants (SuperAdmin): → http://localhost:4200/saas/login

No slug. Just:
Email: superadmin@erecruitment.com
Password: SuperAdmin@123
To log into the Acme company (tenant Admin): → http://localhost:4200/login

Tenant slug: acme
Email: admin@acme.com
Password: Admin@123

The platform provides:

- A **SaaS console** for a global SuperAdmin to onboard tenants
- A **per‑tenant admin console** for managing candidates, jobs, applications, interviews, settings, branding, audit logs, and users
- A **public careers portal** per tenant (`/t/{slug}/jobs`) where candidates browse jobs and apply with a CV — fully themed with the tenant's branding

---

## Table of Contents

- [📸 Screenshots](#-screenshots)

1. [Overview](#1-overview)
2. [Tech Stack](#2-tech-stack)
3. [System Architecture](#3-system-architecture)
4. [Multi‑Tenancy Model](#4-multi-tenancy-model)
5. [Roles & Permissions](#5-roles--permissions)
6. [Features](#6-features)
7. [Folder Structure](#7-folder-structure)
8. [Database Schema](#8-database-schema)
9. [API Reference](#9-api-reference)
10. [Security Model](#10-security-model)
11. [Prerequisites](#11-prerequisites)
12. [Setup & Run](#12-setup--run)
13. [Default Credentials](#13-default-credentials)
14. [User Manual](#14-user-manual)
15. [Public Career Portal Flow](#15-public-career-portal-flow)
16. [Configuration Reference](#16-configuration-reference)
17. [Troubleshooting](#17-troubleshooting)
18. [Viva / Interview Q&A](#18-viva--interview-qa)

---

## 📸 Screenshots

> A quick look at the redesigned UI (Clean Modern SaaS theme).

### Admin console

| Login | Dashboard |
|---|---|
| <img alt="Login" src="https://github.com/user-attachments/assets/166f0060-31c3-45fd-853a-3443ee48d557" /> | <img alt="Dashboard" src="https://github.com/user-attachments/assets/e48334c0-eb01-4ba4-adde-694c5a35e4bd" /> |

| Candidates | Applications pipeline |
|---|---|
| <img alt="Candidates" src="https://github.com/user-attachments/assets/469e9164-e5ad-43a7-b962-42158e6b6932" /> | <img alt="Applications pipeline" src="https://github.com/user-attachments/assets/53a75a08-3309-4fc1-8a12-eef08191813d" /> |

| Application details — Interviews · Offer · Communication · Notes | Branding & live preview |
|---|---|
| <img alt="Application details" src="https://github.com/user-attachments/assets/363185b2-246c-407e-8699-d33e7b21ff75" /> | <img alt="Branding and live preview" src="https://github.com/user-attachments/assets/68ffa6e1-ed98-496d-b055-23f6a42422d1" /> |

### Public careers portal (tenant‑branded)

| Careers listing | Job details & apply |
|---|---|
| <img alt="Careers listing" src="https://github.com/user-attachments/assets/6a69dac3-9a06-4784-9b9f-ffa53baf4fcc" /> | <img alt="Job details and apply" src="https://github.com/user-attachments/assets/6984f16f-2298-4ec6-9e56-2859a138f975" /> |

---

## 1. Overview

The platform manages the full recruitment lifecycle for multiple companies (tenants) under a single SaaS instance:

| Stage | What the platform does |
|---|---|
| **Onboarding** | A SuperAdmin creates a tenant and its first Admin user. Each tenant gets its own slug (e.g. `acme`) used in URLs (`/t/acme/jobs`). |
| **Job Posting** | Tenant Admins / Recruiters post jobs with status Draft / Published / Closed. Only Published jobs appear on the public portal. |
| **Candidate Sourcing** | Internal candidates can be added by recruiters. External candidates apply via the public careers portal — a single multipart submission creates the candidate, uploads the CV, and creates the application. |
| **Pipeline** | Each application moves through Submitted → Reviewed → Shortlisted → Hired (or Rejected). Every status change is appended to a status‑history table with the actor's email. |
| **Interviews** | Recruiters schedule interview rounds and individual interviews; they can reschedule/edit, send reminders, assign participants, and reviewers submit feedback (rating + decision). The candidate is emailed on schedule/reschedule/cancel/reminder. |
| **Offers** | An offer is drafted, sent, and tracked (Accepted / Declined / Withdrawn). Accepting an offer auto‑advances the application to Hired. |
| **Collaboration** | Recruiters/hiring managers leave internal notes and structured scorecards on an application, and send/track direct emails to the candidate (full communication history). |
| **Branding** | Tenants customize their public portal: company name, logo, primary/secondary/background colors, font family, custom CSS. |
| **Audit** | Significant actions (job created, application status change, public apply, etc.) are written to an audit log with actor, IP, user‑agent, and JSON snapshot. |

---

## 2. Tech Stack

**Backend**

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 (code‑first), SQL Server (LocalDB for dev)
- JWT Bearer authentication (HMAC‑SHA256)
- Swashbuckle / Swagger
- MailKit + MimeKit for SMTP email notifications

**Frontend**

- Angular 21 (standalone components, signals, lazy‑loaded routes)
- Angular Material (CDK + components)
- RxJS, TypeScript 5.9
- Vitest for unit tests

**Tooling**

- `dotnet user-secrets` for local secrets
- Angular CLI / Vite‑powered dev server (HMR)

---

## 3. System Architecture

The backend follows **Clean Architecture** with four projects:

```
ERecruitment.sln
├── src/
│   ├── ERecruitment.Domain/          (entities, base types, ITenantEntity)
│   ├── ERecruitment.Application/     (interfaces: IApplicationDbContext,
│   │                                   IAuditLogger, IEmailNotificationService,
│   │                                   ITenantProvider, ICurrentUser, IDateTime)
│   ├── ERecruitment.Infrastructure/  (EF Core DbContext, migrations, tenancy,
│   │                                   SMTP, audit, seed data)
│   └── ERecruitment.API/             (controllers, DTOs, JWT, middleware,
│                                       Swagger, Program.cs)
└── erecruitment-web/                  (Angular 21 SPA)
```

Dependencies flow inward: API → Application + Infrastructure → Domain. The Infrastructure project depends on Application (to implement its interfaces) and Domain.

### Request lifecycle

```
[Browser]
   │  https://localhost:7289/api/Candidates  (Authorization: Bearer …)
   ▼
[ASP.NET Core Pipeline]
   ├── HTTPS redirection
   ├── Static files (/uploads/* served from wwwroot)
   ├── CORS                       (allows http://localhost:4200)
   ├── Authentication             (validates JWT, populates User claims)
   ├── TenantResolutionMiddleware (reads tenantId claim, validates tenant
   │                               is active, sets TenantProvider)
   └── Authorization              (FallbackPolicy = RequireAuthenticatedUser)
        │
        ▼
[Controller]   →   IApplicationDbContext   →   ApplicationDbContext (EF)
                                                     │
                                                     │  global query filter:
                                                     │  e.TenantId == CurrentTenantId
                                                     ▼
                                                 [SQL Server]
```

---

## 4. Multi‑Tenancy Model

Multi‑tenancy is enforced at the **EF Core query‑filter** level so application code can't accidentally bypass it.

- Every tenant‑scoped entity inherits from `BaseEntity`, which carries a `Guid TenantId` and implements `ITenantEntity`.
- In `ApplicationDbContext.OnModelCreating`, every `ITenantEntity` gets a global query filter:
  ```csharp
  modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
  ```
- `CurrentTenantId` is a property on the DbContext that reads from a scoped `ITenantProvider`. If no tenant is set (e.g. SuperAdmin requests, or pre‑auth requests), it returns `Guid.Empty` — a safe sentinel that matches no real tenant rows.
- `TenantResolutionMiddleware` runs **after** authentication, reads the `tenantId` claim from the JWT, verifies the tenant exists and is active, and calls `_tenantProvider.SetTenant(...)`.
- A `SaveChangesInterceptor` automatically stamps `TenantId`, `CreatedAt`, and `UpdatedAt` on every `BaseEntity` being added, and prevents `TenantId` from being changed on update.

**Tenant resolution rules:**

| Caller | How tenant is set |
|---|---|
| Authenticated tenant user | From `tenantId` claim in JWT, set by `TenantResolutionMiddleware` |
| `SuperAdmin` | Bypasses middleware. SuperAdmin endpoints either don't need tenant scoping (e.g. `/api/Tenants`) or use `IgnoreQueryFilters()` explicitly |
| Public career portal (`/api/public/{slug}/...`) | Looks up tenant by slug, then explicitly calls `_tenantProvider.SetTenant(tenant.Id)` inside the controller |

The `Tenant` entity itself is **not** a tenant entity — it lives outside the filter so SuperAdmin and anonymous slug‑lookups can find tenants.

---

## 5. Roles & Permissions

Four roles are stored in `AppUser.Role`:

| Role | TenantId | Can do |
|---|---|---|
| **SuperAdmin** | `null` | Cross‑tenant SaaS admin: list/create/disable/enable tenants. Cannot directly read tenant‑scoped data through normal endpoints. |
| **Admin** | tenant | Full management within the tenant: users, settings, branding, audit logs, plus everything a Recruiter can do. |
| **Recruiter** | tenant | Manage candidates, jobs, applications, schedule interviews, change application status. |
| **HiringManager** | tenant | Read access to candidates/jobs/applications, can submit interview feedback for interviews they participate in. |

Authorization is enforced two ways:

1. **Role attribute** on controllers/actions: `[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "SuperAdmin")]`, `[Authorize(Roles = "Admin,Recruiter")]`, etc.
2. **Global fallback policy** (`Program.cs`): every endpoint requires an authenticated user unless explicitly marked `[AllowAnonymous]` (Auth controller, Public Career controller).

---

## 6. Features

### SaaS console (SuperAdmin)
- List all tenants with counts (users / jobs / applications)
- Create a new tenant + first Admin user in one call
- Disable / enable a tenant (disabled tenants stop serving public pages and reject logins)

### Tenant admin / recruiter

**Dashboard** — high‑level metrics: candidates, jobs by status, total applications, hires.

**Candidates**
- List (server‑side **paginated + searchable** — the endpoint never loads an unbounded result set), create, edit, delete
- Upload candidate CV (server‑side validates type/size, stored under `wwwroot/uploads/{tenantId}/candidates/{candidateId}/`)
- **CVs are served only through an authenticated, tenant‑scoped endpoint** (`GET /api/Candidates/{id}/resume/file`), never as anonymous static files

**Jobs**
- CRUD with status (Draft / Published / Closed)
- List is server‑side **paginated + searchable**; a lightweight `stats` endpoint powers the dashboard status breakdown
- Only Published jobs are visible on the public portal

**Applications**
- Global search + per‑job pipeline view
- Filters: status, salary range, experience range, keyword (name / email / phone / job title)
- Pagination + sort (date, salary, experience)
- Status update with comment → appended to status‑history with the actor's email (the candidate is emailed a **status‑changed** notice)
- View full status history per application
- Rich application workspace (details dialog) organised into tabs: **Overview · Interviews · Offer · Communication · Notes**

**Interviews**
- Create interview rounds per application (e.g. "Technical Round 1")
- Schedule individual interviews under each round (date, duration, online/onsite, meeting link, participants)
- **Reschedule / edit** an interview (time, duration, mode, location, link, notes, participants) — the candidate is re‑notified with the updated details
- **Send a reminder** email on demand
- Cancel / mark complete
- Submit feedback per reviewer (rating + decision Hire / No Hire)
- Candidate emails on schedule / reschedule / cancel / reminder (configurable SMTP), each recorded in the application's communication history

**Offers**
- Create a draft offer per application (position, salary + currency, start date, expiry, notes)
- Lifecycle **Draft → Sent → Accepted / Declined / Withdrawn**, enforced by server‑side state guards
- **Accepting an offer auto‑advances the application to Hired** (recorded in the status history)

**Communication**
- Send an ad‑hoc email to the candidate directly from the application
- Full **communication history** per application — every email (auto status notices, interview notices, direct emails) with delivery status; failures are recorded with the error so nothing is silent

**Notes & Scorecards**
- Internal collaboration feed per application
- Free‑text **notes** between recruiters / hiring managers
- Structured **scorecards** (Technical / Communication / Culture‑fit, each 1–5, + a recommendation Strong Yes … Strong No)
- Author attribution; deletion restricted to the author or an Admin

**Settings**
- Tenant settings (company name, primary color, file upload limits, allowed resume types)
- Pipeline stages (default Submitted → Reviewed → Shortlisted → Rejected → Hired, customizable)
- Email templates — auto‑seeded **idempotently per type** (ApplicationReceived, StatusChanged, InterviewScheduled, InterviewCancelled, InterviewReminder), all editable by admins

**Branding**
- Logo upload (PNG/JPG/WEBP, stored at `wwwroot/uploads/{tenantId}/branding/logo.{ext}`)
- Favicon
- Primary / Secondary / Background colors, font family, template choice
- Optional Custom CSS (injected into a `<style>` element on the public pages)

**Audit Logs**
- Search by date range, action type, entity type, entity id, actor user, keyword
- Detail dialog shows the full JSON data snapshot, IP, and User-Agent

**Users**
- List tenant users
- Create / edit / disable / delete users
- Reset password
- Toggle active

### Public careers portal (anonymous)

- Per‑tenant URL: `http(s)://your-host/t/{slug}/jobs`
- Job listing with the tenant's branding applied (logo, colors, font, custom CSS)
- Job detail page with description and an apply form
- Multipart application submission: candidate info + CV file
- Server validates file size (≤ 10 MB), content‑type (PDF / DOC / DOCX), and **magic bytes** (so a `.exe` renamed to `.pdf` is rejected)
- Server saves the CV with a random `Guid.N` filename (no path‑traversal)
- Duplicate applications (same email + same job) are rejected with HTTP 409
- Successful submission shows a success card with a short reference code (e.g. `9227B0DB`)
- Disabled tenants (or unknown slugs) show a friendly "Careers page not available" state

---

## 7. Folder Structure

```
.
├── ERecruitment.sln
├── README.md                                    ← you are here
├── src/
│   ├── ERecruitment.Domain/
│   │   ├── Common/
│   │   │   ├── AuditableEntity.cs               (Id + Created/UpdatedAt)
│   │   │   ├── BaseEntity.cs                    (TenantId, implements ITenantEntity)
│   │   │   └── ITenantEntity.cs
│   │   └── Entities/                            (JobPosting, Candidate, JobApplication,
│   │                                              Interview, AppUser, Tenant, AuditLog, …)
│   ├── ERecruitment.Application/
│   │   ├── Abstractions/                        (IApplicationDbContext, IAuditLogger,
│   │   │                                          ITenantProvider, ICurrentUser, …)
│   │   └── Features/                            (placeholder for MediatR-style handlers)
│   ├── ERecruitment.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs          (DbSets, query filters, indexes)
│   │   │   └── SaveChangesInterceptor.cs        (auto-stamps TenantId/audit fields)
│   │   ├── Tenancy/                             (TenantProvider, TenantContext)
│   │   ├── Auth/CurrentUser.cs                  (ICurrentUser via HttpContext)
│   │   ├── Auditing/AuditLogger.cs
│   │   ├── Email/                               (SmtpEmailSender, EmailNotificationService)
│   │   ├── Migrations/                          (EF Core migrations)
│   │   ├── Seeding/SeedData.cs                  (creates SuperAdmin on startup)
│   │   └── DependencyInjection/ServiceCollectionExtensions.cs
│   └── ERecruitment.API/
│       ├── Controllers/                         (Auth, Tenants, Candidates, Jobs,
│       │                                          Applications, Interviews, Settings,
│       │                                          PublicCareer, PublicTenants/Theme,
│       │                                          AuditLogs, Users)
│       ├── DTOs/                                (request/response shapes)
│       ├── Middleware/TenantResolutionMiddleware.cs
│       ├── Security/JwtTokenService.cs
│       ├── Extensions/                          (Swagger setup, etc.)
│       ├── wwwroot/uploads/                     (tenant-scoped resumes & branding)
│       ├── appsettings.json                     (placeholders only — real values via secrets)
│       └── Program.cs
└── erecruitment-web/
    ├── package.json
    ├── angular.json
    ├── src/
    │   ├── environments/environment.ts          (apiBaseUrl)
    │   ├── main.ts
    │   ├── styles.scss
    │   └── app/
    │       ├── app.config.ts                    (HTTP, router, animations, interceptors)
    │       ├── app.routes.ts                    (lazy-loaded routes)
    │       ├── core/
    │       │   ├── guards/auth.guard.ts
    │       │   ├── interceptors/auth.interceptor.ts  (adds Bearer, handles 401)
    │       │   ├── models/api-models.ts
    │       │   └── services/                    (one service per backend controller)
    │       ├── layout/shell/                    (sidenav + role-filtered menu)
    │       ├── pages/
    │       │   ├── auth/login + auth/superadmin-login
    │       │   ├── dashboard/
    │       │   ├── candidates/
    │       │   ├── jobs/
    │       │   ├── applications/                (list, status update, history,
    │       │   │                                 details dialog with embedded interviews)
    │       │   ├── users/
    │       │   ├── settings/                    (tenant settings, pipeline, templates,
    │       │   │                                 branding sub-page)
    │       │   ├── audit-logs/
    │       │   └── saas/tenants/                (SuperAdmin tenant console)
    │       ├── public/
    │       │   ├── public-jobs/                 (/t/:slug/jobs)
    │       │   └── public-job-details/          (/t/:slug/jobs/:id, application form)
    │       └── shared/components/
    └── tsconfig.app.json / tsconfig.spec.json
```

---

## 8. Database Schema

The DB is created code‑first via EF Core migrations.

**Common base types**

- `AuditableEntity` — `Id (Guid)`, `CreatedAt`, `UpdatedAt`
- `BaseEntity : AuditableEntity, ITenantEntity` — adds `TenantId (Guid)`

**Tenancy & users**

| Entity | Notes |
|---|---|
| `Tenant` | `Id`, `Name`, `Slug` (unique), `IsActive`, `Plan`, `BillingEmail`, `CreatedAt`, `DisabledAt`. Not tenant‑filtered. |
| `AppUser` | `TenantId` is **nullable** (null for SuperAdmin). `FullName`, `Email`, `PasswordHash`, `Role`, `IsActive`. Indexed on `(TenantId, Email)`. |

**Recruitment core**

| Entity | Notes |
|---|---|
| `JobPosting` | `Title`, `Department`, `Location`, `Description`, `Status` (Draft/Published/Closed). |
| `Candidate` | Personal info, single previous company + experience, education (institute + subject), expected salary, **resume metadata** (filename, content-type, size, URL). Unique `(TenantId, Email)`. |
| `JobApplication` | `CandidateId`, `JobPostingId`, `Status`, `Notes`, expected salary, **CV snapshot** (`ResumeUrlSnapshot`, etc. — preserves the version reviewed). Unique `(TenantId, CandidateId, JobPostingId)` — used by the duplicate‑application guard. |
| `JobApplicationStatusHistory` | `FromStatus`, `ToStatus`, `Comment`, `ChangedBy` (actor email). Indexed on `(TenantId, JobApplicationId, CreatedAt)`. |

**Interviews**

| Entity | Notes |
|---|---|
| `InterviewRound` | Per application, `Name`, `SortOrder`, `Status`. |
| `Interview` | `JobApplicationId`, `InterviewRoundId`, `StartsAtUtc`, `DurationMinutes`, `Mode` (Online/Onsite/Phone), `Location`, `MeetingLink`, `Status`, `Notes`. |
| `InterviewParticipant` | One row per `(Interview, User)`. Unique. |
| `InterviewFeedback` | One row per `(Interview, ReviewerUser)`. Rating + decision + comments. |

**Offers & collaboration**

| Entity | Notes |
|---|---|
| `Offer` | `JobApplicationId`, `CandidateId`, `JobPostingId`, `PositionTitle`, `Salary` + `SalaryCurrency`, `StartDate`, `ExpiresAt`, `Notes`, `Status` (Draft/Sent/Accepted/Declined/Withdrawn/Expired), `CreatedByEmail`, `SentAt`, `RespondedAt`, `ResponseNote`. Indexed on `(TenantId, JobApplicationId)`. |
| `ApplicationNote` | Per‑application collaboration item. `Kind` (Note/Scorecard), `Body`, `AuthorUserId`/`AuthorEmail`; scorecards add `TechnicalScore`, `CommunicationScore`, `CultureFitScore` (1–5) + `Recommendation`. Indexed on `(TenantId, JobApplicationId, CreatedAt)`. |

**Configuration**

| Entity | Notes |
|---|---|
| `TenantSettings` | Company name, file upload limits, allowed resume types, primary color, timezone. Unique on `TenantId`. |
| `TenantThemeSettings` | Branding: logo URL, favicon URL, primary/secondary/background colors, font family, template, custom CSS. Unique on `TenantId`. |
| `PipelineStage` | Customizable stages, unique `(TenantId, Key)`. |
| `EmailTemplate` | Per `(TenantId, TemplateType)`, unique. |
| `EmailLog` | History of every sent email (`ToEmail`, `TemplateType`, `Subject`, `Body`, `Status`, `Error`, `RelatedId` = applicationId). Backs the per‑application **communication history**. |
| `AuditLog` | Action, EntityType, EntityId, actor (UserId, Email, Role), Summary, DataJson, IpAddress, UserAgent. Indexed on `(TenantId, CreatedAt)` and `(TenantId, EntityType, EntityId)`. |

---

## 9. API Reference

Base URL (dev): `https://localhost:7289`. Full schema is at `/swagger`.

### Authentication
| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/api/Auth/login` | Anonymous | Body: `{ tenantSlug, email, password }`. Returns access token + user + tenant. |
| POST | `/api/Auth/register` | Anonymous | Register a tenant user under an existing tenant slug (Admin / Recruiter / HiringManager). |
| POST | `/api/Auth/superadmin/login` | Anonymous | Body: `{ email, password }`. Returns access token (no tenantId claim). |

### Tenants (SuperAdmin only)
| Method | Path | Notes |
|---|---|---|
| GET | `/api/Tenants` | List with `users`/`jobs`/`applications` counts (uses `IgnoreQueryFilters()`). |
| POST | `/api/Tenants/create-with-admin` | Atomic: creates a tenant + first Admin user. |
| PUT | `/api/Tenants/{id}/disable` | Sets `IsActive=false`. Disabled tenants reject logins and 404 the public portal. |
| PUT | `/api/Tenants/{id}/enable` | Re‑enables. |

### Candidates / Jobs / Applications / Interviews / Offers / Communications / Notes / Settings / Audit Logs / Users / TenantSettings (theme)
See Swagger UI for full request/response shapes. Highlights:

- **Candidates**: full CRUD; `GET /api/Candidates` is **paginated + searchable** (`?page=&pageSize=&search=`, returns `{ total, page, pageSize, items }`); `POST /api/Candidates/{id}/resume` for CV upload (multipart, field `file`); `GET /api/Candidates/{id}/resume/file` streams the CV (**authenticated, tenant‑scoped**).
- **Jobs**: full CRUD with role‑gated mutations (`Admin`/`Recruiter`); `GET /api/Jobs` is paginated + searchable; `GET /api/Jobs/stats` returns the status breakdown for the dashboard.
- **Applications**: list / by‑job / by‑candidate, `PUT {id}/status` (history + audit + **status‑changed email**), `GET {id}/history`, `POST /api/Applications/search`, `POST /api/jobs/{jobId}/applications/search`.
- **Interviews**: `GET get-by-application/{appId}` returns rounds + interviews + participants + feedback in one call. `createRound`, `createSchedule`, `PUT {id}` (reschedule/edit), `PUT {id}/reminder`, `{id}/cancel`, `{id}/complete`, `{id}/feedback`. Schedule / reschedule / cancel / reminder email the candidate.
- **Offers**: `GET get-by-application/{appId}`, `POST` (draft), `PUT {id}` (edit draft), `PUT {id}/send`, `PUT {id}/accept` (→ application Hired), `PUT {id}/decline`, `PUT {id}/withdraw` (Admin/Recruiter).
- **Communications**: `GET get-by-application/{appId}` (email history), `POST send` (ad‑hoc email to the candidate, logged) (Admin/Recruiter).
- **ApplicationNotes**: `GET get-by-application/{appId}`, `POST` (note or scorecard), `DELETE {id}` (author or Admin). Open to Admin/Recruiter/HiringManager.
- **Settings**: `GET get-all` auto‑seeds default pipeline stages and email templates (idempotent per template type).
- **TenantSettings/theme**: `GET`, `PUT`, `POST theme/logo` (multipart, field `file`).
- **AuditLogs**: `POST search`, `GET {id}` (Admin only).
- **Users**: list/create/update/reset‑password/toggle‑active/delete (Admin only).

### Public career portal (anonymous)
| Method | Path | Notes |
|---|---|---|
| GET | `/api/public/{slug}/jobs/get-all` | List Published jobs. Project to a public DTO (no `tenantId` / internal columns). |
| GET | `/api/public/{slug}/jobs/{jobId}` | Job detail. |
| GET | `/api/public/{slug}/theme` | Tenant branding for the public portal. |
| POST | `/api/public/{slug}/jobs/{jobId}/apply` | Multipart submission (`Resume` file + form fields). Returns `{ message, applicationId, referenceCode }`. |

---

## 10. Security Model

| Concern | Implementation |
|---|---|
| **Authentication** | JWT Bearer (HMAC‑SHA256). Token is signed with a secret stored in user‑secrets (dev) / env vars (prod). The signing key must be ≥ 32 chars; `Program.cs` fails fast otherwise. JWT carries `sub` (user id), `email`, `role`, `tenantId` (nullable), `fullName`. The handler is configured with `MapInboundClaims = false` so claim names match JWT registered names rather than legacy schema URIs. |
| **Authorization** | Global fallback policy `RequireAuthenticatedUser` on every endpoint. Public endpoints opt out with `[AllowAnonymous]`. Per‑role gates use `[Authorize(Roles = "...")]`. |
| **Tenant isolation** | EF Core global query filter on every `ITenantEntity`: `e.TenantId == CurrentTenantId`. SuperAdmin gets `Guid.Empty` (matches no rows) by default. Cross‑tenant SuperAdmin queries must opt in via `IgnoreQueryFilters()`. The `SaveChangesInterceptor` stamps `TenantId` on insert and prevents tenant changes on update. |
| **Password storage** | ASP.NET `PasswordHasher<AppUser>` (PBKDF2 via Identity v3 format). |
| **Public file uploads (CV)** | Server‑side validation: size ≤ 10 MB, content‑type whitelist (PDF, DOC, DOCX), **magic‑byte verification** (so a renamed file is rejected), random `Guid.N` filename to prevent path traversal and overwrite collisions. Saved under `wwwroot/uploads/{tenantId}/candidates/{candidateId}/`. |
| **CV access control** | Resumes are **not** served as static files. Request‑blocking middleware (before `UseStaticFiles`) returns 404 for any `/uploads/**/candidates/**` URL; CVs are streamed only via the authenticated, tenant‑scoped `GET /api/Candidates/{id}/resume/file`. Tenant branding under `/uploads/**/branding/**` stays public. |
| **Duplicate‑apply guard** | Public apply checks `(candidateId, jobPostingId)` and returns 409 if already applied. Backed by the unique index on `JobApplication`. |
| **Tenant disable kill‑switch** | All public endpoints reject disabled tenants with 404. Tenant logins also fail closed. |
| **Auditing** | `AuditLogger` writes `AuditLog` rows for status changes, user toggles, public applies, etc. — including actor user id, email, role, IP, user agent, JSON data snapshot. |
| **CORS** | Dev allows only `http://localhost:4200`. Production should restrict to the deployed origin. |
| **Secrets** | `appsettings.json` ships with empty placeholders. Real values must come from `dotnet user-secrets` (dev) or environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Smtp__*`) in production. |
| **Frontend stale‑token handling** | `auth.interceptor.ts` catches 401 on any non‑login request, clears localStorage, and redirects to `/login` (or `/saas/login`). `auth.guard.ts` decodes the JWT and rejects tokens past their `exp` claim — guard‑level early termination. |

### Known limitations (documented honestly)

- JWTs are stored in **localStorage** (XSS‑exfiltratable). HttpOnly‑cookie auth is a documented next step but not yet implemented.
- No rate limiting on `/api/Auth/login` or `/api/public/*/apply` yet.
- No CAPTCHA on the public apply endpoint.
- No refresh tokens — when the access token expires, the user is redirected to login.

---

## 11. Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.x (a newer SDK such as 10.x also works **if** the .NET 8 runtime is installed) |
| EF Core CLI (`dotnet-ef`) | latest — install with `dotnet tool install --global dotnet-ef` (needed to create the DB) |
| SQL Server LocalDB (or a real SQL Server) | bundled with Visual Studio / SQL Server Express |
| Node.js | 20.x or newer (npm 11.x) |
| Angular CLI | optional (npm scripts work without it) |

---

## 12. Setup & Run

### 12.1 Clone

```powershell
git clone <your-fork-url>
cd Recruitment
```

### 12.2 Configure backend secrets (one‑time)

Real values **never** go in `appsettings.json`. Use `dotnet user-secrets`:

```powershell
cd src\ERecruitment.API
dotnet user-secrets init                                 # idempotent

# DB connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" `
  "Server=(localdb)\MSSQLLocalDB;Database=ERecruitmentDb;Trusted_Connection=True;TrustServerCertificate=True;"

# JWT signing key — must be ≥ 32 random chars
dotnet user-secrets set "Jwt:Key" "<generate-a-long-random-string>"

# SMTP (optional, only if you want application‑received emails to actually send)
dotnet user-secrets set "Smtp:Host"      "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port"      "587"
dotnet user-secrets set "Smtp:User"      "you@example.com"
dotnet user-secrets set "Smtp:Pass"      "<app-password>"
dotnet user-secrets set "Smtp:FromEmail" "you@example.com"
dotnet user-secrets set "Smtp:FromName"  "ERecruitment"
```

> **Production**: instead of user‑secrets, set environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Smtp__Host`, etc.). `Program.cs` fails fast if `Jwt:Key` is missing or shorter than 32 characters.

### 12.3 Apply database migrations

> ⚠️ **This step is mandatory and must be done before the first backend run.** The app does **not** auto‑migrate on startup. If the `ERecruitmentDb` database does not exist, the backend will build and connect, then **crash during startup** when the SuperAdmin seed step queries a non‑existent database — the API exits before binding its port.

First install the EF Core CLI tool (one‑time, global). It's required for `dotnet ef`:

```powershell
dotnet tool install --global dotnet-ef        # idempotent; skip if already installed
```

> The global tool is installed to `%USERPROFILE%\.dotnet\tools`. If `dotnet ef` is "not found" right after install, that folder isn't on your `PATH` yet — open a new terminal, or prepend it for the current session:
> ```powershell
> $env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"
> ```

Then, from the repo root, create/update the database:

```powershell
dotnet ef database update `
  --project src\ERecruitment.Infrastructure `
  --startup-project src\ERecruitment.API
```

This creates the `ERecruitmentDb` database and applies all migrations (it's idempotent — safe to re‑run; an up‑to‑date DB is a no‑op). Verify it worked: LocalDB should now contain `ERecruitmentDb` with ~17 tables.

### 12.4 Run the backend

```powershell
dotnet run --project src\ERecruitment.API\ERecruitment.API.csproj --launch-profile https
```

The API binds (by default) to:
- HTTP: `http://localhost:5263`
- HTTPS: `https://localhost:7289`
- Swagger UI: `https://localhost:7289/swagger`

The first run also seeds the SuperAdmin user via `SeedData.SeedSuperAdminAsync`.

> **Note on the .NET SDK:** the projects target **`net8.0`**. A newer SDK (e.g. .NET 10) can still build and run them as long as the **.NET 8 runtime** is installed (`dotnet --list-runtimes` should list `Microsoft.AspNetCore.App 8.x`).

> **Dev HTTPS certificate:** the Angular app calls `https://localhost:7289` from the browser, so the ASP.NET dev cert must be trusted, or API calls fail with a certificate error. Trust it once with:
> ```powershell
> dotnet dev-certs https --trust
> ```
> Check status anytime with `dotnet dev-certs https --check --trust`.

### 12.5 Run the frontend

In a separate terminal:

```powershell
cd erecruitment-web
npm install            # first time only
npm start
```

The dev server starts at `http://localhost:4200` and proxies API calls to `https://localhost:7289` per `src/environments/environment.ts`.

> If you deploy the API on a different host/port, update `apiBaseUrl` in `environment.ts` (or use `environment.prod.ts` for a production build).

### 12.6 Production build (frontend)

```powershell
cd erecruitment-web
ng build --configuration production
# Output: dist/erecruitment-web/
```

Serve `dist/erecruitment-web/` behind any static web server. Reverse‑proxy `/api/*` and `/uploads/*` to the API.

---

## 13. Default Credentials

The first time the API starts, `SeedData.SeedSuperAdminAsync` creates the SuperAdmin if it doesn't already exist:

| Role | Email | Password | Login URL |
|---|---|---|---|
| **SuperAdmin** | `superadmin@erecruitment.com` | `SuperAdmin@123` | `http://localhost:4200/saas/login` |

**Change this password immediately in any non‑local environment** (use `PUT /api/Users/{id}/reset-password` from another admin, or update directly in DB via the password hasher).

To create your first **tenant + Admin**, log in as SuperAdmin and either:

- Use the SaaS console UI at `/saas/tenants`, or
- Call `POST /api/Tenants/create-with-admin` with `{ name, slug, adminFullName, adminEmail, adminPassword }`.

The new tenant Admin then logs in at `http://localhost:4200/login` using their tenant slug + email + password.

---

## 14. User Manual

### 14.1 SuperAdmin — onboarding a tenant

1. Open `http://localhost:4200/saas/login`.
2. Sign in with the SuperAdmin credentials.
3. From the sidenav, open **Tenants** (route `/saas/tenants`).
4. Click **Create tenant**, fill in:
   - Tenant name (display name, e.g. "Acme Corp")
   - Slug (URL‑safe, lowercase, no spaces — e.g. `acme`)
   - Plan (Free / Pro / Enterprise — informational)
   - First Admin user: full name, email, password
5. Submit. The tenant Admin can now log in at `/login` using slug `acme` + their email + password.
6. To temporarily suspend a tenant, click **Disable** on its row. The tenant's users will be locked out and `/t/acme/jobs` will return a friendly "not available" page.

### 14.2 Tenant Admin — first‑time setup

After logging in at `/login` (slug + email + password):

1. **Settings → Pipeline stages** — review the default stages (Submitted, Reviewed, Shortlisted, Rejected, Hired). Add custom stages if your process needs them.
2. **Settings → Email templates** — review the default templates that fire on application events.
3. **Branding** (`/settings/branding`) — set company name, upload a logo (PNG/JPG/WEBP), pick primary/secondary/background colors and a font family. Save. The public portal at `/t/{slug}/jobs` immediately reflects these.
4. **Users** — invite recruiters and hiring managers (Admin / Recruiter / HiringManager).

### 14.3 Recruiter — daily workflow

**Posting a job**
1. **Jobs → New job**: title, department, location, description, status `Draft` while you iterate.
2. Once ready, set status to `Published`. The job now appears at `/t/{slug}/jobs`.

**Adding an internal candidate**
1. **Candidates → New candidate**: full name, email (unique per tenant), phone, address, education, experience, expected salary.
2. Open the candidate, **Upload CV** (PDF / DOC / DOCX, ≤ 10 MB).

**Creating an application manually**
1. From a candidate or a job, click **Create application**, pick the counterpart, optionally set expected salary.
2. The application starts as `Submitted`.

**Moving an application through the pipeline**
1. **Applications** → search for the application (by status, salary, experience, keyword) or open it via the per‑job pipeline view.
2. Click **Update status**, pick the new status, optionally add a comment.
3. The change is recorded in the status‑history (with your email as `ChangedBy`) and an `Application.StatusChanged` audit log is written. If SMTP is configured, the candidate gets an email.

**Scheduling interviews**
1. Open an application → **Interviews** tab.
2. **Add round** (e.g. "Technical Round"). Multiple rounds can run sequentially.
3. **Schedule interview** under a round: date/time (UTC), duration, mode (Online / Onsite / Phone), location or meeting link, participants (tenant users).
4. Participants receive an invite email if SMTP is configured.
5. After the interview, **mark complete** and submit feedback (rating + decision). Admins can submit feedback on any interview; non‑Admin users must be participants.

### 14.4 Hiring Manager

- Read‑only browse of candidates / jobs / applications.
- Submit feedback on interviews you're a participant in.

### 14.5 Public applicant — applying for a job

1. Visit the careers URL the company shared, e.g. `http://localhost:4200/t/acme/jobs`.
2. Browse open roles. Each card shows the role, department, and location.
3. Click **Apply**. The job‑detail page shows the description and a multi‑section application form.
4. Fill in the required fields (full name, email, phone) and optional fields (address, previous company, experience, education, expected salary, notes).
5. **Upload CV** — PDF / DOC / DOCX, max 10 MB. The browser validates extension/type/size locally before submission.
6. Click **Submit application**. On success you see a confirmation card with a short reference code (e.g. `9227B0DB`). Save this code in case you contact the company about your application.
7. Trying to apply twice with the same email + same job shows "You have already applied to this job."

---

## 15. Public Career Portal Flow

```
Candidate   ──►  GET  /t/{slug}/jobs                      (Angular route)
                  │
                  ├─►  GET /api/public/{slug}/theme       (tenant branding)
                  └─►  GET /api/public/{slug}/jobs/get-all (Published jobs only)

                  Disabled tenant or unknown slug
                  ───►  Both endpoints return 404
                        SPA shows "Careers page not available"

Candidate   ──►  GET  /t/{slug}/jobs/{jobId}              (apply page)
                  └─►  GET /api/public/{slug}/jobs/{jobId} (job detail)

Candidate   ──►  POST /api/public/{slug}/jobs/{jobId}/apply  (multipart)
                  │   form fields: fullName, email, phone, …
                  │   Resume file
                  │
                  ▼  server-side
                  ├─ Validate active tenant
                  ├─ Validate email format
                  ├─ Validate file: size ≤10MB, MIME whitelist, magic bytes
                  ├─ Find/create Candidate by email (tenant-scoped)
                  ├─ Block duplicate (candidateId + jobId)
                  ├─ Save resume as {Guid.N}.{ext} under wwwroot/uploads/...
                  ├─ Create JobApplication with CV snapshot
                  ├─ Send "application received" email (if SMTP configured)
                  └─ Audit log: Application.AppliedPublic
                  ──►  { message, applicationId, referenceCode }
```

---

## 16. Configuration Reference

`src/ERecruitment.API/appsettings.json` keys (placeholders only — real values via secrets):

```jsonc
{
  "ConnectionStrings": { "DefaultConnection": "" },     // SQL Server
  "Jwt": {
    "Issuer": "ERecruitment",
    "Audience": "ERecruitment",
    "Key": "",                                          // ≥32 chars (Program.cs enforces)
    "AccessTokenMinutes": 120
  },
  "Smtp": {
    "Host": "", "Port": 587,
    "User": "", "Pass": "",
    "FromEmail": "", "FromName": "ERecruitment",
    "UseStartTls": true
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

CORS (in `Program.cs`) is open to `http://localhost:4200` only. Add production origins there before deploying.

---

## 17. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| API exits at startup with `Jwt:Key must be configured and at least 32 characters` | Missing JWT key in user‑secrets / env | `dotnet user-secrets set "Jwt:Key" "<long random>"` |
| API exits with `ConnectionStrings:DefaultConnection is not configured` | Same — missing connection string secret | `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` |
| Backend builds and runs the SuperAdmin seed query, then **exits immediately** (process dies, no port bound) | The `ERecruitmentDb` database doesn't exist yet — there is **no auto‑migrate** on startup | Run the migrations from step 12.3: `dotnet ef database update --project src\ERecruitment.Infrastructure --startup-project src\ERecruitment.API` |
| `Hosting failed to start … Failed to bind to address https://127.0.0.1:7289: address already in use` (`SocketException 10048`) | Another instance of the API is already running on that port (e.g. a Visual Studio debug session **and** a `dotnet run` terminal) | You already have it running — use the existing one. Otherwise stop the stray process: `Get-NetTCPConnection -LocalPort 7289 -State Listen \| % { Stop-Process -Id $_.OwningProcess -Force }` |
| `dotnet ef` is "not found" right after `dotnet tool install --global dotnet-ef` | The global tools folder isn't on `PATH` in the current shell | Open a new terminal, or run `$env:PATH = "$env:USERPROFILE\.dotnet\tools;$env:PATH"` |
| Browser blocks API calls with `NET::ERR_CERT_AUTHORITY_INVALID` / `ERR_CERT_*` against `https://localhost:7289` | The ASP.NET dev HTTPS certificate isn't trusted on this machine | `dotnet dev-certs https --trust` (then restart the browser) |
| `dotnet ef` says "no migrations were applied" | DB already up to date | OK — nothing to do |
| Frontend shows `401 Unauthorized` on every call after a JWT signing key change | Old token in `localStorage` was signed with a different key | Auth interceptor now auto‑clears on 401 and redirects to `/login`. Refresh once. |
| Public page shows "Careers page not available" | Slug not recognized OR tenant disabled | Use the SuperAdmin SaaS console to enable the tenant, or check the slug spelling |
| Logo or favicon missing on the public page | The tenant hasn't uploaded one yet, or the `/uploads/...` path doesn't resolve | Upload a logo via `Settings → Branding`. The frontend's `ThemeService` prepends `environment.apiBaseUrl` to relative paths. |
| File upload returns "File contents do not match the declared type" | The first 4–8 bytes don't match the declared MIME's magic signature | Upload a real PDF/DOC/DOCX, not a renamed file |
| Duplicate apply returns `409 You have already applied to this job.` | A `JobApplication` already exists for this `(candidate email, job)` | Expected behavior |

---

