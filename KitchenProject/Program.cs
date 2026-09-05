using KitchenProject.Classes;
using KitchenProject.Models;
using KitchenProject.Models.Entities;
using KitchenProject.Models.Interfaces;
using KitchenProject.Models.Repositories;
using KitchenProject.Models.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();


builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

//-------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>
	(x => x.UseSqlServer(builder.Configuration.GetConnectionString("DbCon")));
//-------------------------------------------------

builder.Services.Configure<ProgrammerSettings>
	(builder.Configuration.GetSection("ProgrammerSettings"));


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 3; // ÇáØæá ÇáÃÏäì 3 ÃÍÑİ
	options.Password.RequiredUniqueChars = 1; // ÍÑİ İÑíÏ æÇÍÏ Úáì ÇáÃŞá
	options.Password.RequireNonAlphanumeric = false; // ÚäÏ ÊİÚíáåÇ ÓÊÊØáÈ ßáãÇÊ ÇáãÑæÑ ÑãÒ ÎÇÕ æÇÍÏ Úáì ÇáÃŞá
	options.Password.RequireDigit = true; //íÌÈ Ãä ÊÍÊæí Úáì ÑŞã
	options.Password.RequireLowercase = false; //áÇ íáÒã ÃÍÑİ ÕÛíÑÉ
	options.Password.RequireUppercase = false; //áÇ íáÒã ÃÍÑİ ßÈíÑÉ
	options.Lockout.MaxFailedAccessAttempts = 5; // ÇŞİÇá ÇáÍÓÇÈ ÈÚÏ 5 ãÍÇæáÇÊ ÏÎæá İÇÔáÉ
	options.Lockout.DefaultLockoutTimeSpan =
			 TimeSpan.FromMinutes(1); //ÇŞİÇá ÇáÍÓÇÈ áãÏÉ ÎãÓÉ ÚÔÑ ÏŞíŞÉ 
	options.SignIn.RequireConfirmedAccount = true; // ÊÓÌíá ÇáÏÎæá íÊØáÈ ÊÃßíÏ ÇáÍÓÇÈ
	options.User.RequireUniqueEmail = true; // ÚäÏ ÇäÔÇÁ ÇáãÓÊÎÏã íÌÈ ÇÓÊÎÏÇã ÈÑíÏ ÅáßÊÑæäí İÑíÏ
})
	   .AddEntityFrameworkStores<ApplicationDbContext>()
	//ÈŞÇÚÏÉ ÇáÈíÇäÇÊ ááæÕæá Çáì ÇáÌÏÇæá æÇÏÑÇÁ ÇáÅÓÊÚáÇãÇÊ Identity ÑÈØ
	.AddDefaultTokenProviders(); //íõÓÊÎÏã ãæİÑ ÇáÑãæÒ ÇáÅİÊÑÇÖí áÅäÔÇÁ ÑãæÒ ÅÚÇÏÉ ÊÚííä ßáãÉ ÇáãÑæÑ æÊÛííÑ ÇáÈÑíÏ ÇáÅáßÊÑæäí æÅÑÓÇáåÇ Åáì ÇáÈÑíÏ ÇáÅáßÊÑæäí æÇáåÇÊİ æÇáãÕÇÏŞÉ ÇáËäÇÆíÉ .


//ÕÇáÍÉ ÅáÇ áãÏÉ ÇáÏŞÇÆŞ Çæ ÇáÓÇÚÇÊ ÇáãÍÏÏÉ ÈÚÏ ÅäÔÇÆåÇ//DataProtectorTokenProvider//áä Êßæä ÇáÑãæÒ ÇáÊí íÊã ÅäÔÇÄåÇ ÈæÇÓØÉ ãæİÑ ÇáÑãæÒ
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
							   options.TokenLifespan = TimeSpan.FromMinutes(15));
//opt.TokenLifespan = TimeSpan.FromDays(1));


//-------------------------------------------------


builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("CreatePolicy", policy => policy.RequireClaim("Create", "true"));
	options.AddPolicy("EditPolicy", policy => policy.RequireClaim("Edit", "true"));
	options.AddPolicy("DeletePolicy", policy => policy.RequireClaim("Delete", "true"));

	options.AddPolicy("EditUserPolicy", policy => policy.RequireClaim("EditUser", "true"));
	options.AddPolicy("SiteStatePolicy", policy => policy.RequireClaim("SiteState", "true"));
	//options.AddPolicy("ProfilePolicy", policy => policy.RequireClaim("UserProfile", "true")); // just example

	options.AddPolicy("AdminOrProgPolicy", policy => policy.RequireRole("Prog", "Admin")); // ÇĞÇ ÇÑÏäÇ ÇÓÊÚãÇá ÇáÑæá ßÈæáíÕí Ëã äØÈŞåÇ İí ÇáßæäÊÑæáÑ ßÈæáíÕí


	options.AddPolicy("ProgOrAdminOrEmployeePolicy", policy =>
	{
		policy.RequireClaim(ClaimTypes.Role, "Admin", "Employee", "Prog"); // ÇĞÇ ÇÑÏäÇ ÇÓÊÚãÇá ÇáÑæá ßÈæáíÕí Ëã äØÈŞåÇ İí ÇáßæäÊÑæáÑ ßÈæáíÕí
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

//appsettings.jason ÊÓÌíá ŞÓã ÇáÅÚÏÇÏÇÊ ÇáÎÇÕÉ ÈÇáÅíãíá æÇáãæÌæÏ İí 
var emailconfig = builder.Configuration
				  .GetSection("MailSettings").Get<MailSettings>();
builder.Services.AddSingleton(emailconfig);

builder.Services.AddScoped<IEmailSender, EmailSender>();


builder.Services.AddMvc(options =>
{
	options.Filters.Add(typeof(AsyncActionFilter));
}).AddXmlSerializerFormatters();
builder.Services.AddHttpContextAccessor(); //AsyncActionFilter áÅÓÊÚãÇáåÇ áÌáÈ ÇáíæÒÑ ÇáÍÇáí İí 


builder.Services.AddSingleton<UserSessionTracker>();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseStatusCodePagesWithReExecute("/Error/{0}");
}
else if (app.Environment.IsDevelopment())
	app.UseDeveloperExceptionPage(); //ÕİÍÉ ÇáÎØÃ ÇáÎÇÕÉ ÈÇáãØæÑ


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();//Who are you//ááÊÍŞŞ ãä åæíÉ ÇáãÓÊÎÏã¡ ÇáÇÓã æßáãÉ ÇáãÑæÑ æÇáÑãÒ ÇáÎÇÕ æÇáãÕÇÏŞÉ ÇáËäÇÆíÉ
app.UseAuthorization();//What you allowed to do//ááÊÍŞŞ ãä ÕáÇÍíÉ ÇáãÓÊÎÏã

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Customers}/{action=Index}/{id?}");




app.Run();
