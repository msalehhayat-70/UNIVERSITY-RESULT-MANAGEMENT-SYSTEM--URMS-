# URMS — Quick Start (5 Steps to Running)

## Step 1 — Install Prerequisites
- Visual Studio 2022 Community: https://visualstudio.microsoft.com
  - Workloads: "ASP.NET and web development" + ".NET desktop development"
- PostgreSQL 16: https://www.postgresql.org/download/windows/
  - During install: set password for `postgres` user, keep port 5432

## Step 2 — Configure Database
Open `src/URMS.API/appsettings.json` and update:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=URMS;Username=postgres;Password=YOUR_PASSWORD"
```
Replace `YOUR_PASSWORD` with your PostgreSQL password.

## Step 3 — Create Database Tables

**Option A (Recommended) — EF Core:**
Open Developer PowerShell in Visual Studio or any terminal:
```powershell
cd C:\path\to\URMS
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/URMS.Data --startup-project src/URMS.API
```

**Option B — Manual SQL:**
Open pgAdmin → URMS database → Query Tool → paste contents of `database_manual_script.sql` → Run

## Step 4 — Run the API
```powershell
cd src/URMS.API
dotnet run
```
✓ API: http://localhost:5000
✓ Swagger: http://localhost:5000/swagger

## Step 5 — Run Blazor Frontend
Open a second terminal:
```powershell
cd src/URMS.Blazor
dotnet run
```
✓ App: http://localhost:5001

---

## First Launch Flow

1. Open http://localhost:5001
2. Click **Create Account** tab
3. Fill in name/email/password → Submit → **Examiner account created**
4. Click **Create Account** again
5. Fill in name/email/password → Submit → **HOD account created**
6. Registration button disappears permanently
7. Log in as Examiner → Dashboard → Start!

---

## Test the Full Flow

| Step | Who | Action |
|------|-----|--------|
| 1 | Examiner | Create batch (Dashboard → + Batch) |
| 2 | Examiner | Add students (Students page) |
| 3 | Examiner | Add subjects with credit hours (Dashboard → select semester → + Add Subject) |
| 4 | Examiner | Create teacher account (Teachers page) |
| 5 | Teacher  | Log in → Upload grade sheet Excel |
| 6 | HOD      | Log in → Review → Approve gradesheet |
| 7 | Examiner | Generate Result (Results page → Generate button turns gold) |
| 8 | Examiner | Download Class PDF + Individual Cards ZIP |

---

## Excel Format for Grade Sheet Upload
```
Row 1: Registration No | Student Name | Marks Obtained
Row 2: 232201070 | Muhammad Saleh Hayat | 78
Row 3: 232201086 | Syed Ali Murtajiz Bukhari | 65
...
```

## Excel Format for Student Bulk Import
```
Row 1: Registration No | Full Name | Father Name
Row 2: 232201070 | Muhammad Saleh Hayat | Gul Nisar
...
```

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `dotnet-ef: not found` | Run: `dotnet tool install --global dotnet-ef` |
| `Connection refused` on Blazor | Start the API first (Step 4) |
| CORS error | Ensure API runs on port 5000, Blazor on 5001 |
| Migration fails | Check PostgreSQL is running + password is correct |
| PDF download fails | iText7 package required - run `dotnet restore` |

---

## GPA Formula (Verified)
```
SGPA = Σ(GradePoints × CreditHours) / Σ(CreditHours)

Example — Muhammad Saleh Hayat Semester 4:
  Theory of Automata:  3 CH × 4.00 (A)  = 12.00
  Expository Writing:  3 CH × 4.00 (A)  = 12.00
  Adv. Database Sys:   2 CH × 3.33 (B+) =  6.66
  Applied Physics:     2 CH × 2.67 (B-) =  5.34
  Islamic Studies:     2 CH × 3.67 (A-) =  7.34
  CO & Assembly Lang:  2 CH × 3.00 (B)  =  6.00
  CO & Assembly (Lab): 1 CH × 3.67 (A-) =  3.67
  Adv. DB Systems Lab: 1 CH × 3.00 (B)  =  3.00
  Applied Physics Lab: 1 CH × 3.67 (A-) =  3.67
  
  Total CH = 17
  SGPA = 59.68 / 17 = 3.51 ✓ (matches KICSIT transcript)
```

## Team
| Name | Roll No |
|------|---------|
| M Saleh Hayat | 232201070 |
| Syed Ali Murtajiz Bukhari | 232201086 |
| M Muneeb | 232201093 |

*Submitted to: Sir Uzair Hassan | Advance Programming | CS-6-B | KICSIT | Spring 2026*
