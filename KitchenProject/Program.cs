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

//-------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>
	(x => x.UseSqlServer(builder.Configuration.GetConnectionString("DbCon")));
//-------------------------------------------------

builder.Services.Configure<ProgrammerSettings>
	(builder.Configuration.GetSection("ProgrammerSettings"));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 6; // minimum 6 characters
	options.Password.RequiredUniqueChars = 1; // Õ—› ›—Ìœ Ê«Õœ ⁄·Ï «·√ﬁ·
	options.Password.RequireNonAlphanumeric = false; // ⁄‰œ  ›⁄Ì·Â« ”  ÿ·» ﬂ·„«  «·„—Ê— —„“ Œ«’ Ê«Õœ ⁄·Ï «·√ﬁ·
	options.Password.RequireDigit = true; //ÌÃ» √‰  Õ ÊÌ ⁄·Ï —ﬁ„
	options.Password.RequireLowercase = false; //·« Ì·“„ √Õ—› ’€Ì—…
	options.Password.RequireUppercase = false; //·« Ì·“„ √Õ—› ﬂ»Ì—…
	options.Lockout.MaxFailedAccessAttempts = 5; // «ﬁ›«· «·Õ”«» »⁄œ 5 „Õ«Ê·«  œŒÊ· ›«‘·…
	options.Lockout.DefaultLockoutTimeSpan =
			 TimeSpan.FromMinutes(15); //«ﬁ›«· «·Õ”«» ·„œ… Œ„”… ⁄‘— œﬁÌﬁ… 
	options.SignIn.RequireConfirmedAccount = true; //  ”ÃÌ· «·œŒÊ· Ì ÿ·»  √ﬂÌœ «·Õ”«»
	options.User.RequireUniqueEmail = false; // staff may have no email; login is by user name
})
	   .AddEntityFrameworkStores<ApplicationDbContext>()
	//»ﬁ«⁄œ… «·»Ì«‰«  ··Ê’Ê· «·Ï «·Ãœ«Ê· Ê«œ—«¡ «·≈” ⁄·«„«  Identity —»ÿ
	.AddDefaultTokenProviders()
	.AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>(); //Ìı” Œœ„ „Ê›— «·—„Ê“ «·≈› —«÷Ì ·≈‰‘«¡ —„Ê“ ≈⁄«œ…  ⁄ÌÌ‰ ﬂ·„… «·„—Ê— Ê €ÌÌ— «·»—Ìœ «·≈·ﬂ —Ê‰Ì Ê≈—”«·Â« ≈·Ï «·»—Ìœ «·≈·ﬂ —Ê‰Ì Ê«·Â« › Ê«·„’«œﬁ… «·À‰«∆Ì… .


//’«·Õ… ≈·« ·„œ… «·œﬁ«∆ﬁ «Ê «·”«⁄«  «·„Õœœ… »⁄œ ≈‰‘«∆Â«//DataProtectorTokenProvider//·‰  ﬂÊ‰ «·—„Ê“ «· Ì Ì „ ≈‰‘«ƒÂ« »Ê«”ÿ… „Ê›— «·—„Ê“
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

	options.AddPolicy("AdminOrProgPolicy", policy => policy.RequireRole("Prog", "Admin")); // «–« «—œ‰« «” ⁄„«· «·—Ê· ﬂ»Ê·Ì’Ì À„ ‰ÿ»ﬁÂ« ›Ì «·ﬂÊ‰ —Ê·— ﬂ»Ê·Ì’Ì


	options.AddPolicy("ProgOrAdminOrEmployeePolicy", policy =>
	{
		policy.RequireClaim(ClaimTypes.Role, "Admin", "Employee", "Prog"); // «–« «—œ‰« «” ⁄„«· «·—Ê· ﬂ»Ê·Ì’Ì À„ ‰ÿ»ﬁÂ« ›Ì «·ﬂÊ‰ —Ê·— ﬂ»Ê·Ì’Ì
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

//appsettings.jason  ”ÃÌ· ﬁ”„ «·≈⁄œ«œ«  «·Œ«’… »«·≈Ì„Ì· Ê«·„ÊÃÊœ ›Ì 
var emailconfig = builder.Configuration
				  .GetSection("MailSettings").Get<MailSettings>();
builder.Services.AddSingleton(emailconfig);

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddMvc(options =>
{
	options.Filters.Add(typeof(AsyncActionFilter));
}).AddXmlSerializerFormatters();
builder.Services.AddHttpContextAccessor(); //AsyncActionFilter ·≈” ⁄„«·Â« ·Ã·» «·ÌÊ“— «·Õ«·Ì ›Ì 


builder.Services.AddSingleton<UserSessionTracker>();

var app = builder.Build();

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
	app.UseDeveloperExceptionPage(); //’›Õ… «·Œÿ√ «·Œ«’… »«·„ÿÊ—


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();//Who are you//·· Õﬁﬁ „‰ ÂÊÌ… «·„” Œœ„° «·«”„ Êﬂ·„… «·„—Ê— Ê«·—„“ «·Œ«’ Ê«·„’«œﬁ… «·À‰«∆Ì…
app.UseAuthorization();//What you allowed to do//·· Õﬁﬁ „‰ ’·«ÕÌ… «·„” Œœ„

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Customers}/{action=Index}/{id?}");




app.Run();
