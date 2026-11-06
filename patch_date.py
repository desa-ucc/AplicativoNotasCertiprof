import re

with open('Backend/Controllers/PlannerMetasController.cs', 'r') as f:
    content = f.read()


helper_method = """
        private DateTime? TryParseDueDateTime(string mesDeCierre, string anio)
        {
            if (string.IsNullOrWhiteSpace(anio) || string.IsNullOrWhiteSpace(mesDeCierre))
                return null;

            // Map Spanish months to integers if necessary, or just attempt a parse if it's already a number
            int month = 12; // default to december if we can't parse the month name
            var mesLower = mesDeCierre.ToLower().Trim();

            if (mesLower.StartsWith("ene")) month = 1;
            else if (mesLower.StartsWith("feb")) month = 2;
            else if (mesLower.StartsWith("mar")) month = 3;
            else if (mesLower.StartsWith("abr")) month = 4;
            else if (mesLower.StartsWith("may")) month = 5;
            else if (mesLower.StartsWith("jun")) month = 6;
            else if (mesLower.StartsWith("jul")) month = 7;
            else if (mesLower.StartsWith("ago")) month = 8;
            else if (mesLower.StartsWith("sep")) month = 9;
            else if (mesLower.StartsWith("oct")) month = 10;
            else if (mesLower.StartsWith("nov")) month = 11;
            else if (mesLower.StartsWith("dic")) month = 12;
            else int.TryParse(mesLower, out month);

            if (month < 1 || month > 12) month = 12;

            if (int.TryParse(anio, out int year))
            {
                 try
                 {
                     // Set due date to the last day of the target month
                     int daysInMonth = DateTime.DaysInMonth(year, month);
                     return new DateTime(year, month, daysInMonth, 23, 59, 59, DateTimeKind.Utc);
                 }
                 catch
                 {
                     return null;
                 }
            }
            return null;
        }

        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)
"""
content = content.replace("        private async Task ProcessExcelInBackground(byte[] fileBytes, string usuarioCarga, AppDbContext dbContext, IPlannerService plannerService, CancellationToken token)", helper_method)


# replace the call:
old_call = """
                            // Process 1 requirement: Generate Planner Task ID with retry logic
                            plannerTaskId = await plannerService.CreateTaskAsync(nombreProyecto);
"""
new_call = """
                            // Parse dueDateTime
                            var dueDateTime = TryParseDueDateTime(mesDeCierre, anio);

                            // Process 1 requirement: Generate Planner Task ID with retry logic
                            plannerTaskId = await plannerService.CreateTaskAsync(nombreProyecto, null, dueDateTime);
"""
content = content.replace(old_call, new_call)


with open('Backend/Controllers/PlannerMetasController.cs', 'w') as f:
    f.write(content)
