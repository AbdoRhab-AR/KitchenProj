using KitchenProject.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KitchenProject.Models
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		private readonly IServiceProvider _serviceProvider;

		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options
									, IServiceProvider serviceProvider) : base(options)
		{
			_serviceProvider = serviceProvider;
		}

		public DbSet<Contact> Contact { get; set; }
		public DbSet<Customer> Customers { get; set; }
		public DbSet<Project> Projects { get; set; }
		public DbSet<SiteVisit> SiteVisits { get; set; }
		public DbSet<SiteInfo> SiteInfo { get; set; }
		public DbSet<SiteState> SiteState { get; set; }
		public DbSet<Quote> Quotes { get; set; }
		public DbSet<QuoteItem> QuoteItems { get; set; }
		public DbSet<Contract> Contracts { get; set; }
		public DbSet<CustomerPayment> CustomerPayments { get; set; }
		public DbSet<Expense> Expenses { get; set; }
		public DbSet<Supplier> Suppliers { get; set; }
		public DbSet<Item> Items { get; set; }
		public DbSet<ItemGroup> ItemGroups { get; set; }
		public DbSet<Warehouse> Warehouses { get; set; }
		public DbSet<StockTransaction> StockTransactions { get; set; }
		public DbSet<Purchase> Purchases { get; set; }
		public DbSet<PurchaseItem> PurchaseItems { get; set; }
		public DbSet<SupplierPayment> SupplierPayments { get; set; }
		public DbSet<MaterialRequest> MaterialRequests { get; set; }
		public DbSet<MaterialRequestItem> MaterialRequestItems { get; set; }
		public DbSet<UserProfile> UserProfiles { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Contact>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Customer>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Project>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<SiteVisit>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<SiteInfo>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<SiteState>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Quote>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<QuoteItem>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Contract>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<CustomerPayment>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Expense>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Supplier>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Item>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<ItemGroup>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Warehouse>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<StockTransaction>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<Purchase>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<PurchaseItem>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<SupplierPayment>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<MaterialRequest>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<MaterialRequestItem>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Entity<UserProfile>().Property(x => x.Id).HasDefaultValueSql("NEWID()");
			modelBuilder.Seed(_serviceProvider);


			modelBuilder.Entity<ApplicationUser>()
			 .HasOne(a => a.UserProfile)
			 .WithOne(b => b.ApplicationUser)
			 .HasForeignKey<UserProfile>(b => b.UserId)
			 .OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Project>()
		   .HasOne(x => x.Customer)
		   .WithMany(x => x.Projects)
		   .HasForeignKey(x => x.CustomerId)
		   .OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Project>()
				.HasOne(x => x.SalesUser)
				.WithMany(x => x.SalesProjects)
				.HasForeignKey(x => x.SalesUserId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Project>()
				.HasOne(x => x.EngineerUser)
				.WithMany(x => x.EngineerProjects)
				.HasForeignKey(x => x.EngineerUserId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Project>()
				.HasOne(x => x.DesignerUser)
				.WithMany(x => x.DesignerProjects)
				.HasForeignKey(x => x.DesignerUserId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<SiteVisit>()
				.HasOne(x => x.Project).WithMany(x => x.SiteVisits)
				.HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<SiteVisit>()
				.HasOne(x => x.Engineer).WithMany()
				.HasForeignKey(x => x.EngineerId).OnDelete(DeleteBehavior.Restrict);
		
			modelBuilder.Entity<Contract>()
				.HasOne(x => x.Projects).WithMany(x => x.Contracts)
				.HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<QuoteItem>()
				.HasOne(x => x.Quote).WithMany(x => x.Items)
				.HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Contract>()
				.HasOne(x => x.Quote).WithMany()
				.HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<CustomerPayment>()
				.HasOne(x => x.Project).WithMany(x => x.CustomerPayments)
				.HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<CustomerPayment>()
				.HasOne(x => x.Contract).WithMany(x => x.CustomerPayments)
				.HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<PurchaseItem>()
				.HasOne(x => x.Purchases).WithMany(x => x.Items)
				.HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<PurchaseItem>()
				.HasOne(x => x.Items).WithMany(x => x.PurchaseItems)
				.HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StockTransaction>()
				.HasOne(x => x.Items).WithMany(x => x.StockTransactions)
				.HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StockTransaction>()
				.HasOne(x => x.Warehouses).WithMany(x => x.StockTransactions)
				.HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StockTransaction>()
				.HasOne(x => x.Projects).WithMany(x => x.StockTransactions)
				.HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StockTransaction>()
				.HasOne(x => x.Purchase).WithMany()
				.HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Item>()
				.HasOne(x => x.Group).WithMany(x => x.Items)
				.HasForeignKey(x => x.ItemGroupId).OnDelete(DeleteBehavior.Restrict);

			// طلبات المواد الداخلية: لا يُحذف مشروع أو مخزن أو مستخدم مرتبط بطلب
			modelBuilder.Entity<MaterialRequest>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MaterialRequest>().HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MaterialRequest>().HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MaterialRequest>().HasOne(x => x.DecidedBy).WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MaterialRequest>().HasOne(x => x.Purchase).WithMany().HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.SetNull);
			modelBuilder.Entity<MaterialRequest>().HasIndex(x => x.RequestNo).IsUnique();
			modelBuilder.Entity<MaterialRequestItem>().HasOne(x => x.MaterialRequest).WithMany(x => x.Items).HasForeignKey(x => x.MaterialRequestId).OnDelete(DeleteBehavior.Cascade);
			modelBuilder.Entity<MaterialRequestItem>().HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Item>().HasIndex(x => x.Code).IsUnique();
			modelBuilder.Entity<ItemGroup>().HasIndex(x => x.Code).IsUnique();

			modelBuilder.Entity<QuoteItem>()
				.HasOne(x => x.Item).WithMany()
				.HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);

			modelBuilder.Entity<Purchase>()
				.HasOne(x => x.Suppliers).WithMany(x => x.Purchases)
				.HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Purchase>()
				.HasOne(x => x.Warehouses).WithMany()
				.HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Purchase>()
				.HasOne(x => x.Projects).WithMany()
				.HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<SupplierPayment>()
				.HasOne(x => x.supplier).WithMany(x => x.Payments)
				.HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<SupplierPayment>()
				.HasOne(x => x.Purchase).WithMany(x => x.Payments)
				.HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Restrict);
		}
	}
}
