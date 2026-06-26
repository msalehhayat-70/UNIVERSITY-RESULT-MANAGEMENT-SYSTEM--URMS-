# 🎓 University Result Management System (URMS)

<div align="center">


![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-7B2FBE?style=for-the-badge&logo=blazor)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=for-the-badge&logo=postgresql)
![License](https://img.shields.io/badge/License-Academic-D4AC0D?style=for-the-badge)

A fully automated, role-based university result management system 

*Eliminates manual result compilation errors · Reduces result time from weeks to hours · Generates professional PDF result cards*

</div>



## 📋 Table of Contents

1. [Problem Statement](#-problem-statement)
2. [Solution Overview](#-solution-overview)
3. [Features](#-features)
4. [System Architecture](#-system-architecture)
5. [Tech Stack](#-tech-stack)
6. [Database Design](#-database-design)
7. [User Roles](#-user-roles)
8. [GPA Calculation](#-gpa-calculation)
9. [Installation & Setup](#-installation--setup)
10. [Running the Project](#-running-the-project)
11. [Usage Guide](#-usage-guide)
12. [API Reference](#-api-reference)
13. [Project Structure](#-project-structure)
14. [Screenshots](#-screenshots)
15. [Future Enhancements](#-future-enhancements)

---

## ❗ Problem Statement

The result generation process at KICSIT was carried out **entirely by hand**, causing:

- ⏳ **Weeks or months of delay** after examinations conclude
- ❌ **Human errors** in grade calculation — results had to be recalled and reissued
- 📂 **No centralised workflow** — grade sheets passed through no systematic approval chain
- ❌ **No credit-hour weighting** — all subjects treated equally regardless of academic weight
- 📊 **No analytics** — no visibility into student performance trends or at-risk students

> *"In a recent incident, results for an entire batch were published with errors. Students identified discrepancies after publication, forcing examiners to rebuild and reissue the complete result set — damaging institutional credibility."*

---

## ✅ Solution Overview

URMS replaces the manual process with a **fully automated, role-based pipeline**:

```
Examiner configures subjects (with credit hours)
        ↓
Teacher uploads grade sheet Excel
        ↓
System parses grades, runs anomaly detection
        ↓
HOD reviews, approves / rejects / customises
        ↓
Examiner generates result with one click
        ↓
System calculates credit-hour-weighted SGPA & CGPA
        ↓
Professional PDF result cards generated instantly
```

**Result:** What took weeks now takes **hours**. What had errors now has **zero manual calculation**.

---

## ✨ Features

### 🔐 Security
- One-time registration — first 2 accounts are Examiner and HOD, then registration closes permanently
- JWT-based authentication with role claims
- BCrypt password hashing
- Role-based route protection — each user sees only their own dashboard

### 👨‍💼 Examiner Module
- Create and manage batches (e.g. Batch9-CS-FALL-2023)
- Register subjects with credit hours and teacher assignments per semester
- View gradesheet approval progress per batch/semester
- **Generate results** with one click when all gradesheets are approved
- Download **Class Result Sheet** (landscape PDF, all students)
- Download **Individual Result Cards** (one A4 PDF per student)
- Download all result cards as a **ZIP archive**
- Manage student roster (bulk Excel import supported)

### 👩‍💼 HOD Module
- Review all uploaded gradesheets per batch/semester
- **Approve** — forwards to Examiner
- **Reject** — sends remarks notification to teacher
- **Customise** — edit marks inline, then forward
- View AI anomaly alerts on suspicious gradesheets
- Analytics dashboard with subject performance, teacher rankings, at-risk students

### 👨‍🏫 Teacher Module
- View all assigned subjects (set by Examiner)
- Upload grade sheet Excel per subject
- Re-upload after HOD rejection
- Real-time status: Pending / Approved / Rejected

### 📊 Analytics & AI
- **At-risk student detection** — flags students with declining SGPA or CGPA < 1.5
- **Gradesheet anomaly detection** — alerts when gap between average and top mark > 35, identical marks for 5+ students, or failure rate > 40%
- **AI-generated result summary** paragraph
- **Teacher performance ranking** by pass rate
- **Subject difficulty analysis**

### 📄 PDF Generation (iText7)
- Matches **exact KICSIT result format** from reference documents
- Individual student card: header, subject table, SGPA/CGPA, class position, academic status, signature space
- Class result sheet: landscape, all students, all subjects as columns, SGPA/CGPA to 9dp then 2dp, academic status
- 50 students per page with automatic page breaks

### 🔔 Notification System
- Real-time bell notifications for all roles
- Auto-polling every 45 seconds (JavaScript interop)
- Mark all read / mark one read
- Notification types: gradesheet uploaded, approved, rejected, result generated

---

## 🏗 System Architecture

URMS follows the **MVC (Model-View-Controller)** pattern:

```
┌─────────────────────────────────────────────────────────┐
│                    USER (Browser)                        │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│              VIEW — Blazor WebAssembly (.razor)          │
│   Login │ Examiner │ HOD │ Teacher │ Profile dashboards  │
└──────────────────────────┬──────────────────────────────┘
                           │ HTTP / JSON
┌──────────────────────────▼──────────────────────────────┐
│           CONTROLLER — ASP.NET Core Web API              │
│  Auth │ Examiner │ HOD │ Teacher │ Student │ Analytics   │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│                  SERVICE LAYER                           │
│  GpaCalculation │ ExcelImport │ PdfService │ Analytics   │
└──────────────────────────┬──────────────────────────────┘
                           │ Entity Framework Core
┌──────────────────────────▼──────────────────────────────┐
│              MODEL — PostgreSQL Database                 │
│  Students │ Batches │ Subjects │ Gradesheets │ Results   │
└─────────────────────────────────────────────────────────┘
```

---

## 🛠 Tech Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| Frontend | Blazor WebAssembly | .NET 8 | Role-based dashboards, real-time UI |
| Backend | ASP.NET Core Web API | .NET 8 | REST endpoints, business logic |
| Database | PostgreSQL | 16 | Permanent relational data store |
| ORM | Entity Framework Core | 8.0 | Database access, migrations |
| PDF | iText7 | 8.0.3 | KICSIT-format result card generation |
| Excel | EPPlus | 7.0.4 | Grade sheet parsing |
| Auth | JWT + BCrypt | — | Secure login, password hashing |
| IDE | Visual Studio 2022 | — | Development environment |
| Version Control | Git / GitHub | — | Collaboration |

---

## 🗄 Database Design

```
SystemConfig          → registered_accounts counter (security lock)
Users                 → Examiner, HOD, Teacher accounts
Programs              → BS Computer Science, BS Computer Engineering
Batches               → Batch9-CS-FALL-2023, Batch10-CS-SPRING-2024...
BatchSemesters        → Semester 1–8 per batch
SubjectConfigurations → Subject name, code, credit hours, teacher, max marks
Students              → RegNo, Name, FatherName, BatchId, CurrentSemester
Gradesheets           → Upload record, status, HOD remarks
StudentMarks          → Marks, grade, grade points per student per subject
SGPAResults           → Stored SGPA per student per semester (9dp precision)
CGPAResults           → Stored CGPA at each semester milestone
Notifications         → In-app notifications for all roles
AIAlerts              → Anomaly detection results
```

### Key Relationships
```
Batch ──< BatchSemester ──< SubjectConfiguration ──< Gradesheet ──< StudentMark
                                                                          │
Student ──────────────────────────────────────────────────────────────────┘
Student ──< SGPAResult (one per semester)
Student ──< CGPAResult (one per milestone)
```

---

## 👤 User Roles

### Registration Flow
```
First launch → Register button visible
Account 1 created → Examiner (automatic)
Account 2 created → HOD (automatic)
Register button disappears PERMANENTLY
From now on, only Examiner or HOD can create Teacher accounts
```

### Role Permissions

| Action | Examiner | HOD | Teacher |
|--------|----------|-----|---------|
| Create batches | ✅ | ❌ | ❌ |
| Register subjects with credit hours | ✅ | ❌ | ❌ |
| Create teacher accounts | ✅ | ✅ | ❌ |
| Manage students | ✅ | ❌ | ❌ |
| Upload grade sheets | ❌ | ❌ | ✅ |
| Approve/reject grade sheets | ❌ | ✅ | ❌ |
| Customise grade sheets | ❌ | ✅ | ❌ |
| Generate results | ✅ | ❌ | ❌ |
| Download PDFs | ✅ | ❌ | ❌ |
| View analytics | ✅ | ✅ | ❌ |

---

## 📐 GPA Calculation

### Grade Scale (KICSIT Standard)

| Marks Range | Letter Grade | Grade Points |
|-------------|-------------|-------------|
| 85 – 100 | A | 4.00 |
| 80 – 84 | A- | 3.67 |
| 75 – 79 | B+ | 3.33 |
| 71 – 74 | B | 3.00 |
| 68 – 70 | B- | 2.67 |
| 64 – 67 | C+ | 2.33 |
| 61 – 63 | C | 2.00 |
| 58 – 60 | C- | 1.67 |
| 54 – 57 | D+ | 1.33 |
| 50 – 53 | D | 1.00 |
| Below 50 | F | 0.00 |

### SGPA Formula (Credit-Hour Weighted)
```
SGPA = Σ(GradePoints_i × CreditHours_i) / Σ(CreditHours_i)
```

**Example — Muhammad Saleh Hayat, Semester 4:**
```
Theory of Automata    → 3 CH × 4.00 (A)  = 12.00
Expository Writing    → 3 CH × 4.00 (A)  =  12.00
Advanced Database Sys → 2 CH × 3.33 (B+) =   6.66
Applied Physics       → 2 CH × 2.67 (B-) =   5.34
Islamic Studies       → 2 CH × 3.67 (A-) =   7.34
CO & Assembly Lang    → 2 CH × 3.00 (B)  =   6.00
CO & Assembly (Lab)   → 1 CH × 3.67 (A-) =   3.67
Adv. DB Systems Lab   → 1 CH × 3.00 (B)  =   3.00
Applied Physics Lab   → 1 CH × 3.67 (A-) =   3.67

Total Credit Hours = 17
SGPA = 59.68 / 17 = 3.51 ✓ (matches KICSIT transcript exactly)
```

### CGPA Formula
```
Semester 1: CGPA = SGPA₁
Semester N: CGPA = (SGPA₁ + SGPA₂ + ... + SGPAₙ) / N
```

### Academic Status

| CGPA Range | Status |
|-----------|--------|
| 3.50 – 4.00 | Excellent |
| 3.00 – 3.49 | Very Good |
| 2.50 – 2.99 | Good |
| 2.00 – 2.49 | Satisfactory |
| 1.50 – 1.99 | Fair |
| 1.00 – 1.49 | Warning |
| Below 1.00 | Extended Temporary Enrollment |

---

## ⚙ Installation & Setup

### Prerequisites

| Requirement | Download |
|-------------|---------|
| Visual Studio 2022 (Community) | https://visualstudio.microsoft.com |
| .NET 8 SDK | https://dotnet.microsoft.com/download |
| PostgreSQL 16 | https://www.postgresql.org/download/windows |
| pgAdmin 4 | Included with PostgreSQL installer |

> During Visual Studio install select workloads:
> - ✅ ASP.NET and web development
> - ✅ .NET desktop development

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/URMS.git
cd URMS
```

### 2. Configure Database

Open `src/URMS.API/appsettings.json` and update:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=URMS;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "URMS_SuperSecretKey_2026_KICSIT_ChangeThisInProduction!",
    "Issuer": "URMS.API",
    "Audience": "URMS.Blazor"
  }
}
```

### 3. Create Database Tables

```powershell
# Install EF Core tools (once only)
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update --project src/URMS.Data --startup-project src/URMS.API
```

> **Alternative:** If migration fails, open pgAdmin → URMS database → Query Tool → paste `database_manual_script.sql` → Run

---

## 🚀 Running the Project

### Terminal 1 — Start API
```powershell
cd src/URMS.API
dotnet run
```
```
✓ Database migrated.
URMS API → http://localhost:5000
Swagger  → http://localhost:5000/swagger
```

### Terminal 2 — Start Frontend
```powershell
cd src/URMS.Blazor
dotnet run
```
```
Now listening on: http://localhost:5001
```

Open browser → **http://localhost:5001**

---

## 📖 Usage Guide

### First Launch

```
1. Open http://localhost:5001
2. Click "Create Account" tab
3. Fill name / email / password → Submit
   → EXAMINER account created automatically
4. Click "Create Account" again
5. Fill name / email / password → Submit
   → HOD account created automatically
6. Registration button disappears permanently
```

### Complete Result Generation Workflow

```
STEP 1: Examiner → Dashboard → "+ Batch"
        Create batch (e.g. Batch9-CS-FALL-2023)

STEP 2: Examiner → Students
        Add students (RegNo, Full Name, Father Name)
        OR bulk import via Excel

STEP 3: Examiner → Dashboard → Select Semester → "+ Add Subject"
        Add each subject:
          - Subject Name, Code
          - Credit Hours (Theory: 2-3, Lab: 1)
          - Assigned Teacher
          - Max Marks

STEP 4: Examiner → Teachers
        Create teacher account → share credentials

STEP 5: Teacher logs in → Dashboard
        Upload Excel grade sheet for each subject

STEP 6: HOD logs in → Dashboard
        Review each gradesheet → Approve / Reject / Customise

STEP 7: Examiner → Results
        Generate button turns gold when all approved
        Click Generate → results calculated instantly

STEP 8: Download:
        [Class PDF]  → All students, landscape format
        [All Cards ZIP] → Individual result card per student
```

### Excel Formats

**Grade Sheet (uploaded by Teacher):**
```
Row 1: Header row
Col A: Registration No
Col B: Student Name
Last Col: Grade (A, A-, B+, B, B-, C+, C, C-, D+, D, F)
```

**Student Import (uploaded by Examiner):**
```
Row 1: Header row
Col A: Registration No
Col B: Full Name
Col C: Father Name
```

---

## 🔌 API Reference

Base URL: `http://localhost:5000`
Interactive Docs: `http://localhost:5000/swagger`

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/auth/registration-status` | Check if registration is open |
| POST | `/api/auth/register` | Create first 2 accounts |
| POST | `/api/auth/login` | Login, returns JWT token |
| POST | `/api/auth/create-teacher` | Examiner/HOD creates teacher account |

### Examiner
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/examiner/dashboard` | Dashboard stats + batch tree |
| GET/POST | `/api/examiner/batches` | List / create batches |
| GET/POST/PUT/DELETE | `/api/examiner/subjects` | Manage subject configurations |
| GET | `/api/examiner/teachers` | List all teachers |
| POST | `/api/examiner/generate-result/{id}` | Generate semester results |
| GET | `/api/examiner/result/{id}/class-pdf` | Download class result PDF |
| GET | `/api/examiner/result/{id}/all-cards-zip` | Download all result cards ZIP |
| GET | `/api/examiner/result/{id}/student/{sid}/pdf` | Individual student card |

### HOD
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/hod/dashboard` | Dashboard with pending count |
| GET | `/api/hod/semesters/{id}/gradesheets` | List gradesheets for review |
| POST | `/api/hod/gradesheets/{id}/approve` | Approve gradesheet |
| POST | `/api/hod/gradesheets/{id}/reject` | Reject with remarks |
| POST | `/api/hod/gradesheets/{id}/customise` | Edit marks and forward |

### Teacher
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teacher/dashboard` | Assigned subjects + status |
| POST | `/api/teacher/upload-gradesheet/{id}` | Upload Excel grade sheet |
| POST | `/api/teacher/reupload-gradesheet/{id}` | Re-upload after rejection |

### Students
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/students/batch/{id}` | Get students for a batch |
| POST | `/api/students` | Add single student |
| POST | `/api/students/import` | Bulk import students |

---

## 📁 Project Structure

```
URMS/
├── README.md                        ← This file
├── SETUP.md                         ← Detailed setup guide
├── database_manual_script.sql       ← Fallback SQL script
├── URMS.sln                         ← Open in Visual Studio 2022
│
└── src/
    ├── URMS.Models/                 ← M in MVC
    │   ├── Entities/Entities.cs     ← Database entity classes
    │   └── DTOs/                    ← Data Transfer Objects
    │       ├── DTOs.cs
    │       └── HODTeacherDTOs.cs
    │
    ├── URMS.Data/                   ← Database Layer
    │   ├── AppDbContext.cs          ← EF Core DbContext (13 tables)
    │   └── Migrations/              ← Auto-generated migration files
    │
    ├── URMS.Services/               ← Business Logic Layer
    │   ├── Interfaces/IServices.cs  ← Service contracts
    │   └── Implementations/
    │       ├── AuthService.cs       ← Login, register, JWT generation
    │       ├── ExaminerService.cs   ← Batch, subject, result generation
    │       ├── HODService.cs        ← Approve/reject/customise
    │       ├── TeacherService.cs    ← Upload, re-upload gradesheets
    │       ├── GpaCalculationService.cs ← SGPA/CGPA formulas
    │       ├── ExcelImportService.cs    ← EPPlus grade sheet parser
    │       ├── PdfService.cs            ← iText7 PDF generation
    │       ├── AnalyticsService.cs      ← At-risk detection, AI summary
    │       └── NotificationService.cs   ← In-app notifications
    │
    ├── URMS.API/                    ← C in MVC — REST API
    │   ├── Program.cs               ← DI registration, JWT, CORS, Swagger
    │   ├── appsettings.json         ← DB connection string, JWT config
    │   └── Controllers/
    │       ├── AuthController.cs
    │       ├── ExaminerController.cs
    │       ├── HODController.cs
    │       ├── TeacherController.cs
    │       ├── StudentController.cs
    │       ├── ProfileController.cs
    │       └── AnalyticsController.cs
    │
    └── URMS.Blazor/                 ← V in MVC — Frontend
        ├── Program.cs               ← Service registration
        ├── App.razor                ← Root component
        ├── _Imports.razor           ← Global using statements
        ├── Pages/
        │   ├── Login.razor          ← Auth page
        │   ├── Profile.razor        ← Update name/password
        │   ├── Examiner/
        │   │   ├── Dashboard.razor  ← Batch tree + subject config
        │   │   ├── Students.razor   ← Student management
        │   │   ├── Teachers.razor   ← Teacher accounts
        │   │   └── Results.razor    ← Generate + download PDFs
        │   ├── HOD/
        │   │   ├── Dashboard.razor  ← Gradesheet review table
        │   │   ├── Analytics.razor  ← AI analytics dashboard
        │   │   └── Customise.razor  ← Inline mark editor
        │   └── Teacher/
        │       └── Dashboard.razor  ← Upload grade sheets
        ├── Shared/
        │   ├── MainLayout.razor     ← Sidebar + topbar + notifications
        │   └── SvgIcon.razor        ← Reusable icon component
        ├── Services/
        │   ├── AuthStateService.cs
        │   ├── ExaminerApiService.cs
        │   ├── HODTeacherApiServices.cs
        │   ├── JsInteropService.cs  ← File download + polling
        │   └── NotificationStateService.cs
        └── wwwroot/
            ├── css/app.css          ← URMS design system (navy/gold)
            ├── js/urms.js           ← downloadFile() + polling
            └── index.html
```

---

## 🔮 Future Enhancements

- [ ] **Student Portal** — students log in to view their own result cards
- [ ] **SMS/Email notifications** — notify students when results are published
- [ ] **ML.NET at-risk prediction** — trained model instead of rule-based detection
- [ ] **Mobile app** — React Native or MAUI for teachers on the go
- [ ] **Multi-department** — CE, CS, IT departments with separate HODs
- [ ] **Result verification QR code** — scan to verify authenticity of printed result
- [ ] **Transcript generation** — full 8-semester academic transcript PDF
- [ ] **Gradesheet OCR** — auto-read scanned paper grade sheets

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|---------|
| `Connection refused 5432` | Start PostgreSQL service: `net start postgresql-x64-16` |
| `dotnet-ef not found` | Run: `dotnet tool install --global dotnet-ef` |
| `CORS error` | Ensure API runs on port 5000, Blazor on 5001 |
| Result shows 0.00 SGPA | Students not added before gradesheet upload — add students first, reset gradesheets, re-upload |
| Father name empty in PDF | Update `FatherName` in Students page or via pgAdmin |
| Duplicate students in result | Students imported into wrong batch — fix BatchId in pgAdmin |
| PDF download fails | Run `dotnet restore` to install iText7 package |

---


</div>
