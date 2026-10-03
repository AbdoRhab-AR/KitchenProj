using System.ComponentModel.DataAnnotations;

namespace KitchenProject.Models.Entities
{
	public class Warehouse : BaseEntity
	{

		[MaxLength(100)]
		public required  string Name { get; set; }

		[MaxLength(250)]
		public string? Location { get; set; }

		public Guid? ManagerId { get; set; }
		public bool IsActive { get; set; } = true;
		public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
	}
}
