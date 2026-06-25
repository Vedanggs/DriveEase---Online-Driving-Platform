using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SchoolsDbContext>();

        if (await context.Schools.AnyAsync()) return;

        var schools = new[]
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

        await context.Schools.AddRangeAsync(schools);
        await context.SaveChangesAsync();

        // 3 instructors per school
        var instructors = new List<Instructor>();
        var licenseCounter = 1001;

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

        for (var i = 0; i < schools.Length; i++)
        {
            var (name1, name2, name3) = instructorNames[i];
            var schoolId = schools[i].Id;
            var state = "MH";

            instructors.Add(Instructor.Create(schoolId, name1, $"{state}-{licenseCounter++:0000}"));
            instructors.Add(Instructor.Create(schoolId, name2, $"{state}-{licenseCounter++:0000}"));
            instructors.Add(Instructor.Create(schoolId, name3, $"{state}-{licenseCounter++:0000}"));
        }

        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();
    }
}
