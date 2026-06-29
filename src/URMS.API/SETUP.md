# URMS — Complete Setup & Run Guide
## KICSIT | CS-6-B | Advance Programming | Spring 2026

---

## Prerequisites

Install these before anything else:

| Tool | Download |
|------|----------|
| Visual Studio 2022 (Community) | https://visualstudio.microsoft.com |
| .NET 8 SDK | https://dotnet.microsoft.com/download |
| PostgreSQL 16 | https://www.postgresql.org/download |
| pgAdmin 4 | Included with PostgreSQL installer |
| Git | https://git-scm.com |

During Visual Studio install, select these workloads:
- ASP.NET and web development
- .NET desktop development

---

## Step 1 — Create the Database

Open pgAdmin or psql and run:

```sql
CREATE DATABASE URMS;
```

Then update the connection string in:
`src/URMS.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=URMS;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
  }
}
```

Replace `YOUR_POSTGRES_PASSWORD` with your actual PostgreSQL password.

---

## Step 2 — Run Migrations (Create All Tables)

Open a terminal in the project root folder and run:

```bash
dotnet ef migrations add InitialCreate --project src/URMS.Data --startup-project src/URMS.API
dotnet ef database update --project src/URMS.Data --startup-project src/URMS.API
```

This creates all 14 tables automatically in PostgreSQL.

If you get `dotnet-ef not found`, install it first:
```bash
dotnet tool install --global dotnet-ef
```

---

## Step 3 — Run the API

```bash
cd src/URMS.API
dotnet run
```

API will start at: http://localhost:5000  
Swagger UI at: http://localhost:5000/swagger

Keep this terminal open.

---

## Step 4 — Run the Blazor Frontend

Open a second terminal:

```bash
cd src/URMS.Blazor
dotnet run
```

App opens at: http://localhost:5001

---

## Step 5 — First Launch (Account Setup)

1. Open http://localhost:5001 in your browser
2. You will see **Sign In** and **Create Account** tabs
3. Click **Create Account**
4. Fill in name, email, password → submit
   - This creates the **Examiner** account automatically
5. Click **Create Account** again
   - This creates the **HOD** account automatically
6. The register button **disappears permanently**
7. From now on, only Login is shown

---

## Step 6 — Create Teacher Accounts

1. Log in as **Examiner**
2. Go to **Teachers** page from the sidebar
3. Click **Create Teacher Account**
4. Enter name, email, and a temporary password
5. Share those credentials with the teacher

---

## Step 7 — Add Batches and Students

1. Log in as **Examiner**
2. Dashboard → click **+ Batch** to create a batch (e.g. Batch9-CS-FALL-2023)
3. Go to **Students** page
4. Select the batch → add students one by one, or bulk import via Excel

Excel format for bulk import:
```
Col A: Registration No   (e.g. 232201070)
Col B: Full Name         (e.g. Muhammad Saleh Hayat)
Col C: Father Name       (e.g. Gul Nisar)
```

---

## Step 8 — Configure Subjects (Start of Semester)

1. Log in as **Examiner**
2. On the Dashboard, expand a batch and click a semester
3. Click **+ Add Subject**
4. For each subject enter:
   - Subject Name (e.g. Operating Systems)
   - Subject Code (e.g. OS301)
   - Type: Theory or Laboratory
   - Credit Hours (Theory = 2 or 3, Lab = 1)
   - Assigned Teacher (selected from dropdown)
   - Max Marks (default 100)
5. Repeat for all subjects in the semester
6. Assigned teachers receive a notification automatically

---

## Step 9 — Teachers Upload Grade Sheets

1. Log in as **Teacher**
2. Dashboard shows all assigned subjects
3. For each subject, click **Upload** and select the Excel file

Excel format for grade sheet:
```
Row 1: Header (Registration No | Student Name | Marks Obtained)
Row 2+: Data rows
```

4. On upload:
   - System parses marks, calculates grades and grade points
   - HOD receives a notification to review
   - Subject config is locked (cannot be edited)

---

## Step 10 — HOD Reviews Grade Sheets

1. Log in as **HOD**
2. Dashboard shows all batches and semesters with progress bars
3. Click a semester → see all gradesheets
4. For each gradesheet choose:
   - **Approve** → moves to Examiner dashboard
   - **Reject** → write remarks → teacher is notified to re-upload
   - **Customise** → edit marks directly → save → forwarded to Examiner

Check the **Analytics** page for AI-generated insights on at-risk students,
subject performance, and teacher rankings.

---

## Step 11 — Generate Results (Examiner)

1. Log in as **Examiner**
2. When all gradesheets for a semester are approved by HOD,
   the **Generate** button becomes active (green) on the semester row
3. Click **Generate** → confirm
4. System computes:
   - Credit-hour-weighted SGPA for every student
   - CGPA from all previous semester SGPAs
5. Go to **Results** page → download:
   - **Class Result Sheet** (landscape PDF, all students)
   - **Individual Result Cards** (one page per student)

---

## GPA Formula Reference

```
SGPA = Σ(GradePoints_i × CreditHours_i) / Σ(CreditHours_i)

Example — 3 subjects:
  OOP     (Theory, 3 CH, Grade A  = 4.00) → 4.00 × 3 = 12.00
  DB      (Theory, 3 CH, Grade B+ = 3.33) → 3.33 × 3 =  9.99
  OOP Lab (Lab,    1 CH, Grade A- = 3.67) → 3.67 × 1 =  3.67
  Total credit hours = 7
  SGPA = (12.00 + 9.99 + 3.67) / 7 = 3.666 / 7 ≈ 3.67 (rounded to 2dp)

CGPA = Σ(SGPA of all completed semesters) / N
  Semester 1: CGPA = SGPA1
  Semester 6: CGPA = (SGPA1 + SGPA2 + SGPA3 + SGPA4 + SGPA5 + SGPA6) / 6
```

---

## Grade Scale (KICSIT)

| Marks | Grade | Grade Points |
|-------|-------|-------------|
| 85–100 | A   | 4.00 |
| 80–84  | A-  | 3.67 |
| 75–79  | B+  | 3.33 |
| 71–74  | B   | 3.00 |
| 68–70  | B-  | 2.67 |
| 64–67  | C+  | 2.33 |
| 61–63  | C   | 2.00 |
| 58–60  | C-  | 1.67 |
| 54–57  | D+  | 1.33 |
| 50–53  | D   | 1.00 |
| Below 50 | F | 0.00 |

---

## Academic Status

| CGPA Range | Status |
|------------|--------|
| 3.50 – 4.00 | Excellent |
| 3.00 – 3.49 | Very Good |
| 2.50 – 2.99 | Good |
| 2.00 – 2.49 | Satisfactory |
| 1.50 – 1.99 | Fair |
| 1.00 – 1.49 | Warning |
| Below 1.00  | Extended Temporary Enrollment |

---

## Project Structure

```
URMS/
├── README.md
├── SETUP.md                          ← this file
├── URMS.sln
└── src/
    ├── URMS.Models/                  Entity classes + DTOs
    │   ├── Entities/Entities.cs
    │   └── DTOs/DTOs.cs
    ├── URMS.Data/                    Database (EF Core + PostgreSQL)
    │   └── AppDbContext.cs
    ├── URMS.Services/                Business logic
    │   ├── Interfaces/IServices.cs
    │   └── Implementations/
    │       ├── AuthService.cs
    │       ├── ExaminerService.cs
    │       ├── HODService.cs
    │       ├── TeacherService.cs
    │       ├── GpaCalculationService.cs
    │       ├── NotificationService.cs
    │       ├── ExcelImportService.cs  EPPlus parser
    │       └── PdfService.cs          iText7 generator
    ├── URMS.API/                     REST API (ASP.NET Core)
    │   └── Controllers/
    │       ├── AuthController.cs
    │       ├── ExaminerController.cs
    │       ├── HODController.cs
    │       ├── TeacherController.cs
    │       ├── StudentController.cs
    │       └── ProfileController.cs
    └── URMS.Blazor/                  Frontend (Blazor WebAssembly)
        ├── Pages/
        │   ├── Login.razor
        │   ├── Profile.razor
        │   ├── Examiner/
        │   │   ├── Dashboard.razor   Batch tree + subject config
        │   │   ├── Teachers.razor
        │   │   ├── Students.razor
        │   │   └── Results.razor
        │   ├── HOD/
        │   │   ├── Dashboard.razor   Gradesheet review
        │   │   ├── Analytics.razor   AI analytics
        │   │   └── Customise.razor   Inline mark editor
        │   └── Teacher/
        │       └── Dashboard.razor   Upload grade sheets
        ├── Shared/
        │   ├── MainLayout.razor
        │   └── SvgIcon.razor
        └── Services/
            ├── AuthStateService.cs
            ├── ExaminerApiService.cs
            ├── HODTeacherApiServices.cs
            ├── JwtAuthStateProvider.cs
            └── NotificationStateService.cs
```

---

## Common Issues

**"No connection to database"**
→ Make sure PostgreSQL is running and password in appsettings.json is correct.

**"dotnet-ef not found"**
→ Run: `dotnet tool install --global dotnet-ef`

**"API not found" on Blazor**
→ Make sure API is running on port 5000 before opening the Blazor app.

**"CORS error"**
→ Check that Blazor is running on http://localhost:5001 (matches CORS policy in Program.cs).

**Excel upload not parsing marks**
→ Ensure the Excel file has headers in row 1 and data from row 2.
   Column A = Registration No, Column B = Name, Column C = Marks.

---

## Team

| Name | Roll No | Responsibility |
|------|---------|----------------|
| M Saleh Hayat | 232201070 | Backend API, Database, GPA Engine, Deployment |
| Syed Ali Murtajiz Bukhari | 232201086 | Blazor Frontend, HOD & Examiner UI |
| M Muneeb | 232201093 | Teacher Module, Excel Parser, PDF Generation, Testing |

---

*Submitted to: Sir Uzair Hassan | Subject: Advance Programming | KICSIT*
