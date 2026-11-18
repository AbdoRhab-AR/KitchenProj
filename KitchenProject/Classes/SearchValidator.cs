using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace KitchenProject.Classes
{
	/// <summary>
	/// التحقق الأمني الصارم لعمليات البحث بالاسم وحقول الأسماء لمنع الثغرات (SQLi / XSS / Command Injection)
	/// وضمان أن البحث يتم بالاسم فقط دون تنفيذ أي أوامر أو رموز غير مصرح بها.
	/// </summary>
	public static partial class SearchValidator
	{
		public const int MaxSearchLength = 60;

		public const string InvalidSearchMessage =
			"تم إيقاف البحث: يُسمح بالبحث بالاسم فقط (حروف عربية أو إنجليزية فقط) ويُمنع إدخال الأرقام أو الرموز أو الأوامر.";

		public const string InvalidEntityNameMessage =
			"الاسم المدخل يحتوي على رموز أو أوامر غير مسموح بها أمنياً.";

		// البحث بالاسم فقط: حروف عربية وإنجليزية ومسافات فقط (بدون أرقام أو رموز أو علامات ترقيم)
		[GeneratedRegex(@"^[\u0621-\u064A\u066E-\u06D3\u064B-\u065Fa-zA-Z\s]+$", RegexOptions.Compiled)]
		private static partial Regex NameOnlyRegex();

		// أنماط الرموز الخطرة أو محاولات الحقن
		[GeneratedRegex(@"[<>'""`;=\\|&${}\[\]^~%*+!?@#]|--|/\*|\*/|0x[0-9a-fA-F]+", RegexOptions.Compiled)]
		private static partial Regex DangerousSymbolsRegex();

		// الكلمات المحجوزة والأوامر البرمجية (SQL / XSS / Shell) التي يُمنع تنفيذها أو البحث بها
		private static readonly HashSet<string> BlockedCommandKeywords = new(StringComparer.OrdinalIgnoreCase)
		{
			// SQL Injection Keywords
			"select", "insert", "update", "delete", "drop", "alter", "create", "truncate",
			"exec", "execute", "union", "declare", "cast", "convert", "waitfor", "delay",
			"benchmark", "sleep", "sysobjects", "syscolumns", "information_schema",
			"xp_cmdshell", "sp_executesql", "table", "database", "schema", "grant", "revoke",
			"having", "group", "order", "where", "from", "into", "values", "join", "inner", "outer",
			// XSS & Script Injection Keywords
			"script", "javascript", "vbscript", "expression", "onload", "onerror", "onclick",
			"onmouseover", "onfocus", "onblur", "onsubmit", "onchange", "oninput", "onkeydown",
			"onkeyup", "eval", "alert", "prompt", "confirm", "document", "window", "cookie",
			"localstorage", "sessionstorage", "location", "history", "navigator", "fetch",
			"xmlhttprequest", "iframe", "object", "embed", "svg", "style", "meta", "base",
			// OS / Shell Commands
			"cmd", "powershell", "pwsh", "bash", "sh", "zsh", "curl", "wget", "netcat", "nmap",
			"whoami", "chmod", "chown", "sudo", "erase", "format", "shutdown", "reboot",
			"taskkill", "ping", "tracert", "nslookup", "system", "invoke"
		};

		/// <summary>
		/// يتحقق من أن نص البحث يتكون من اسم صالح فقط (حروف عربية أو إنجليزية ومسافات) وخالٍ تماماً من الأوامر أو الثغرات.
		/// </summary>
		public static bool TryValidateNameSearch(string? input, out string cleanName, out string? errorMessage)
		{
			cleanName = string.Empty;
			errorMessage = null;

			if (string.IsNullOrWhiteSpace(input))
			{
				return true;
			}

			var trimmed = Regex.Replace(input.Trim(), @"\s+", " ");

			if (trimmed.Length > MaxSearchLength)
			{
				errorMessage = InvalidSearchMessage;
				return false;
			}

			if (DangerousSymbolsRegex().IsMatch(trimmed) || !NameOnlyRegex().IsMatch(trimmed))
			{
				errorMessage = InvalidSearchMessage;
				return false;
			}

			// فحص كل كلمة للتأكد من عدم وجود أي أمر برمجي مكتوب بالحروف
			var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			foreach (var token in tokens)
			{
				if (BlockedCommandKeywords.Contains(token))
				{
					errorMessage = InvalidSearchMessage;
					return false;
				}
			}

			cleanName = trimmed;
			return true;
		}

		/// <summary>
		/// يتحقق من سلامة حقول الأسماء عند الحفظ لمنع إدخال أكواد خبيثة أو أوامر.
		/// </summary>
		public static bool IsSafeEntityName(string? input, bool strictlyLettersOnly = false)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return true;
			}

			var trimmed = input.Trim();
			if (DangerousSymbolsRegex().IsMatch(trimmed))
			{
				return false;
			}

			if (strictlyLettersOnly && !NameOnlyRegex().IsMatch(trimmed))
			{
				return false;
			}

			var tokens = Regex.Split(trimmed, @"[\s\-./()]+")
							  .Where(t => !string.IsNullOrWhiteSpace(t));
			foreach (var token in tokens)
			{
				if (BlockedCommandKeywords.Contains(token))
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	/// سمة تحقق لحقول الأسماء في النماذج لمنع حفظ أي أوامر أو رموز خبيثة.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public sealed class SafeNameAttribute : ValidationAttribute
	{
		public bool StrictlyLettersOnly { get; set; }

		public SafeNameAttribute(bool strictlyLettersOnly = false)
		{
			StrictlyLettersOnly = strictlyLettersOnly;
			ErrorMessage = strictlyLettersOnly
				? "يجب أن يحتوي الاسم على حروف عربية أو إنجليزية فقط بدون أرقام أو رموز أو أوامر"
				: SearchValidator.InvalidEntityNameMessage;
		}

		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			if (value is string str && !string.IsNullOrWhiteSpace(str))
			{
				if (!SearchValidator.IsSafeEntityName(str, StrictlyLettersOnly))
				{
					return new ValidationResult(ErrorMessage,
						validationContext.MemberName != null ? new[] { validationContext.MemberName } : null);
				}
			}
			return ValidationResult.Success;
		}
	}
}
