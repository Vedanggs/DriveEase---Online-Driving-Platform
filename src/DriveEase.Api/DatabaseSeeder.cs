using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SchoolsDbContext>();

        var desiredSchools = new[]
        {
            DrivingSchool.Register("Pune Road Masters",            "12 MG Road, Shivajinagar, Pune - 411005",              "admin@puneroadmasters.com"),
            DrivingSchool.Register("Mumbai Drive Academy",         "45 Linking Road, Bandra West, Mumbai - 400050",        "admin@mumbaidriveacademy.com"),
            DrivingSchool.Register("Nashik Road Pro",              "8 College Road, Nashik - 422005",                      "admin@nashikroadpro.com"),
            DrivingSchool.Register("Nagpur Speed School",          "22 Wardha Road, Dharampeth, Nagpur - 440010",          "admin@nagpurspeedschool.com"),
            DrivingSchool.Register("Aurangabad Motor Training",    "5 Station Road, Cidco, Aurangabad - 431001",           "admin@aurangabadmotor.com"),
            DrivingSchool.Register("Kolhapur Drive Centre",        "18 Tarabai Park, Kolhapur - 416003",                   "admin@kolhapurdrive.com"),
            DrivingSchool.Register("Solapur Road Academy",         "33 Hotgi Road, Solapur - 413003",                      "admin@solapurroadacademy.com"),
            DrivingSchool.Register("Thane AutoDrive School",       "7 Gokhale Road, Naupada, Thane West - 400602",         "admin@thaneautodrive.com"),
            DrivingSchool.Register("Navi Mumbai Driving Hub",      "Plot 14, Sector 17, Vashi, Navi Mumbai - 400703",      "admin@navimumbaidriving.com"),
            DrivingSchool.Register("Pimpri-Chinchwad Road School", "88 Old Mumbai Road, Pimpri, Pune - 411018",            "admin@pcmcroadschool.com"),
            DrivingSchool.Register("Sangli Drive Institute",       "21 Miraj Road, Sangli - 416416",                       "admin@sanglidrive.com"),
            DrivingSchool.Register("Satara Motor Academy",         "9 Powai Naka, Satara - 415001",                        "admin@sataramotor.com"),
            DrivingSchool.Register("Latur Road Training Centre",   "15 Udgir Road, Latur - 413512",                        "admin@laturroadtraining.com"),
            DrivingSchool.Register("Jalgaon Drive School",         "42 NH-6 Bypass, Jalgaon - 425001",                     "admin@jalgaondrive.com"),
            DrivingSchool.Register("Amravati AutoSkills",          "6 Badnera Road, Amravati - 444601",                    "admin@amravatiautoskills.com"),
            DrivingSchool.Register("Akola Road Masters",           "11 NH-53, Civil Lines, Akola - 444001",                "admin@akolaroadmasters.com"),
            DrivingSchool.Register("Ratnagiri Coastal Drive",      "3 Beach Road, Ratnagiri - 415612",                     "admin@ratnagiricoastaldrive.com"),
        };

        // Add only schools whose name isn't already in the database
        var existingNames = (await context.Schools.Select(s => s.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newSchools    = desiredSchools.Where(s => !existingNames.Contains(s.Name)).ToArray();

        if (newSchools.Length == 0) return;

        await context.Schools.AddRangeAsync(newSchools);
        await context.SaveChangesAsync();

        // Seed 3 instructors for each newly added school only
        var instructorNames = new[]
        {
            ("Rajesh Kumar",    "Arjun Sharma",   "Priya Desai"),
            ("Suresh Patil",    "Neha Joshi",     "Amit Kulkarni"),
            ("Vikram Nair",     "Sneha Reddy",    "Rahul Mehta"),
            ("Deepak Tiwari",   "Kavita Rao",     "Sanjay Gupta"),
            ("Mohan Pawar",     "Anita Shinde",   "Ravi Jadhav"),
            ("Ganesh Bhosale",  "Pooja Naik",     "Sachin More"),
            ("Nilesh Mane",     "Sunita Kadam",   "Yogesh Salunkhe"),
            ("Anil Gaikwad",    "Madhuri Wagh",   "Tejas Sawant"),
            ("Vinod Kharat",    "Rekha Dalvi",    "Omkar Chavan"),
            ("Santosh Jagtap",  "Vaishali Rane",  "Nikhil Deshpande"),
            ("Pramod Kale",     "Usha Patil",     "Shubham Pol"),
            ("Mangesh Karale",  "Archana Jadhav", "Rohit Bhatt"),
            ("Dilip Londhe",    "Sujata Shinde",  "Akash Thorat"),
            ("Hemant Patil",    "Geeta Borse",    "Vishal Nikam"),
            ("Ajay Deshmukh",   "Nanda Kulkarni", "Kiran Ghuge"),
            ("Sunil Wankhade",  "Asha Dhole",     "Pratik Ingale"),
            ("Ramesh Gavhane",  "Swati Bendre",   "Tushar Kolhe"),
        };

        var instructors    = new List<Instructor>();
        var licenseCounter = 1001 + (existingNames.Count * 3);

        // Map desiredSchools index → instructorNames index so names stay consistent
        for (var i = 0; i < desiredSchools.Length; i++)
        {
            if (!newSchools.Contains(desiredSchools[i])) continue;

            var (name1, name2, name3) = instructorNames[i];
            var schoolId = desiredSchools[i].Id;

            instructors.Add(Instructor.Create(schoolId, name1, $"MH-{licenseCounter++:0000}"));
            instructors.Add(Instructor.Create(schoolId, name2, $"MH-{licenseCounter++:0000}"));
            instructors.Add(Instructor.Create(schoolId, name3, $"MH-{licenseCounter++:0000}"));
        }

        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();
    }
}
