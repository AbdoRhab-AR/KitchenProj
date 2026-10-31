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
					  Name = "بن قسومة لحلول المطابخ",
					  Activity = "Bin Gsoma Kitchen Solutions Company",
					  About = "شركة بن قسومة لاستيراد الأثاث المنزلي والمكتبي وحلول المطابخ - السراج، طرابلس.",
					  LogoUrl = "/images/brand/logo.png",
					  LetterheadUrl = "/images/brand/letterhead.png",
					  Created = SeedDate
				  });

			modelBuilder.Entity<Contact>().HasData(
			  new Contact()
			  {
				  Id = new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
				  Email = "info@bengsoma.ly",
				  Phone = "00218910997500",
				  Facebook = "",
				  X = "",
				  Instagram = "",
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

			//--------------------Inventory-----------------------------
			// المخزن الرئيسي + بنود الأصناف الجاهزة بأسماء مؤقتة تُعدل من شاشة البنود
			modelBuilder.Entity<Warehouse>().HasData(
				new Warehouse
				{
					Id = new Guid("d3f1c2a4-5b6e-4f70-8a91-0b2c3d4e5f61"),
					Name = "المخزن الرئيسي",
					Created = SeedDate
				});

			modelBuilder.Entity<ItemGroup>().HasData(
				new ItemGroup { Id = new Guid("a1000000-0000-4000-8000-00000000000a"), Code = "A", Name = "البند A", SortOrder = 1, Created = SeedDate },
				new ItemGroup { Id = new Guid("b1000000-0000-4000-8000-00000000000b"), Code = "B", Name = "البند B", SortOrder = 2, Created = SeedDate },
				new ItemGroup { Id = new Guid("c1000000-0000-4000-8000-00000000000c"), Code = "C", Name = "البند C", SortOrder = 3, Created = SeedDate });

			//--------------------Roles-----------------------------
			var progRoleId = AppRoles.ProgRoleId;

			modelBuilder.Entity<IdentityRole>().HasData(
				new IdentityRole
				{
					Id = progRoleId,
					Name = "Prog",
					NormalizedName = "Prog".ToUpper(),
					ConcurrencyStamp = "43dccabf-992f-42c2-8ea0-e023ce5ad8dc"
				}
			);

			// أدوار الموظفين (المدير، المبيعات، المهندس...) - انظر Classes/AppRoles
			modelBuilder.Entity<IdentityRole>().HasData(AppRoles.Seed.Select(r => new IdentityRole
			{
				Id = r.Id,
				Name = r.Name,
				NormalizedName = r.Name.ToUpperInvariant(),
				ConcurrencyStamp = r.Stamp
			}).ToArray());

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

