using System; // מאפשר שימוש בפקודות בסיסיות כמו Console, List, וכו'
using System.Collections.Generic; // מאפשר שימוש במבני נתונים כמו רשימות
using System.IO; // מאפשר קריאה וכתיבה לקבצים
using System.Linq; // מאפשר שימוש ב- LINQ כדי לבצע חיפושים ופעולות על אוספים
using System.Text.RegularExpressions; // מאפשר עבודה עם ביטויים רגולריים
using System.Data.SqlClient; // מאפשר חיבור ופעולה עם SQL Server
using BLL.Interfaces; // כולל את הממשק IMemoryCardService, שמוגדר במקום אחר

namespace BLL.Services
{
    // שירות שמטפל בהבאת מידע מכרטיס זיכרון
    public class MemoryCardService : IMemoryCardService
    {
        // משתנים קבועים המייצגים את תיקיית MISC ואת שם הקובץ
        private const string FolderName = "MISC";
        private const string FileName = "AUTPRINT.MRK";

        // מגדירים את המידע להתחברות למסד נתונים
        private readonly string connectionString = "Server=your_server;Database=your_db;User Id=your_user;Password=your_password;"; // עדכון פרטי החיבור למסד נתונים

        // פונקציה שמעבדת את כרטיס הזיכרון
        public void ProcessMemoryCard()
        {
            // מקבלת את כל הכוננים המהירים במערכת
            DriveInfo[] drives = DriveInfo.GetDrives();

            // בוחרת את הכוננים הניתנים להסרה (כרטיסי זיכרון לדוגמה)
            var removableDrives = drives.Where(d => d.IsReady && d.DriveType == DriveType.Removable);

            // עבור כל כונן נתון שנמצא
            foreach (DriveInfo drive in removableDrives)
            {
                // בונה את הנתיב של תיקיית MISC והקובץ AUTPRINT.MRK
                string miscFolderPath = Path.Combine(drive.RootDirectory.FullName, FolderName);
                string filePath = Path.Combine(miscFolderPath, FileName);

                // אם תיקיית MISC והקובץ קיימים, מעבד את המידע
                if (Directory.Exists(miscFolderPath) && File.Exists(filePath))
                {
                    Console.WriteLine($"Processing file: {filePath}"); // מציג את הקובץ המעובד
                    List<PrintJob> printJobs = ParsePrintJobs(filePath); // קרא את פרטי עבודות ההדפסה מהקובץ

                    // אם יש עבודות הדפסה, שומר אותן במסד הנתונים
                    if (printJobs.Any())
                    {
                        SaveToDatabase(printJobs);
                    }
                    else
                    {
                        Console.WriteLine("No print jobs found in the file.");
                    }
                }
                else
                {
                    // אם תיקיית MISC או הקובץ לא קיימים
                    Console.WriteLine($"MISC folder or {FileName} not found in {drive.Name}");
                }
            }
        }

        // פונקציה שמפרקת את עבודות ההדפסה מהקובץ
        private List<PrintJob> ParsePrintJobs(string filePath)
        {
            List<PrintJob> jobs = new List<PrintJob>(); // רשימה לאחסון עבודות ההדפסה
            string[] lines = File.ReadAllLines(filePath); // קורא את כל השורות בקובץ

            PrintJob currentJob = null; // משתנה לאחסון עבודה נוכחית
            // יצירת ביטויים רגולריים למציאת הנתונים בקובץ
            Regex jobRegex = new Regex(@"\[JOB\]"); // מחפש את המילה JOB
            Regex pidRegex = new Regex(@"PRT PID = (\d+)"); // מחפש את PID
            Regex qtyRegex = new Regex(@"PRT QTY = (\d+)"); // מחפש את כמות ההדפסות
            Regex imgRegex = new Regex(@"<IMG SRC = ""(.+?)"">"); // מחפש את הנתיב לתמונה

            // עבור כל שורה בקובץ
            foreach (string line in lines)
            {
                // אם השורה מציינת את תחילת עבודה חדשה
                if (jobRegex.IsMatch(line))
                {
                    if (currentJob != null)
                    {
                        jobs.Add(currentJob); // מוסיף את העבודה הנוכחית לרשימה
                    }
                    currentJob = new PrintJob(); // מתחיל עבודה חדשה
                }
                // אם השורה מכילה PID
                else if (pidRegex.IsMatch(line) && currentJob != null)
                {
                    currentJob.PID = int.Parse(pidRegex.Match(line).Groups[1].Value); // שומר את ה-PID
                }
                // אם השורה מכילה כמות
                else if (qtyRegex.IsMatch(line) && currentJob != null)
                {
                    currentJob.Quantity = int.Parse(qtyRegex.Match(line).Groups[1].Value); // שומר את כמות ההדפסות
                }
                // אם השורה מכילה נתיב לתמונה
                else if (imgRegex.IsMatch(line) && currentJob != null)
                {
                    currentJob.ImagePath = imgRegex.Match(line).Groups[1].Value; // שומר את נתיב התמונה
                }
            }

            // אם העבודה הנוכחית לא הוספה לרשימה, הוספה אותה בסוף
            if (currentJob != null)
            {
                jobs.Add(currentJob);
            }

            return jobs; // מחזיר את רשימת עבודות ההדפסה
        }

        // פונקציה ששומרת את עבודות ההדפסה במסד הנתונים
        private void SaveToDatabase(List<PrintJob> jobs)
        {
            using (SqlConnection connection = new SqlConnection(connectionString)) // יוצרת חיבור למסד הנתונים
            {
                connection.Open(); // פותחת את החיבור
                // עבור כל עבודה ברשימה
                foreach (var job in jobs)
                {
                    string query = "INSERT INTO PrintJobs (PID, Quantity, ImagePath) VALUES (@PID, @Quantity, @ImagePath)"; // השאילתה להוספת נתונים
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PID", job.PID); // מוסיפה את ה-PID כפרמטר
                        command.Parameters.AddWithValue("@Quantity", job.Quantity); // מוסיפה את הכמות כפרמטר
                        command.Parameters.AddWithValue("@ImagePath", job.ImagePath); // מוסיפה את נתיב התמונה כפרמטר

                        command.ExecuteNonQuery(); // מבצעת את השאילתה במסד הנתונים
                    }
                }
            }
            Console.WriteLine("Print jobs successfully saved to database."); // מציגה הודעה על שמירת הנתונים בהצלחה
        }
    }

    // מחלקת PrintJob המייצגת עבודה להדפסה
    public class PrintJob
    {
        public int PID { get; set; } // מזהה עבודה
        public int Quantity { get; set; } // כמות הדפסות
        public string ImagePath { get; set; } // נתיב לתמונה
    }
}
