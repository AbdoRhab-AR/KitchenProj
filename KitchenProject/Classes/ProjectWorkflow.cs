using KitchenProject.Models.Entities;
using KitchenProject.Models.Enums;
using KitchenProject.Models.Interfaces;

namespace KitchenProject.Classes
{
	public static class ProjectWorkflow
	{
		// ينقل المشروع للمرحلة المطلوبة إن كان في مرحلة أقدم منها فقط
		// (لا يرجع المشروع للخلف، ولا يلمس المشاريع الملغاة لأن قيمة Cancelled أكبر من كل المراحل)
		// الحفظ يتم مع SaveAsync الخاص بالعملية الأصلية لأن كل الـ UnitOfWork تشترك في نفس الـ DbContext
		public static async Task AdvanceAsync(IUnitOfWork<Project> projects, Guid projectId, ProjectStatus to)
		{
			var project = await projects.Entity.GetByIdAsync(projectId);
			if (project != null && project.Status < to)
			{
				project.Status = to;
				project.Modified = DateTime.UtcNow;

				if (to == ProjectStatus.InProgress && project.StartDate == null)
					project.StartDate = DateTime.UtcNow;
			}
		}
	}
}
