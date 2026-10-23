using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain;

namespace PeopleHub.Data;

public static class SeedData
{
    public static async Task Initialise(IServiceProvider provider,IConfiguration config,IHostEnvironment env)
    {
        using var scope=provider.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // For the initial SQLite bootstrap. Move to migrations before evolving production schemas.
        if(!env.IsDevelopment() && config["HRMS_ALLOW_ENSURE_CREATED"]!="true")
            throw new InvalidOperationException("Production startup requires an explicitly reviewed DB bootstrap (HRMS_ALLOW_ENSURE_CREATED=true) or migrations.");
        await db.Database.EnsureCreatedAsync();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach(var role in new[]{"Admin","HR","Manager","Employee"})
            if(!await roles.RoleExistsAsync(role))await roles.CreateAsync(new IdentityRole(role));
        if(!env.IsDevelopment() && config["HRMS_SEED_DEMO"]!="true")return;
        if(config["HRMS_SEED_DEMO"]!="true")return;
        var pass=config["HRMS_SEED_PASSWORD"];
        if(string.IsNullOrWhiteSpace(pass)||pass.Length<12)
            throw new InvalidOperationException("Set HRMS_SEED_PASSWORD (12+ characters) to enable demo seeding.");
        var company=await db.Companies.FirstOrDefaultAsync(x=>x.Code=="DEMO");
        if(company is null){company=new Company{Name="Atlas People Operations",Code="DEMO"};db.Companies.Add(company);await db.SaveChangesAsync();}
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var admin=await users.FindByNameAsync("demo.hr");
        if(admin is null){
            admin=new AppUser{UserName="demo.hr",Email="demo.hr@example.invalid",DisplayName="Demo HR Administrator",CompanyId=company.Id,EmailConfirmed=true};
            var result=await users.CreateAsync(admin,pass);
            if(!result.Succeeded)throw new InvalidOperationException(string.Join("; ",result.Errors.Select(x=>x.Description)));
        }
        if(!await users.IsInRoleAsync(admin,"HR"))await users.AddToRoleAsync(admin,"HR");
        var deptNames=new[]{("Engineering","ENG"),("People & Culture","HR"),("Finance","FIN"),("Operations","OPS"),("Sales","SAL")};
        foreach(var (name,code) in deptNames){if(!await db.Departments.AnyAsync(x=>x.CompanyId==company.Id&&x.Code==code))db.Departments.Add(new Department{CompanyId=company.Id,Name=name,Code=code});}
        await db.SaveChangesAsync();
        var departments=await db.Departments.Where(x=>x.CompanyId==company.Id).ToDictionaryAsync(x=>x.Code);
        var people=new[]{("Aisha","Rahman","HR Manager","HR"),("Omar","Hassan","Software Engineer","ENG"),("Maya","Thomas","UX Designer","ENG"),
            ("Fatima","Al Balushi","Accountant","FIN"),("Daniel","Lewis","Operations Lead","OPS"),("Zara","Khan","Recruiter","HR"),
            ("Adil","Khan","Sales Executive","SAL"),("Sana","Patel","Data Analyst","FIN"),("Khalid","Mansoor","Support Specialist","OPS"),
            ("Leena","Joseph","Developer","ENG"),("Yusuf","Ali","Coordinator","OPS"),("Noor","Ahmed","Account Executive","SAL")};
        for(var i=0;i<people.Length;i++){
            var p=people[i];var code=$"EMP-{i+1:0000}";
            if(!await db.Employees.AnyAsync(x=>x.CompanyId==company.Id&&x.EmployeeCode==code))
                db.Employees.Add(new Employee{CompanyId=company.Id,EmployeeCode=code,FirstName=p.Item1,LastName=p.Item2,JobTitle=p.Item3,DepartmentId=departments[p.Item4].Id,Email=$"{p.Item1.ToLower()}.{p.Item2.ToLower().Replace(" ","")}@example.invalid",JoinDate=new DateOnly(2023+i%3,1+i%12,12),Location="Muscat"});
        }
        await db.SaveChangesAsync();
        var employees=await db.Employees.Where(x=>x.CompanyId==company.Id).OrderBy(x=>x.Id).ToListAsync();
        if(admin.EmployeeId is null){admin.EmployeeId=employees[0].Id;await users.UpdateAsync(admin);}
        if(!await db.LeavePolicies.AnyAsync(x=>x.CompanyId==company.Id)){
            db.LeavePolicies.AddRange(new LeavePolicy{CompanyId=company.Id,Name="Annual leave",AnnualDays=30},new LeavePolicy{CompanyId=company.Id,Name="Sick leave",AnnualDays=14});
            await db.SaveChangesAsync();
        }
        var annual=await db.LeavePolicies.FirstAsync(x=>x.CompanyId==company.Id);
        var today=DateOnly.FromDateTime(DateTime.Today);
        foreach(var (p,i) in employees.Select((x,i)=>(x,i))){
            if(!await db.Attendances.AnyAsync(x=>x.CompanyId==company.Id&&x.EmployeeId==p.Id&&x.WorkDate==today))
                db.Attendances.Add(new Attendance{CompanyId=company.Id,EmployeeId=p.Id,WorkDate=today,CheckIn=new TimeOnly(8,(i*4)%50),CheckOut=new TimeOnly(17,0),Status=i%6==0?"Remote":"Present"});
        }
        if(!await db.LeaveRequests.AnyAsync(x=>x.CompanyId==company.Id))
            foreach(var (p,i) in employees.Skip(1).Take(5).Select((x,i)=>(x,i)))db.LeaveRequests.Add(new LeaveRequest{CompanyId=company.Id,EmployeeId=p.Id,LeavePolicyId=annual.Id,StartDate=today.AddDays(i+2),EndDate=today.AddDays(i+4),Reason="Fictional sample leave request"});
        if(!await db.JobOpenings.AnyAsync(x=>x.CompanyId==company.Id)){
            db.JobOpenings.AddRange(new JobOpening{CompanyId=company.Id,Title="Backend Engineer",DepartmentId=departments["ENG"].Id},
                new JobOpening{CompanyId=company.Id,Title="Finance Analyst",DepartmentId=departments["FIN"].Id},
                new JobOpening{CompanyId=company.Id,Title="HR Associate",DepartmentId=departments["HR"].Id});
        }
        await db.SaveChangesAsync();
    }
}
