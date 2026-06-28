using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

/// <summary>
/// Implements credit-hour-weighted SGPA and CGPA calculation.
/// Grades are taken directly from official gradesheets.
/// </summary>
public class GpaCalculationService : IGpaCalculationService
{
    // ===========================
    // Grade → GPA Points
    // ===========================
    public decimal GradeToPoints(string grade) =>
        grade.ToUpper().Trim() switch
        {
            "A" => 4.00m,
            "A-" => 3.70m,
            "B+" => 3.30m,
            "B" => 3.00m,
            "B-" => 2.70m,
            "C+" => 2.30m,
            "C" => 2.00m,
            "C-" => 1.70m,
            "D+" => 1.30m,
            "D" => 1.00m,
            "F" => 0.00m,
            "W/C" => 0.00m,
            _ => 0.00m
        };

    // ===========================
    // SGPA Calculation
    // ===========================
    /// <summary>
    /// SGPA = Σ(GradePoints × CreditHours) / Σ(CreditHours)
    /// </summary>
    public decimal CalculateSGPA(
        List<(decimal gradePoints, int creditHours)> subjectResults)
    {
        if (subjectResults == null || subjectResults.Count == 0)
            return 0m;

        decimal weightedSum =
            subjectResults.Sum(s => s.gradePoints * s.creditHours);

        int totalCreditHours =
            subjectResults.Sum(s => s.creditHours);

        if (totalCreditHours == 0)
            return 0m;

        return Math.Round(weightedSum / totalCreditHours, 2);
    }

    // ===========================
    // CGPA Calculation
    // ===========================
    /// <summary>
    /// CGPA = Average of all semester SGPAs
    /// </summary>
    public decimal CalculateCGPA(List<decimal> semesterSGPAs)
    {
        if (semesterSGPAs == null || semesterSGPAs.Count == 0)
            return 0m;

        return Math.Round(semesterSGPAs.Average(), 2);
    }
}