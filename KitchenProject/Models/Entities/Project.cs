using KitchenProject.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;

namespace KitchenProject.Models.Entities
{
	public class Project : BaseEntity
	{
		[MaxLength(30)]
		public required string ProjectNo { get; set; }

		[ValidateNever]
		public Customer Customer { get; set; }

		[Required(ErrorMessage = "Customer is required")]
		[Display(Name = "Customers")]
		public Guid CustomerId { get; set; }

		[MaxLength(150)]
		public required string ProjectName { get; set; }

		[MaxLength(100)]
		public string? KitchenType { get; set; }

		public ProjectStatus Status { get; set; }
		[ValidateNever]
		public ApplicationUser? SalesUser { get; set; }
		public string? SalesUserId { get; set; }
		[ValidateNever]
		public ApplicationUser? EngineerUser { get; set; }
		public string? EngineerUserId { get; set; }
		[ValidateNever]
		public ApplicationUser? DesignerUser { get; set; }
		public string? DesignerUserId { get; set; }

		[MaxLength(250)]
		public string? Address { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal EstimatedAmount { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal FinalAmount { get; set; }

		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? StartDate { get; set; }
		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? ExpectedDeliveryDate { get; set; }
		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? DeliveredAt { get; set; }
		[DisplayFormat(DataFormatString = "{0:dd-MM-yyyy hh:mm:ss tt}")]
		public DateTime? ClosedAt { get; set; }

		public string? Notes { get; set; }


		public ICollection<SiteVisit> SiteVisits { get; set; } = new List<SiteVisit>();
		public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
		public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
		public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
		public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
		public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
	}
}
