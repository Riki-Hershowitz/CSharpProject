using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class MemoryCardService
{
    private const string FolderName = "MISC";
    private const string FileName = "AUTPRINT.MRK";

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

                // אם יש עבודות הדפסה, מציג אותן במסך
                if (printJobs.Any())
                {
                    foreach (var job in printJobs)
                    {
                        Console.WriteLine($"PID: {job.PID}, Quantity: {job.Quantity}, ImagePath: {job.ImagePath}");
                    }
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
}

// מחלקת PrintJob המייצגת עבודה להדפסה
public class PrintJob
{
    public int PID { get; set; } // מזהה עבודה
    public int Quantity { get; set; } // כמות הדפסות
    public string ImagePath { get; set; } // נתיב לתמונה
}

// קריאה לפונקציה ממקום אחר בקוד
public class Program
{
    public static void Main()
    {
        MemoryCardService service = new MemoryCardService();
        service.ProcessMemoryCard(); // קורא לפונקציה המעבדת את כרטיס הזיכרון
    }
}
