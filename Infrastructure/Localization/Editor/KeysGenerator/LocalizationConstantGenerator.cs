using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEditor;
using UnityEngine;

namespace Localization.Editor
{
	public static partial class LocalizationConstantGenerator
	{
		private const string CLASS_NAME = "LocKeys";

		private const string ROOT_NODE = "ROOT";
		private const string CONSTANTS_NAME = "Constants";

		internal static void Generate(IEnumerable<string> keys)
		{
			if (keys == null || !keys.Any())
				return;

			var className = CLASS_NAME;

			var folderPath = projectSettings.folderPath;
			var rootName = "Localization";
			var asmdefName = $"{rootName}.{CONSTANTS_NAME}.asmdef";
			var asmdefFilePath = Path.Combine(folderPath, asmdefName);
			var asmdefFileInfo = new FileInfo(asmdefFilePath);
			if (!asmdefFileInfo.Exists)
			{
				var constantsAsmdefPath = asmdefFileInfo.FullName;
				var text = $@"
					{{
						""name"": ""{rootName}.{CONSTANTS_NAME}"",
						""rootNamespace"": """",
						""references"": [],
						""includePlatforms"": [],
						""excludePlatforms"": [],
						""allowUnsafeCode"": false,
						""overrideReferences"": false,
						""precompiledReferences"": [],
						""autoReferenced"": true,
						""defineConstraints"": [],
						""versionDefines"": [],
						""noEngineReferences"": true
					}}";

				var directory = Path.GetDirectoryName(constantsAsmdefPath);

				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				File.WriteAllText(constantsAsmdefPath, text);
				AssetDatabase.ImportAsset(constantsAsmdefPath);
				var asmdefAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(constantsAsmdefPath);
				LocalizationDebug.Log($"Generated .asmdef: {constantsAsmdefPath}", asmdefAsset);
			}

			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			var fileName = $"{className}.{projectSettings.scriptFileNamePostfix}.cs";
			var path = Path.Combine(folderPath, fileName);

			var existingData = File.Exists(path) ? File.ReadAllText(path) : null;

			var root = new GeneratorConstantsNode {name = ROOT_NODE};

			foreach (var key in keys)
			{
				if (key.Contains("\n"))
				{
					LocalizationDebug.LogError($"Key [ {key} ] contains new line...");
					continue;
				}

				if (Enumerable.Any(projectSettings.skipTags, tag => key.Contains(tag)))
					continue;

				var parts = key.Split(projectSettings.keySeparator);
				var current = root;

				foreach (var part in parts)
				{
					if (!current.children.ContainsKey(part))
						current.children[part] = new GeneratorConstantsNode {name = part};
					current = current.children[part];
				}

				current.fullPath = key;
			}

			var compilationUnit = SyntaxFactory.CompilationUnit();

			var namespaceName = $"{rootName}.{CONSTANTS_NAME}";

			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
			   .NormalizeWhitespace()
			   .WithLeadingTrivia(SyntaxFactory.Comment(projectSettings.scriptComment));

			var classDeclaration = SyntaxFactory.ClassDeclaration(className)
			   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

			var needSpace = false;
			AddConstants(root, new List<string>(), ref needSpace);

			namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);
			compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

			const string NEW_LINE_TAG = "{NEW_LINE}";
			var code = compilationUnit
			   .NormalizeWhitespace(indentation: "	", eol: "\n")
			   .ToFullString();
			code = code.Replace(NEW_LINE_TAG, string.Empty);

			if (existingData == code)
				return;

			using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
			{
				writer.NewLine = "\n";
				writer.Write(code);
			}

			var separator = "Assets/";
			var unityPath = separator + path.Split(separator)[^1];
			AssetDatabase.ImportAsset(unityPath);

			var textAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(unityPath);
			var prefix = existingData == null ? "Generated" : "Updated";
			LocalizationDebug.Log($"{prefix} constants: {unityPath}", textAsset);

			void AddConstants(GeneratorConstantsNode node, List<string> pathSegments, ref bool space)
			{
				var hasAnyLeaves = node.children.Values.Any(HasLeaf);

				var currentPath = new List<string>(pathSegments) {node.name};
				var handled = false;

				foreach (var child in node.children.Values.OrderBy(c => c.name))
				{
					if (child.children.Count != 0 || child.fullPath == null)
						continue;

					var constName = GetName(child.fullPath);
					var fieldDeclaration = SyntaxFactory.FieldDeclaration(
							SyntaxFactory.VariableDeclaration(
									SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
							   .AddVariables(
									SyntaxFactory.VariableDeclarator(constName)
									   .WithInitializer(
											SyntaxFactory.EqualsValueClause(
												SyntaxFactory.LiteralExpression(
													SyntaxKind.StringLiteralExpression,
													SyntaxFactory.Literal(child.fullPath))))))
					   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword));

					using (ListPool<SyntaxTrivia>.Get(out var trivia))
					{
						if (!handled && hasAnyLeaves)
						{
							if (space)
								trivia.Add(SyntaxFactory.Comment(NEW_LINE_TAG));

							space = false;
							handled = true;
						}

						using (StringBuilderPool.Get(out var builder))
						{
							builder.Append("/// <summary>");
							builder.Append("\n");
							var several = projectSettings.locales.Length > 1;
							if (several)
							{
								builder.Append("		/// Translations:");
								builder.Append(" <br/>\n");
							}

							foreach (var (locale, index) in projectSettings.locales.WithIndex())
							{
								var comment = string.Empty;

								if (index != 0)
									comment += " <br/>\n";

								comment += "		/// <c>";
								if (several)
									comment += $"[{locale}]	";

								var translation = LocManager.GetEditor(child.fullPath, locale)?.Replace("\n", "\n		/// ");

								if (translation != null)
									translation = FixXmlTags(translation) + "</c>";
								else
									LocalizationDebug.LogError(
										$"Empty translation for key [ {child.fullPath} ] in locale by code [ {locale} ]");
								comment += translation;
								builder.Append(comment);

								string FixXmlTags(string text)
								{
									return Regex.Replace(
										text,
										@"<(/?)(?!/?(para|see|c|b)\b)[^>]*>",
										m => SecurityElement.Escape(m.Value) // &lt;...&gt;
									);
								}
							}

							builder.Append("\n");
							builder.Append("		/// </summary>");
							var syntaxTriviaComment = SyntaxFactory.Comment(builder.ToString());
							trivia.Add(syntaxTriviaComment);
						}

						fieldDeclaration = fieldDeclaration.WithLeadingTrivia(trivia);
					}

					classDeclaration = classDeclaration.AddMembers(fieldDeclaration);

					space = true;
				}

				foreach (var child in node.children.Values.OrderBy(c => c.name))
				{
					if (child.children.Count > 0)
						AddConstants(child, currentPath, ref space);
				}
			}

			bool HasLeaf(GeneratorConstantsNode c)
			{
				if (c.fullPath != null)
					return true;

				return c.children.Values.Any(HasLeaf);
			}

			string GetName(string input)
			{
				if (string.IsNullOrEmpty(input))
					return string.Empty;

				var sb = new StringBuilder();

				for (int i = 0; i < input.Length; i++)
				{
					char current = input[i];
					char? next = i + 1 < input.Length ? input[i + 1] : (char?) null;

					if (char.IsUpper(current))
					{
						if (i > 0 && (char.IsLower(input[i - 1]) || (next.HasValue && char.IsLower(next.Value))))
							sb.Append('_');
						sb.Append(current);
					}
					else
					{
						sb.Append(char.ToUpper(current));
					}
				}

				return sb.ToString().Replace("/", string.Empty);
			}
		}
	}

	internal class GeneratorConstantsNode
	{
		public string name;
		public Dictionary<string, GeneratorConstantsNode> children = new();
		public string fullPath; //id
	}
}
