# Screenshots

Drop your PNG screenshots in **this folder** (`docs/screenshots/`) using the exact
filenames below. The main [`README.md`](../../README.md) → **Screenshots** section
references these names, so once the files are here they render automatically on GitHub.

> Recommended: capture at ~1440px wide, PNG. Keep filenames lowercase, exactly as listed.

## Files the README expects

| Filename | What to capture | How to get there |
|---|---|---|
| `login.png` | Tenant login screen | `http://localhost:4200/login` (logged out) |
| `dashboard.png` | Admin dashboard (KPI cards) | log in as a tenant admin, landing page `/` |
| `candidates.png` | Candidates list page | `/candidates` |
| `jobs.png` *(optional)* | Jobs list page | `/jobs` |
| `applications.png` | Applications pipeline + filters | `/applications` |
| `application-details.png` | Application details dialog (Interviews / Offer / Communication / Notes tabs) | `/applications` → open an application → **View details** |
| `settings.png` *(optional)* | Tenant settings (pipeline / templates) | `/settings` |
| `branding.png` | Branding page with the live preview | `/settings/branding` |
| `saas-tenants.png` *(optional)* | SuperAdmin tenants console | log in at `/saas/login` → `/saas/tenants` |
| `public-careers.png` | Public careers portal (tenant‑branded) | `http://localhost:4200/t/acme/jobs` |
| `public-apply.png` | Public job details + apply form | `/t/acme/jobs/{id}` |

Demo logins (dev):
- **Tenant admin** — slug `acme`, `admin@acme.com` / `Admin@123`
- **SuperAdmin** — `superadmin@erecruitment.com` / `SuperAdmin@123`
