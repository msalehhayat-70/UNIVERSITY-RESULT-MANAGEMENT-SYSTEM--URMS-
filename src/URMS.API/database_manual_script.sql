-- =====================================================================
-- URMS Initial Database Script
-- Generated for PostgreSQL 16
-- Run this ONLY if EF Core migrations fail
-- Otherwise use: dotnet ef database update
-- =====================================================================

-- Drop and recreate (dev only)
-- DROP SCHEMA public CASCADE; CREATE SCHEMA public;

-- SystemConfig
CREATE TABLE IF NOT EXISTS "SystemConfigs" (
    "Id"          SERIAL PRIMARY KEY,
    "ConfigKey"   TEXT NOT NULL UNIQUE,
    "ConfigValue" TEXT NOT NULL DEFAULT '0'
);
INSERT INTO "SystemConfigs"("ConfigKey","ConfigValue") VALUES('registered_accounts','0')
    ON CONFLICT("ConfigKey") DO NOTHING;

-- Users
CREATE TABLE IF NOT EXISTS "Users" (
    "UserId"       SERIAL PRIMARY KEY,
    "FullName"     TEXT NOT NULL,
    "Email"        TEXT NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Role"         TEXT NOT NULL,
    "CreatedById"  INTEGER REFERENCES "Users"("UserId") ON DELETE SET NULL,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "IsActive"     BOOLEAN NOT NULL DEFAULT TRUE
);

-- Programs
CREATE TABLE IF NOT EXISTS "Programs" (
    "ProgramId"   SERIAL PRIMARY KEY,
    "ProgramName" TEXT NOT NULL,
    "ProgramCode" TEXT NOT NULL
);
INSERT INTO "Programs"("ProgramName","ProgramCode") VALUES
    ('BS Computer Science','CS'),
    ('BS Computer Engineering','CE')
    ON CONFLICT DO NOTHING;

-- Batches
CREATE TABLE IF NOT EXISTS "Batches" (
    "BatchId"      SERIAL PRIMARY KEY,
    "BatchName"    TEXT NOT NULL,
    "ProgramId"    INTEGER NOT NULL REFERENCES "Programs"("ProgramId"),
    "SemesterType" TEXT NOT NULL,
    "Year"         INTEGER NOT NULL,
    "BatchNumber"  INTEGER NOT NULL,
    "IsActive"     BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- BatchSemesters
CREATE TABLE IF NOT EXISTS "BatchSemesters" (
    "BatchSemesterId"   SERIAL PRIMARY KEY,
    "BatchId"           INTEGER NOT NULL REFERENCES "Batches"("BatchId") ON DELETE CASCADE,
    "SemesterNo"        INTEGER NOT NULL,
    "IsConfigured"      BOOLEAN NOT NULL DEFAULT FALSE,
    "IsResultGenerated" BOOLEAN NOT NULL DEFAULT FALSE,
    "ResultGeneratedAt" TIMESTAMPTZ,
    UNIQUE("BatchId","SemesterNo")
);

-- SubjectConfigurations
CREATE TABLE IF NOT EXISTS "SubjectConfigurations" (
    "ConfigId"        SERIAL PRIMARY KEY,
    "BatchSemesterId" INTEGER NOT NULL REFERENCES "BatchSemesters"("BatchSemesterId") ON DELETE CASCADE,
    "SubjectName"     TEXT NOT NULL,
    "SubjectCode"     TEXT NOT NULL,
    "CreditHours"     INTEGER NOT NULL,
    "SubjectType"     TEXT NOT NULL,
    "TeacherId"       INTEGER NOT NULL REFERENCES "Users"("UserId"),
    "MaxMarks"        INTEGER NOT NULL DEFAULT 100,
    "IsLocked"        BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt"       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Students
CREATE TABLE IF NOT EXISTS "Students" (
    "StudentId"       SERIAL PRIMARY KEY,
    "RegistrationNo"  TEXT NOT NULL UNIQUE,
    "FullName"        TEXT NOT NULL,
    "FatherName"      TEXT NOT NULL DEFAULT '',
    "BatchId"         INTEGER NOT NULL REFERENCES "Batches"("BatchId"),
    "CurrentSemester" INTEGER NOT NULL DEFAULT 1,
    "IsActive"        BOOLEAN NOT NULL DEFAULT TRUE
);

-- Gradesheets
CREATE TABLE IF NOT EXISTS "Gradesheets" (
    "GradesheetId"    SERIAL PRIMARY KEY,
    "SubjectConfigId" INTEGER NOT NULL UNIQUE REFERENCES "SubjectConfigurations"("ConfigId") ON DELETE CASCADE,
    "TeacherId"       INTEGER NOT NULL REFERENCES "Users"("UserId"),
    "FilePath"        TEXT NOT NULL DEFAULT '',
    "Status"          TEXT NOT NULL DEFAULT 'Pending',
    "HodRemarks"      TEXT,
    "ReviewedById"    INTEGER REFERENCES "Users"("UserId") ON DELETE SET NULL,
    "UploadedAt"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ReviewedAt"      TIMESTAMPTZ
);

-- StudentMarks
CREATE TABLE IF NOT EXISTS "StudentMarks" (
    "MarkId"          SERIAL PRIMARY KEY,
    "StudentId"       INTEGER NOT NULL REFERENCES "Students"("StudentId") ON DELETE CASCADE,
    "GradesheetId"    INTEGER NOT NULL REFERENCES "Gradesheets"("GradesheetId") ON DELETE CASCADE,
    "SubjectConfigId" INTEGER NOT NULL REFERENCES "SubjectConfigurations"("ConfigId"),
    "MarksObtained"   NUMERIC(5,2) NOT NULL,
    "Grade"           TEXT NOT NULL,
    "GradePoints"     NUMERIC(3,1) NOT NULL
);

-- SGPAResults
CREATE TABLE IF NOT EXISTS "SGPAResults" (
    "SGPAId"          SERIAL PRIMARY KEY,
    "StudentId"       INTEGER NOT NULL REFERENCES "Students"("StudentId") ON DELETE CASCADE,
    "SemesterNo"      INTEGER NOT NULL,
    "SGPA"            NUMERIC(4,2) NOT NULL,
    "TotalCreditHours"INTEGER NOT NULL DEFAULT 0,
    "CalculatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE("StudentId","SemesterNo")
);

-- CGPAResults
CREATE TABLE IF NOT EXISTS "CGPAResults" (
    "CGPAId"        SERIAL PRIMARY KEY,
    "StudentId"     INTEGER NOT NULL REFERENCES "Students"("StudentId") ON DELETE CASCADE,
    "UpToSemester"  INTEGER NOT NULL,
    "CGPA"          NUMERIC(4,2) NOT NULL,
    "CalculatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE("StudentId","UpToSemester")
);

-- Notifications
CREATE TABLE IF NOT EXISTS "Notifications" (
    "NotifId"   SERIAL PRIMARY KEY,
    "UserId"    INTEGER NOT NULL REFERENCES "Users"("UserId") ON DELETE CASCADE,
    "Title"     TEXT NOT NULL,
    "Message"   TEXT NOT NULL,
    "IsRead"    BOOLEAN NOT NULL DEFAULT FALSE,
    "NotifType" TEXT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- AIAlerts
CREATE TABLE IF NOT EXISTS "AIAlerts" (
    "AlertId"      SERIAL PRIMARY KEY,
    "GradesheetId" INTEGER NOT NULL REFERENCES "Gradesheets"("GradesheetId") ON DELETE CASCADE,
    "AlertType"    TEXT NOT NULL,
    "Message"      TEXT NOT NULL,
    "Severity"     TEXT NOT NULL DEFAULT 'Warning',
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_students_batch   ON "Students"("BatchId");
CREATE INDEX IF NOT EXISTS idx_marks_student    ON "StudentMarks"("StudentId");
CREATE INDEX IF NOT EXISTS idx_marks_gradesheet ON "StudentMarks"("GradesheetId");
CREATE INDEX IF NOT EXISTS idx_notif_user       ON "Notifications"("UserId","IsRead");
CREATE INDEX IF NOT EXISTS idx_sgpa_student     ON "SGPAResults"("StudentId");
CREATE INDEX IF NOT EXISTS idx_cgpa_student     ON "CGPAResults"("StudentId");

-- Done
SELECT 'URMS database tables created successfully.' AS result;
