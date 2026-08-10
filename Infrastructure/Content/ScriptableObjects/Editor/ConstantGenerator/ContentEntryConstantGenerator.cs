using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sapientia.Extensions;
using Sapientia.Pooling;
using UnityEditor;

namespace Content.ScriptableObjects.Editor
{
	// В отличие от ContentConstantGenerator (который по [Constants] на типе генерит константы
	// по Id самих ассетов) — этот генерит константы по СОДЕРЖИМОМУ одного ассета: что именно,
	// решает сам ассет через ContentScriptableObject.EnumerateConstants
	public static class ContentEntryConstantGenerator
	{
		private const string DEFAULT_NAMESPACE = "Content.Constants";
		private const string DEFAULT_OUTPUT_PATH = "Assets/Scripts/Content/Constants";
		private const string GENERATED_POSTFIX = "Generated";

		public static void Generate(ContentScriptableObject scriptableObject, string id)
		{
			if (!scriptableObject.UseConstants)
				return;

			ref var settings = ref scriptableObject.ConstantsSettings;
			if (!settings.useConstants)
				return;

			var entries = scriptableObject.EnumerateConstants()?.ToList();
			if (entries == null || entries.Count == 0)
			{
				ContentDebug.LogWarning("Nothing to generate: no constants", scriptableObject);
				return;
			}

			if (!Write(id, entries, settings, scriptableObject))
				return;

			settings.generatedHash = ComputeHash(id, entries, settings);
			EditorUtility.SetDirty(scriptableObject);
			AssetDatabase.SaveAssetIfDirty(scriptableObject);
		}

		public static bool IsDirty(ContentScriptableObject scriptableObject, string id)
		{
			if (!scriptableObject.UseConstants)
				return false;

			ref var settings = ref scriptableObject.ConstantsSettings;
			if (!settings.useConstants)
				return false;

			var entries = scriptableObject.EnumerateConstants()?.ToList();
			if (entries == null || entries.Count == 0)
				return false;

			return settings.generatedHash != ComputeHash(id, entries, settings);
		}

		/// <summary>
		/// Слепок того, что влияет на содержимое генерируемого файла: сами константы и настройки
		/// вывода. Расходится с сохранённым — значит файл устарел
		/// </summary>
		private static string ComputeHash(string id, List<ContentConstantEntry> entries,
			in ContentConstantsSettings settings)
		{
			using var _ = StringBuilderPool.Get(out var sb);

			sb.Append(id).Append('|')
				.Append(settings.className).Append('|')
				.Append(settings.@namespace).Append('|')
				.Append(settings.outputPath).Append('|');

			foreach (var entry in entries.OrderBy(x => x.name, StringComparer.Ordinal))
				sb.Append(entry.name).Append('=').Append(entry.value).Append(';');

			using var md5 = MD5.Create();
			var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
			return BitConverter.ToString(bytes).Replace("-", string.Empty);
		}

		private static bool Write(string id, List<ContentConstantEntry> entries,
			in ContentConstantsSettings settings, ContentScriptableObject context)
		{
			var className = settings.className.IsNullOrEmpty() ? $"{id}Keys" : settings.className;
			var @namespace = settings.@namespace.IsNullOrEmpty() ? DEFAULT_NAMESPACE : settings.@namespace;
			var folderPath = settings.outputPath.IsNullOrEmpty() ? DEFAULT_OUTPUT_PATH : settings.outputPath;

			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			var path = Path.Combine(folderPath, $"{className}.{GENERATED_POSTFIX}.cs");
			var existingData = File.Exists(path) ? File.ReadAllText(path) : null;

			var classDeclaration = SyntaxFactory.ClassDeclaration(className)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

			var usedNames = new HashSet<string>(StringComparer.Ordinal);
			string lastGroup = null;

			foreach (var entry in entries.OrderBy(x => x.groupPath ?? string.Empty, StringComparer.Ordinal)
						 .ThenBy(x => x.name, StringComparer.Ordinal))
			{
				var name = ToConstantName(entry.name);
				if (!usedNames.Add(name))
				{
					// Разные исходные строки могут схлопнуться в одно имя ("Base Hands"/"base_hands")
					var unique = name;
					for (var i = 2; !usedNames.Add(unique); i++)
						unique = $"{name}_{i}";
					name = unique;
				}

				var isString = entry.value is not int;
				TypeSyntax typeSyntax = SyntaxFactory.PredefinedType(
					SyntaxFactory.Token(isString ? SyntaxKind.StringKeyword : SyntaxKind.IntKeyword));

				var literal = isString
					? SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
						SyntaxFactory.Literal(entry.value?.ToString() ?? string.Empty))
					: SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
						SyntaxFactory.Literal((int) entry.value));

				var fieldDeclaration = SyntaxFactory.FieldDeclaration(
						SyntaxFactory.VariableDeclaration(typeSyntax)
							.AddVariables(SyntaxFactory.VariableDeclarator(name)
								.WithInitializer(SyntaxFactory.EqualsValueClause(literal))))
					.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword));

				var trivia = new List<SyntaxTrivia>();
				if (!entry.groupPath.IsNullOrEmpty() && entry.groupPath != lastGroup)
					trivia.Add(SyntaxFactory.Comment($"// {entry.groupPath}"));
				lastGroup = entry.groupPath;

				if (!entry.summary.IsNullOrWhiteSpace())
					trivia.Add(SyntaxFactory.Comment($"/// <summary>{SecurityElement.Escape(entry.summary)}</summary>"));

				if (trivia.Count > 0)
					fieldDeclaration = fieldDeclaration.WithLeadingTrivia(trivia);

				classDeclaration = classDeclaration.AddMembers(fieldDeclaration);
			}

			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
				.NormalizeWhitespace()
				.WithLeadingTrivia(SyntaxFactory.Comment("// <auto-generated/>"))
				.AddMembers(classDeclaration);

			var code = SyntaxFactory.CompilationUnit()
				.AddMembers(namespaceDeclaration)
				.NormalizeWhitespace(indentation: "\t", eol: "\n")
				.ToFullString();

			var unityPath = "Assets/" + path.Split("Assets/")[^1];

			if (existingData == code)
			{
				ContentDebug.Log($"Unchanged constants: {unityPath}", context);
				return true;
			}

			File.WriteAllText(path, code, new UTF8Encoding(false));
			AssetDatabase.ImportAsset(unityPath);
			ContentDebug.Log($"{(existingData == null ? "Generated" : "Updated")} constants: {unityPath}", context);
			return true;
		}

		private static string ToConstantName(string input)
		{
			if (input.IsNullOrEmpty())
				return string.Empty;

			using var _ = StringBuilderPool.Get(out var sb);

			for (var i = 0; i < input.Length; i++)
			{
				var current = input[i];
				var next = i + 1 < input.Length ? input[i + 1] : (char?) null;

				if (char.IsUpper(current))
				{
					if (i > 0 && (char.IsLower(input[i - 1]) || (next.HasValue && char.IsLower(next.Value))))
						sb.Append('_');
					sb.Append(current);
				}
				else if (char.IsDigit(current))
				{
					if (i > 0 && char.IsLetter(input[i - 1]))
						sb.Append('_');
					sb.Append(current);
				}
				else if (char.IsLetter(current))
				{
					sb.Append(char.ToUpperInvariant(current));
				}
				else
				{
					sb.Append('_');
				}
			}

			var result = sb.ToString();
			while (result.Contains("__"))
				result = result.Replace("__", "_");
			result = result.Trim('_');

			if (result.Length > 0 && char.IsDigit(result[0]))
				result = "_" + result;

			return result;
		}
	}
}
