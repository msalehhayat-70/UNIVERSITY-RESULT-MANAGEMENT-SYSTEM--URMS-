# University Result Management System (URMS)
## KICSIT — CS-6-B — Advance Programming — Spring 2026

### Team Members
| Name | Roll No |
|------|---------|
| M Saleh Hayat | 232201070 |
| Syed Ali Murtajiz Bukhari | 232201086 |
| M Muneeb | 232201093 |

### Tech Stack
- **Backend:** ASP.NET Core Web API (C#) — Visual Studio 2022
- **Frontend:** Blazor WebAssembly
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core 8
- **PDF:** PdfGenerationService.py (iText7 in C# production)
- **Auth:** JWT + BCrypt

### Project Structure
```
URMS/
├── URMS.sln
└── src/
    ├── URMS.Models/         — Entities + DTOs
    ├── URMS.Data/           — AppDbContext (PostgreSQL)
    ├── URMS.Services/       — Business logic + GPA + PDF
    ├── URMS.API/            — REST API Controllers
    └── URMS.Blazor/         — Frontend UI
```

### Setup Instructions

#### 1. Database (PostgreSQL)
```sql
CREATE DATABASE URMS;
```
Update `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=URMS;Username=postgres;Password=YOUR_PASSWORD"
}
```

#### 2. Run Migrations
```bash
cd src/URMS.API
dotnet ef database update
```

#### 3. Run API
```bash
dotnet run --project src/URMS.API
# Swagger: http://localhost:5000/swagger
```

#### 4. Run Blazor Frontend
```bash
dotnet run --project src/URMS.Blazor
```

### Security — First Launch
1. Open the app — Register button is visible (registered_accounts = 0)
2. First registration → **Examiner** account created automatically
3. Second registration → **HOD** account created automatically
4. Register button disappears permanently — only Login shown from now
5. Examiner/HOD create Teacher accounts from their dashboards

### GPA Formula
```
SGPA = Σ(GradePoints_i × CreditHours_i) / Σ(CreditHours_i)
CGPA = Σ(all semester SGPAs) / N semesters
```
- Theory subject (3 CH) has 3× impact vs Lab (1 CH)
- Semester 1: CGPA = SGPA (no previous semesters)
- Both displayed to 9dp, rounded to 2dp on result card

### Grade Scale (KICSIT)
| Marks | Grade | Points |
|-------|-------|--------|
| 85-100 | A | 4.00 |
| 80-84 | A- | 3.67 |
| 75-79 | B+ | 3.33 |
| 71-74 | B | 3.00 |
| 68-70 | B- | 2.67 |
| 64-67 | C+ | 2.33 |
| 61-63 | C | 2.00 |
| 58-60 | C- | 1.67 |
| 54-57 | D+ | 1.33 |
| 50-53 | D | 1.00 |
| <50 | F | 0.00 |

### Modules Delivered (Phase 1)
- [x] Examiner Dashboard (batch tree, subject config, result generation)
- [x] Teacher Management
- [x] Auth with SystemConfig registration lock
- [x] PDF Generation (Student Card + Class Sheet)
- [ ] HOD Module (Phase 2)
- [ ] Teacher Module (Phase 2)
- [ ] AI Analytics (Phase 2)
