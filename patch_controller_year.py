import re

with open('Backend/Controllers/PlannerMetasController.cs', 'r') as f:
    content = f.read()

helper_method = """
        private string ParseYear(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            // 1. Try if it's explicitly a DateTime in Excel
            if (cell.DataType == XLDataType.DateTime)
            {
                try
                {
                    return cell.GetDateTime().Year.ToString("0000");
                }
                catch
                {
                    // Fallback to text parsing
                }
            }

            var textValue = cell.GetString()?.Trim();
            if (string.IsNullOrEmpty(textValue)) return null;

            // 2. Parse number string
            if (int.TryParse(textValue, out int yearNumber))
            {
                // JavaScript Date.getYear() bug -> 126 means 2026
                if (yearNumber >= 100 && yearNumber < 200)
                {
                    return (1900 + yearNumber).ToString();
                }
                // Two digit years (e.g. 26 -> 2026)
                if (yearNumber > 0 && yearNumber < 100)
                {
                    return (2000 + yearNumber).ToString();
                }
                // It's already a full year
                if (yearNumber >= 1900 && yearNumber <= 2100)
                {
                    return yearNumber.ToString("0000");
                }
            }

            // 3. Fallback: it might be a date string like "2026-01-01" or "01/01/2026"
            if (DateTime.TryParse(textValue, out DateTime parsedDate))
            {
                return parsedDate.Year.ToString("0000");
            }

            // If all else fails, just return what we have
            return textValue;
        }

        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)
"""

content = content.replace("        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)", helper_method)

with open('Backend/Controllers/PlannerMetasController.cs', 'w') as f:
    f.write(content)
