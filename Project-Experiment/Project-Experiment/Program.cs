using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class MemoryCardService
{
    private const string FolderName = "MISC";
    private const string FileName = "AUTPRINT.MRK";

    public void ProcessMemoryCard(OrderManagement OManagement)
    {
        DriveInfo[] drives = DriveInfo.GetDrives();
        var removableDrives = drives.Where(d => d.IsReady && d.DriveType == DriveType.Removable);

        if (!removableDrives.Any())
        {
            Console.WriteLine("Memory card not found");
            return;
        }

        foreach (DriveInfo drive in removableDrives)
        {
            string miscFolderPath = Path.Combine(drive.RootDirectory.FullName, FolderName);
            string filePath = Path.Combine(miscFolderPath, FileName);

            if (Directory.Exists(miscFolderPath) && File.Exists(filePath))
            {
                Console.WriteLine($"Processing file: {filePath}");
                List<SavingImages> SavingImage = ParsePrintJobs(filePath, OManagement, drive.RootDirectory.FullName);

                if (SavingImage.Any())
                {
                    foreach (var job in SavingImage)
                    {
                        Console.WriteLine($"Id: {job.Id}, Amount: {job.Amount}, Images: {(job.Images != null ? "Yes" : "No")}");
                    }
                }
                else
                {
                    Console.WriteLine("No print jobs found in the file.");
                }
            }
            else
            {
                Console.WriteLine($"MISC folder or {FileName} not found in {drive.Name}");
            }
        }
    }

    private List<SavingImages> ParsePrintJobs(string filePath, OrderManagement OManagement, string rootPath)
    {
        List<SavingImages> jobs = new List<SavingImages>();
        string[] lines = File.ReadAllLines(filePath);

        SavingImages currentJob = null;
        Regex jobRegex = new Regex(@"\[JOB\]");
        Regex pidRegex = new Regex(@"PRT Id = (\d+)");
        Regex qtyRegex = new Regex(@"PRT QTY = (\d+)");
        Regex imgRegex = new Regex(@"<IMG SRC = \""(.+?)\"">");

        using (var context = new OrderDbContext()) // פתיחת קונטקסט יחיד
        {
            foreach (string line in lines)
            {
                if (jobRegex.IsMatch(line))
                {
                    if (currentJob != null)
                    {
                        context.SavingImages.Add(currentJob); // הוספת התמונה ל-DB
                        jobs.Add(currentJob);
                    }

                    currentJob = new SavingImages();
                    currentJob.OrderCode = OManagement?.OrderCode ?? throw new Exception("Error! The order was not opened properly.");
                }
                else if (pidRegex.IsMatch(line) && currentJob != null)
                {
                    currentJob.Id = int.Parse(pidRegex.Match(line).Groups[1].Value);
                }
                else if (qtyRegex.IsMatch(line) && currentJob != null)
                {
                    currentJob.Amount = int.Parse(qtyRegex.Match(line).Groups[1].Value);
                }
                else if (imgRegex.IsMatch(line) && currentJob != null)
                {
                    string relativePath = imgRegex.Match(line).Groups[1].Value;
                    string fullPath = Path.Combine(rootPath, relativePath); // שימוש בנתיב המלא

                    if (File.Exists(fullPath))
                    {
                        currentJob.Images = File.ReadAllBytes(fullPath);
                    }
                }
            }

            if (currentJob != null)
            {
                context.SavingImages.Add(currentJob);
                jobs.Add(currentJob);
            }

            context.SaveChanges(); // שמירה של כל הרשומות בפעם אחת
        }

        return jobs;
    }
}

// קריאה לפונקציה ממקום אחר בקוד
public class Program
{
    public static void ReceivingTheImagesFromTheCardIntoTheSystem(OrderManagement OManagement)
    {
        MemoryCardService service = new MemoryCardService();
        service.ProcessMemoryCard(OManagement);
    }

    public void AssignNewOrderToOfficer(string orderCode)
    {
        // קבלת כל הפקידים
        var officers = dbContext.Officers.ToList();

        // קבלת כל ההזמנות שלא בוצעו או שבטיפול
        var pendingOrders = dbContext.OrderManagement
                                    .Where(o => o.ProcessStatus == 0 || o.ProcessStatus == 1)
                                    .GroupBy(o => o.OfficerCode)
                                    .Select(g => new
                                    {
                                        OfficerCode = g.Key,
                                        OrderCount = g.Count()
                                    })
                                    .ToList();

        // מציאת הפקיד עם מספר ההזמנות הנמוך ביותר
        var officerWithFewestOrders = officers
            .Select(officer => new
            {
                Officer = officer,
                PendingOrdersCount = pendingOrders.FirstOrDefault(po => po.OfficerCode == officer.OfficerCode)?.OrderCount ?? 0
            })
            .OrderBy(o => o.PendingOrdersCount)
            .FirstOrDefault();

        if (officerWithFewestOrders != null)
        {
            // קבלת ההזמנה החדשה
            var order = new OrderManagement(orderCode, 0, officerWithFewestOrders.Officer.OfficerCode, null);

            // עדכון ההזמנה עם הפקיד שנבחר
            dbContext.OrderManagement.Add(order);

            // עדכון הסטטוס של הפקיד אם צריך (לא חובה)
            officerWithFewestOrders.Officer.IsAvailable = false; // לדוגמה, אם יש שדה כזה

            // שמירה בבסיס הנתונים
            dbContext.SaveChanges();
        }
    }

}
