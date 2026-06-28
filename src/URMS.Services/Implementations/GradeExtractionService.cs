using Newtonsoft.Json.Linq;
using URMS.Models.Entities;
using URMS.Services.Interfaces;

namespace URMS.Services.Implementations;

public class GradeExtractionService
{
    private readonly IGpaCalculationService _gpa;

    public GradeExtractionService(IGpaCalculationService gpa)
    {
        _gpa = gpa;
    }

    public List<(string RegNo, string Grade, decimal GradePoints, decimal Marks)>
        ExtractGradesFromJson(string? jsonData)
    {
        var results = new List<(string, string, decimal, decimal)>();
        if (string.IsNullOrEmpty(jsonData)) return results;

        // ── Guard: must be a JSON array, not plain text ──────────────────────
        jsonData = jsonData.Trim();
        if (!jsonData.StartsWith("["))
        {
            Console.WriteLine($"[EXTRACT] CustomisedData is not a JSON array " +
                $"(starts with: '{jsonData[..Math.Min(60, jsonData.Length)]}') " +
                $"— skipping, will fall back to Excel");
            return results;
        }

        try
        {
            var jArray = JArray.Parse(jsonData);

            // Validate it's actually array-of-arrays, not just a flat array
            if (jArray.Count == 0 || jArray[0] is not JArray)
            {
                Console.WriteLine("[EXTRACT] CustomisedData is not array-of-arrays — skipping");
                return results;
            }

            int totalRows = jArray.Count;
            int lastCol = 0;

            foreach (var r in jArray.Take(10))
            {
                if (r is JArray jr && jr.Count > lastCol)
                    lastCol = jr.Count;
            }

            // Find header row containing "Reg. No." or "Reg No"
            int dataStartIdx = -1;
            for (int i = 0; i < Math.Min(15, totalRows); i++)
            {
                var row = jArray[i] as JArray;
                if (row == null) continue;
                for (int c = 0; c < row.Count; c++)
                {
                    var cell = row[c]?.ToString().ToLower().Trim() ?? "";
                    if (cell.Contains("reg") && (cell.Contains("no") || cell.Contains(".")))
                    {
                        dataStartIdx = i + 2;
                        break;
                    }
                }
                if (dataStartIdx > 0) break;
            }

            if (dataStartIdx < 0)
            {
                Console.WriteLine("[EXTRACT] Header row not found, using default index 6");
                dataStartIdx = 6;
            }

            Console.WriteLine($"[EXTRACT] Data starts at row index {dataStartIdx}, total rows {totalRows}");

            for (int i = dataStartIdx; i < totalRows; i++)
            {
                var row = jArray[i] as JArray;
                if (row == null || row.Count < 3) continue;

                var regNo = row[1]?.ToString().Trim() ?? "";

                if (!System.Text.RegularExpressions.Regex.IsMatch(regNo, @"^\d{7,}$"))
                    continue;

                var gradeText = row[row.Count - 1]?.ToString().Trim().ToUpper() ?? "";
                var totalText = row[row.Count - 2]?.ToString().Trim() ?? "0";

                if (string.IsNullOrWhiteSpace(gradeText) || gradeText.Length > 3)
                {
                    Console.WriteLine($"[EXTRACT] Skipping row {i}: invalid grade '{gradeText}' for {regNo}");
                    continue;
                }

                decimal.TryParse(totalText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal marks);

                decimal gradePoints = _gpa.GradeToPoints(gradeText);

                Console.WriteLine($"[EXTRACT] RegNo={regNo}, Grade={gradeText}, GradePoints={gradePoints}, Marks={marks}");
                results.Add((regNo, gradeText, gradePoints, marks));
            }

            Console.WriteLine($"[EXTRACT] Total extracted: {results.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXTRACT ERROR] {ex.Message}");
        }

        return results;
    }

    public List<StudentMark> ExtractGradesFromCustomisedData(
        string jsonData, int subjectConfigId, int gradesheetId)
        => new();
}