using Newtonsoft.Json.Linq;

namespace URMS.Services.Implementations;

public class StudentMasterImportService
{
    public List<StudentMasterDto> ExtractStudentMasterFromJson(string jsonData)
    {
        var students = new List<StudentMasterDto>();

        if (string.IsNullOrEmpty(jsonData))
            return students;

        try
        {
            var jArray = JArray.Parse(jsonData);
            int dataStartRow = FindHeaderRow(jArray);
            if (dataStartRow < 0)
                return students;

            for (int i = dataStartRow; i < jArray.Count; i++)
            {
                var row = jArray[i] as JArray;
                if (row == null || row.Count < 3)
                    continue;

                var student = ParseStudentRow(row);
                if (student != null)
                    students.Add(student);
            }

            return students;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting student master: {ex.Message}");
            return students;
        }
    }

    private StudentMasterDto ParseStudentRow(JArray row)
    {
        try
        {
            string regNo = row[1]?.ToString().Trim() ?? "";
            string studentName = row[2]?.ToString().Trim() ?? "";
            string fatherName = row[3]?.ToString().Trim() ?? "";

            if (string.IsNullOrWhiteSpace(regNo) || regNo.Length < 5)
                return null;

            if (string.IsNullOrWhiteSpace(studentName))
                return null;

            return new StudentMasterDto
            {
                RegistrationNo = regNo,
                FullName = studentName,
                FatherName = fatherName ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    private int FindHeaderRow(JArray jArray)
    {
        for (int i = 0; i < Math.Min(10, jArray.Count); i++)
        {
            var row = jArray[i] as JArray;
            if (row != null && row.Count > 0)
            {
                string firstCell = row[0]?.ToString().ToLower() ?? "";
                string secondCell = row[1]?.ToString().ToLower() ?? "";

                if ((firstCell.Contains("sr") || firstCell == "1") &&
                    (secondCell.Contains("reg") || secondCell.Contains("registration")))
                {
                    return i + 1;
                }
            }
        }
        return -1;
    }
}

public class StudentMasterDto
{
    public string RegistrationNo { get; set; }
    public string FullName { get; set; }
    public string FatherName { get; set; }
}