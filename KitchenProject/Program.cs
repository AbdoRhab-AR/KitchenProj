using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using KitchenProject.Models.Repositories;
using KitchenProject.Models.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options =>
{
	options.UseArabicValidationMessages();
	options.Filters.Add<RoleAccessFilter>(); // role -> allowed screens, see Classes/AppRoles.cs
});


builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ProfitService>();
builder.Services.AddScoped<AccountingService>();

//-------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>
	(x => x.UseSqlServer(builder.Configuration.GetConnectionString("DbCon")));
//-------------------------------------------------

builder.Services.Configure<ProgrammerSettings>
	(builder.Configuration.GetSection("ProgrammerSettings"));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 6; // minimum 6 characters
	options.Password.RequiredUniqueChars = 1; // حرف فريد واحد على الأقل
	options.Password.RequireNonAlphanumeric = false; // عند تفعيلها ستتطلب كلمات المرور رمز خاص واحد على الأقل
	options.Password.RequireDigit = true; //يجب أن تحتوي على رقم
	options.Password.RequireLowercase = false; //لا يلزم أحرف صغيرة
	options.Password.RequireUppercase = false; //لا يلزم أحرف كبيرة
	options.Lockout.MaxFailedAccessAttempts = 5; // اقفال الحساب بعد 5 محاولات دخول فاشلة
	options.Lockout.DefaultLockoutTimeSpan =
			 TimeSpan.FromMinutes(15); //اقفال الحساب لمدة خمسة عشر دقيقة 
	options.SignIn.RequireConfirmedAccount = true; // تسجيل الدخول يتطلب تأكيد الحساب
	options.User.RequireUniqueEmail = false; // staff may have no email; login is by user name
})
	   .AddEntityFrameworkStores<ApplicationDbContext>()
	//بقاعدة البيانات للوصول الى الجداول وادراء الإستعلامات Identity ربط
	.AddDefaultTokenProviders()
	.AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>(); //يُستخدم موفر الرموز الإفتراضي لإنشاء رموز إعادة تعيين كلمة المرور وتغيير البريد الإلكتروني وإرسالها إلى البريد الإلكتروني والهاتف والمصادقة الثنائية .


//صالحة إلا لمدة الدقائق او الساعات المحددة بعد إنشائها//DataProtectorTokenProvider//لن تكون الرموز التي يتم إنشاؤها بواسطة موفر الرموز
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
							   options.TokenLifespan = TimeSpan.FromMinutes(15));

builder.Services.ConfigureApplicationCookie(options =>
{
	options.LoginPath = "/Account/Login";
	options.LogoutPath = "/Account/Logout";
	options.AccessDeniedPath = "/Account/AccessDenied";
	options.ExpireTimeSpan = TimeSpan.FromHours(12);
	options.SlidingExpiration = true;
});
//opt.TokenLifespan = TimeSpan.FromDays(1));


//-------------------------------------------------


builder.Services.AddAuthorization(options =>
{
	// every page requires login unless marked [AllowAnonymous]
	options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

	options.AddPolicy("CreatePolicy", policy => policy.RequireClaim("Create", "true"));
	options.AddPolicy("EditPolicy", policy => policy.RequireClaim("Edit", "true"));
	options.AddPolicy("DeletePolicy", policy => policy.RequireClaim("Delete", "true"));

	options.AddPolicy("EditUserPolicy", policy => policy.RequireClaim("EditUser", "true"));
	options.AddPolicy("SiteStatePolicy", policy => policy.RequireClaim("SiteState", "true"));
	//options.AddPolicy("ProfilePolicy", policy => policy.RequireClaim("UserProfile", "true")); // just example

	options.AddPolicy("AdminOrProgPolicy", policy => policy.RequireRole("Prog", "Admin")); // اذا اردنا استعمال الرول كبوليصي ثم نطبقها في الكونترولر كبوليصي


	options.AddPolicy("ProgOrAdminOrEmployeePolicy", policy =>
	{
		policy.RequireClaim(ClaimTypes.Role, "Admin", "Employee", "Prog"); // اذا اردنا استعمال الرول كبوليصي ثم نطبقها في الكونترولر كبوليصي
	});

	options.AddPolicy("SettingsPolicy", policy =>
	{
		policy.RequireClaim("SiteState", "EditUser");
	});


	options.AddPolicy("EditUserPolicy", policy =>
		policy.RequireAssertion(p =>
		p.User.IsInRole("Prog") || (p.User.IsInRole("Admin")
		&& p.User.HasClaim(claim => claim.Type == "Edit" && claim.Value == "true")))
	);

});



//------------------------------------------------
//builder.Services.AddAutoMapper(typeof(Program).Assembly);

//appsettings.jason تسجيل قسم الإعدادات الخاصة بالإيميل والموجود في 
var emailconfig = builder.Configuration
				  .GetSection("MailSettings").Get<MailSettings>();
builder.Services.AddSingleton(emailconfig);

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddMvc(options =>
{
	options.Filters.Add(typeof(AsyncActionFilter));
}).AddXmlSerializerFormatters();
builder.Services.AddHttpContextAccessor(); //AsyncActionFilter لإستعمالها لجلب اليوزر الحالي في 


builder.Services.AddSingleton<UserSessionTracker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
	AccountingSeeder.SeedAsync(db).GetAwaiter().GetResult(); // شجرة الحسابات الافتراضية عند أول تشغيل
}

// Fixed number/date format (decimal point ".") regardless of the Windows region settings,
// so amounts typed in number inputs always bind correctly.
var appCulture = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new Microsoft.AspNetCore.Builder.RequestLocalizationOptions
{
	DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(appCulture),
	SupportedCultures = new[] { appCulture },
	SupportedUICultures = new[] { appCulture }
});


if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseStatusCodePagesWithReExecute("/Error/{0}");
}
else if (app.Environment.IsDevelopment())
	app.UseDeveloperExceptionPage(); //صفحة الخطأ الخاصة بالمطور


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();//Who are you//للتحقق من هوية المستخدم، الاسم وكلمة المرور والرمز الخاص والمصادقة الثنائية
app.UseAuthorization();//What you allowed to do//للتحقق من صلاحية المستخدم

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");




app.Run();
