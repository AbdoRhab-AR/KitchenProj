using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KitchenProject.Models.Entities
{
	public class Item :BaseEntity
	{
		[ MaxLength(30)]
		public required string Code { get; set; }

		[MaxLength(150)]
		public required string Name { get; set; } 

		[MaxLength(100)]
		public string? Category { get; set; }

		[MaxLength(30)]
		public string Unit { get; set; } = "Piece";

		[Column(TypeName = "decimal(18,2)")]
		public decimal PurchasePrice { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal SalePrice { get; set; }

		[Column(TypeName = "decimal(18,2)")]
		public decimal MinQuantity { get; set; }

		public bool IsActive { get; set; } = true;
		public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
		public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
	}
}
