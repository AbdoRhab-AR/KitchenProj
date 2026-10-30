using KitchenProject.Classes;
using KitchenProject.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace KitchenProject.Models
{
    public static class ModelBuilderExtensions
    {
		// HasData values must be constant, otherwise every new migration deletes and re-inserts the seed rows
		private static readonly DateTime SeedDate = new DateTime(2026, 9, 5, 13, 1, 21, DateTimeKind.Utc);

		public static void Seed(this ModelBuilder modelBuilder
										  , IServiceProvider serviceProvider)
		{
			modelBuilder.Entity<SiteInfo>().HasData(
				  new SiteInfo()
				  {
					  Id = new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
					  Name = "بن قسومة للمطابخ",
					  Activity = "News site",
					  About = "We are a specialized news website covering political, sports, and economic events, along with various other sections of general interest to readers. We always strive to provide distinguished and reliable content that reflects the ongoing developments on both the local and global stages.",
					  Created = SeedDate
				  });

			modelBuilder.Entity<Contact>().HasData(
			  new Contact()
			  {
				  Id = new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
				  Email = "W.Wide@Gmail.com",
				  Phone = "00218951234567",
				  Facebook = "Worldwide Facebook",
				  X = "Worldwide Twitter",
				  Instagram = "Worldwide Instagram",
				  Created = SeedDate

			  });
			modelBuilder.Entity<SiteState>().HasData(
			 new SiteState()
			 {
				 Id = new Guid("7b2c5bf8-20f1-4956-abb6-e8821e8ce725"),
				 State = true,
				 ClosingMessage = "The site is temporarily closed for development",
				 Created = SeedDate

			 });

			//--------------------Roles-----------------------------
			var progRoleId = "109400b7-ee44-4b6c-bef8-0cc59de1543e";

			modelBuilder.Entity<IdentityRole>().HasData(
				new IdentityRole
				{
					Id = progRoleId,
					Name = "Prog",
					NormalizedName = "Prog".ToUpper(),
					ConcurrencyStamp = "43dccabf-992f-42c2-8ea0-e023ce5ad8dc"
				}
			);

			//-------------------Programmer-----------------------------

			string programmerId = "6068e8a9-d170-4fa6-abba-bed99e9ef5da";
			//string programmerName = "Programmer@Gmail.com";

			var programmerSettings = serviceProvider.
							 GetRequiredService<IOptions<ProgrammerSettings>>().Value;
			string programmerName = programmerSettings.ProgrammerName;

			var Programmer = new ApplicationUser
			{
				Id = programmerId,
				UserName = programmerName,
				NormalizedUserName = programmerName.ToUpper(),
				Email = programmerName,
				NormalizedEmail = programmerName.ToUpper(),
				EmailConfirmed = true,
				PhoneNumberConfirmed = true,
				LockoutEnabled = true,
				SecurityStamp = "04500de6-96fe-4ad7-96dd-fcca96e369d7",
				ConcurrencyStamp = "50286ee0-8cab-4fd9-b86d-4c9e7d81d85b",
				CreatedDate = SeedDate,
				Age = 0,
				// الباس الافتراضي Prog@123 (hash ثابت لأن PasswordHasher يولد salt مختلف كل مرة)
				PasswordHash = "AQAAAAIAAYagAAAAEOiMVW5LwaXnP8KCeBDAECPBrxqGZdZDi2qpMwjKPFc0X+YlPL+CABdS42ebB7WrSw=="
			};

			modelBuilder.Entity<ApplicationUser>().HasData(Programmer);


			//-------------Add User to Role--------------------
			modelBuilder.Entity<IdentityUserRole<string>>().HasData(
				new IdentityUserRole<string>
				{
					RoleId = progRoleId,
					UserId = programmerId
				});
			//-----------------------------------------------------
		}

	}
}

