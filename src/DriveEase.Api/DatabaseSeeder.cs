using DriveEase.Schools.Domain.Entities;
using DriveEase.Schools.Infrastructure.Persistence;
using DriveEase.Students.Domain.Entities;
using DriveEase.Students.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SchoolsDbContext>();

        var desiredSchools = new (string Name, string Address, string ContactEmail)[]
        {
            ("Pune Road Masters",             "12 MG Road, Shivajinagar, Pune - 411005",         "admin@puneroadmasters.com"),
            ("Mumbai Drive Academy",          "45 Linking Road, Bandra West, Mumbai - 400050",   "admin@mumbaidriveacademy.com"),
            ("Nashik Road Pro",               "8 College Road, Nashik - 422005",                 "admin@nashikroadpro.com"),
            ("Nagpur Speed School",           "22 Wardha Road, Dharampeth, Nagpur - 440010",     "admin@nagpurspeedschool.com"),
            ("Aurangabad Motor Training",     "5 Station Road, Cidco, Aurangabad - 431001",      "admin@aurangabadmotor.com"),
            ("Kolhapur Drive Centre",         "18 Tarabai Park, Kolhapur - 416003",              "admin@kolhapurdrive.com"),
            ("Solapur Road Academy",          "33 Hotgi Road, Solapur - 413003",                 "admin@solapurroadacademy.com"),
            ("Thane AutoDrive School",        "7 Gokhale Road, Naupada, Thane West - 400602",    "admin@thaneautodrive.com"),
            ("Navi Mumbai Driving Hub",       "Plot 14, Sector 17, Vashi, Navi Mumbai - 400703", "admin@navimumbaidriving.com"),
            ("Pimpri-Chinchwad Road School",  "88 Old Mumbai Road, Pimpri, Pune - 411018",       "admin@pcmcroadschool.com"),
            ("Sangli Drive Institute",        "21 Miraj Road, Sangli - 416416",                  "admin@sanglidrive.com"),
            ("Satara Motor Academy",          "9 Powai Naka, Satara - 415001",                   "admin@sataramotor.com"),
            ("Latur Road Training Centre",    "15 Udgir Road, Latur - 413512",                   "admin@laturroadtraining.com"),
            ("Jalgaon Drive School",          "42 NH-6 Bypass, Jalgaon - 425001",                "admin@jalgaondrive.com"),
            ("Amravati AutoSkills",           "6 Badnera Road, Amravati - 444601",               "admin@amravatiautoskills.com"),
            ("Akola Road Masters",            "11 NH-53, Civil Lines, Akola - 444001",           "admin@akolaroadmasters.com"),
            ("Ratnagiri Coastal Drive",       "3 Beach Road, Ratnagiri - 415612",                "admin@ratnagiricoastaldrive.com"),
            ("Mumbai Central Driving Academy","123 MG Road, Mumbai Central, Mumbai - 400008",    "admin@mumbaicentral.com"),
            ("Sunrise Driving Academy",       "77 Sunrise Blvd, Bandra East, Mumbai - 400051",   "admin@sunrise.driveease.com"),
            ("Thinkschool Safe Drive Institute", "101 Thinkschool Lane, Pune - 411007",          "admin@thinkschool.com")
        };

        var existingSchoolNames = (await context.Schools
            .Select(s => s.Name)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var schoolsToAdd = desiredSchools
            .Where(s => !existingSchoolNames.Contains(s.Name))
            .Select(s => DrivingSchool.Register(s.Name, s.Address, s.ContactEmail))
            .ToArray();

        if (schoolsToAdd.Length > 0)
        {
            await context.Schools.AddRangeAsync(schoolsToAdd);
            await context.SaveChangesAsync();
        }

        var schoolIdsByName = await context.Schools
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Name, s => s.Id, StringComparer.OrdinalIgnoreCase);

        // Seed 3 instructors for each newly added school only
        var instructorNames = new[]
        {
            ("Suresh Jadhav",   "Neha Joshi",     "Amit Kulkarni"),
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
            ("Amit Sharma",     "Deepak Gupta",   "Vijay Kale"),
            ("Mohan Singh",     "Karan Malhotra", "Sanjay Dutt"),
            ("Rahul Sharma",    "Ketan Patel",    "Anil Deshmukh"),
        };

        var demoMap = new Dictionary<string, (string Email, string License, string PasswordHash)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Amit Sharma", ("amit.sharma@mumbaicentral.com", "MH-01AB1001", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Suresh Jadhav", ("suresh.jadhav@puneroadmasters.com", "MH-12CD2001", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Mohan Singh", ("mohan.singh@sunrise.driveease.com", "MH-02EF3001", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Rahul Sharma", ("rahul.sharma@thinkschool.com", "MH-20GH4001", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Suresh Patil", ("suresh.patil@mumbaidriveacademy.com", "MH-1013", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Vikram Nair", ("vikram.nair@nashikroadpro.com", "MH-1016", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Deepak Tiwari", ("deepak.tiwari@nagpurspeedschool.com", "MH-1019", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Mohan Pawar", ("mohan.pawar@aurangabadmotor.com", "MH-1022", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Ganesh Bhosale", ("ganesh.bhosale@kolhapurdrive.com", "MH-1025", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Nilesh Mane", ("nilesh.mane@solapurroadacademy.com", "MH-1028", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Anil Gaikwad", ("anil.gaikwad@thaneautodrive.com", "MH-1031", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Vinod Kharat", ("vinod.kharat@navimumbaidriving.com", "MH-1034", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Santosh Jagtap", ("santosh.jagtap@pcmcroadschool.com", "MH-1037", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Pramod Kale", ("pramod.kale@sanglidrive.com", "MH-1040", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Mangesh Karale", ("mangesh.karale@sataramotor.com", "MH-1043", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Dilip Londhe", ("dilip.londhe@laturroadtraining.com", "MH-1046", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Hemant Patil", ("hemant.patil@jalgaondrive.com", "MH-1049", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Ajay Deshmukh", ("ajay.deshmukh@amravatiautoskills.com", "MH-1052", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Sunil Wankhade", ("sunil.wankhade@akolaroadmasters.com", "MH-1055", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) },
            { "Ramesh Gavhane", ("ramesh.gavhane@ratnagiricoastaldrive.com", "MH-1058", BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12)) }
        };

        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("Instructor@123", 12);
        var existingInstructorEmails = (await context.Instructors
            .Where(i => i.Email != null)
            .Select(i => i.Email!)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var instructorsToAdd = new List<Instructor>();

        for (var i = 0; i < desiredSchools.Length; i++)
        {
            var (name1, name2, name3) = instructorNames[i];
            if (!schoolIdsByName.TryGetValue(desiredSchools[i].Name, out var schoolId))
                continue;

            void AddInstructor(string name, int slot)
            {
                string email;
                string license;
                string passwordHash;

                if (demoMap.TryGetValue(name, out var demoInfo))
                {
                    email = demoInfo.Email;
                    license = demoInfo.License;
                    passwordHash = demoInfo.PasswordHash;
                }
                else
                {
                    email = $"{name.Replace(" ", ".").ToLower()}@driveease.com";
                    license = $"MH-{1001 + (i * 3) + slot:0000}";
                    passwordHash = defaultPasswordHash;
                }

                if (!existingInstructorEmails.Add(email))
                    return;

                instructorsToAdd.Add(Instructor.Create(schoolId, name, license, email, passwordHash));
            }

            AddInstructor(name1, 0);
            AddInstructor(name2, 1);
            AddInstructor(name3, 2);
        }

        if (instructorsToAdd.Count > 0)
        {
            await context.Instructors.AddRangeAsync(instructorsToAdd);
            await context.SaveChangesAsync();
        }

        // Demo student account — matches the credentials the frontend's
        // "Demo Credentials" button on the student login page fills in.
        var studentsContext = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
        const string demoStudentEmail = "test@gmail.com";

        var demoStudentExists = await studentsContext.Students
            .AnyAsync(s => s.Email == demoStudentEmail);

        if (!demoStudentExists)
        {
            var demoStudent = Student.Register(
                fullName: "Test Student",
                email: demoStudentEmail,
                phoneNumber: "9999999999",
                dateOfBirth: new DateOnly(2000, 1, 1),
                passwordHash: BCrypt.Net.BCrypt.HashPassword("Test@123", 12));

            await studentsContext.Students.AddAsync(demoStudent);
            await studentsContext.SaveChangesAsync();
        }
    }
}
