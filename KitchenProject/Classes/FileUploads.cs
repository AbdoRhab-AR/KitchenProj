namespace KitchenProject.Classes
{
	public static class FileUploads
	{
		public static readonly string[] Images = { ".jpg", ".jpeg", ".png", ".webp" };
		public static readonly string[] Documents = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };

		// يحفظ ملفاً أو أكثر داخل wwwroot/pic باسم مرتبط بالسجل ({prefix}_{recordId}_{timestamp}_{guid})
		public static async Task<(List<string> Urls, string? Error)> SavePicsAsync(
			IEnumerable<IFormFile>? files, IWebHostEnvironment env, string prefix, Guid recordId, int maxMb = 10)
		{
			var urls = new List<string>();
			if (files == null)
				return (urls, null);

			var list = files.Where(f => f != null && f.Length > 0).ToList();
			if (list.Count == 0)
				return (urls, null);

			foreach (var file in list)
			{
				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				if (!Documents.Contains(ext))
					return (urls, $"نوع الملف ({file.FileName}) غير مسموح. المسموح: {string.Join("، ", Documents)}");

				if (file.Length > maxMb * 1024L * 1024L)
					return (urls, $"حجم الملف ({file.FileName}) أكبر من {maxMb} ميجابايت");
			}

			var dir = Path.Combine(env.WebRootPath, "pic");
			Directory.CreateDirectory(dir);

			var index = 0;
			foreach (var file in list)
			{
				var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
				var shortGuid = Guid.NewGuid().ToString("N")[..8];
				var name = $"{prefix}_{recordId:N}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{index++:D2}_{shortGuid}{ext}";
				using (var stream = new FileStream(Path.Combine(dir, name), FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}
				urls.Add($"/pic/{name}");
			}

			return (urls, null);
		}

		// يجلب روابط كل الصور/المخططات المحفوظة في wwwroot/pic لسجل معين
		public static List<string> GetPics(IWebHostEnvironment env, string prefix, Guid recordId)
		{
			var dir = Path.Combine(env.WebRootPath, "pic");
			if (!Directory.Exists(dir))
				return new List<string>();

			var pattern = $"{prefix}_{recordId:N}_*.*";
			return Directory.GetFiles(dir, pattern)
				.Select(Path.GetFileName)
				.Where(n => !string.IsNullOrEmpty(n))
				.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
				.Select(n => $"/pic/{n}")
				.ToList();
		}

		// يحذف صورة محددة من wwwroot/pic
		public static void DeletePic(string? url, IWebHostEnvironment env)
		{
			if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/pic/", StringComparison.OrdinalIgnoreCase))
				return;

			var picRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "pic"));
			var fileName = Path.GetFileName(url);
			if (string.IsNullOrEmpty(fileName))
				return;

			var path = Path.GetFullPath(Path.Combine(picRoot, fileName));
			if (path.StartsWith(picRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
				File.Delete(path);
		}

		// يحذف جميع الصور المرتبطة بسجل معين من wwwroot/pic عند حذف السجل
		public static void DeleteAllPics(IWebHostEnvironment env, string prefix, Guid recordId)
		{
			foreach (var url in GetPics(env, prefix, recordId))
			{
				DeletePic(url, env);
			}
		}

		// يحفظ الملف داخل wwwroot/uploads/{folder} باسم عشوائي ويرجع رابطه، أو رسالة خطأ
		public static async Task<(string? Url, string? Error)> SaveAsync(
			IFormFile file, IWebHostEnvironment env, string folder, string[] allowedExtensions, int maxMb = 10)
		{
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(ext))
				return (null, $"نوع الملف غير مسموح. المسموح: {string.Join("، ", allowedExtensions)}");

			if (file.Length > maxMb * 1024L * 1024L)
				return (null, $"حجم الملف أكبر من {maxMb} ميجابايت");

			var dir = Path.Combine(env.WebRootPath, "uploads", folder);
			Directory.CreateDirectory(dir);

			var name = $"{Guid.NewGuid():N}{ext}";
			using (var stream = new FileStream(Path.Combine(dir, name), FileMode.Create))
			{
				await file.CopyToAsync(stream);
			}

			return ($"/uploads/{folder}/{name}", null);
		}

		// يحذف ملفاً سبق رفعه (يتجاهل الروابط خارج مجلد uploads)
		public static void Delete(string? url, IWebHostEnvironment env)
		{
			if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/"))
				return;

			var uploadsRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads"));
			var path = Path.GetFullPath(Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
			if (path.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
				File.Delete(path);
		}
	}
}
