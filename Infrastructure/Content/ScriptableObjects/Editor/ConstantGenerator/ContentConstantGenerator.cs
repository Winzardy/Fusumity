#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Content.ScriptableObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	public static partial class ContentConstantGenerator
	{
		private const string ROOT_NODE = "ROOT";
		private const string CONSTANTS_NAME = "Constants";
		private const string NEW_LINE_TAG = "{NEW_LINE}";

		internal static void Generate(Type type, List<IUniqueContentEntryScriptableObject> collection)
		{
			if (collection == null || collection.Count == 0)
				return;

			var attribute = type.GetCustomAttribute<ConstantsAttribute>();

			if (attribute == null)
				return;

			var name = type.Name;
			var className = name;
			for (int i = 0; i < projectSettings.removeEndings.Length; i++)
			{
				var ending = projectSettings.removeEndings[i];
				if (name.EndsWith(ending))
				{
					className = name[..^ending.Length];
					break;
				}
			}

			if (attribute is {ReplaceForClassName: not null})
			{
				var from = attribute.ReplaceForClassName.Value.from;
				var to = attribute.ReplaceForClassName.Value.to;
				className = className.Replace(from, to);
			}

			className += projectSettings.classNameEnding;

			var folderPath = projectSettings.folderPath;
			var rootName = nameof(Content);
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

				File.WriteAllText(constantsAsmdefPath, text);
				AssetDatabase.ImportAsset(constantsAsmdefPath);
				var asmdefAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(constantsAsmdefPath);
				ContentDebug.Log($"Generated .asmdef: {constantsAsmdefPath}", asmdefAsset);
			}

			var folder = type.Namespace?.Replace(".", "/") ?? string.Empty;
			var fullPath = Path.Combine(folderPath, folder);

			if (!Directory.Exists(fullPath))
				Directory.CreateDirectory(fullPath);

			var fileName = $"{className}.{projectSettings.scriptPostfix}.cs";
			var path = Path.Combine(fullPath, fileName);

			var existingData = File.Exists(path) ? File.ReadAllText(path) : null;

			var root = new GeneratorConstantsNode {name = ROOT_NODE};

			foreach (var source in collection)
			{
				if (!source.UseCustomId)
					continue;

				var id = source.Id;

				if (attribute?.FilterOut != null && Enumerable.Any(attribute.FilterOut, target => id.Contains(target)))
					continue;

				var parts = id.Split('/');
				var current = root;
				foreach (var part in parts)
				{
					if (!current.children.ContainsKey(part))
						current.children[part] = new GeneratorConstantsNode {name = part};
					current = current.children[part];
				}

				current.fullPath = id;
			}

			var compilationUnit = SyntaxFactory.CompilationUnit();

			var namespaceName = !string.IsNullOrEmpty(type.Namespace)
				? $"{rootName}.{CONSTANTS_NAME}.{type.Namespace}"
				: $"{rootName}.{CONSTANTS_NAME}";

			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
			   .NormalizeWhitespace()
			   .WithLeadingTrivia(SyntaxFactory.Comment(projectSettings.scriptComment));

			var classDeclaration = SyntaxFactory.ClassDeclaration(className)
			   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));

			var needSpace = false;
			TryAddCustomConstants(ref needSpace);
			AddConstants(root, new List<string>(), ref needSpace);

			namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);
			compilationUnit = compilationUnit.AddMembers(namespaceDeclaration);

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
			ContentDebug.Log($"{prefix} constants: {unityPath}", textAsset);

			bool TryAddCustomConstants(ref bool space)
			{
				if (attribute != null && !attribute.CustomConstants.IsNullOrEmpty())
				{
					var commented = false;
					var comment = "Custom";

					foreach (var (constant, index) in attribute.CustomConstants.WithIndex())
					{
						var split = constant.Split(ConstantsAttribute.CUSTOM_CONSTANT_SEPARATOR);

						var id = split[^1];
						var customName = split.Length > 1;
						var name = customName ? split[0] : id;

						var fieldDeclaration = SyntaxFactory.FieldDeclaration(
								SyntaxFactory.VariableDeclaration(
										SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
								   .AddVariables(
										SyntaxFactory.VariableDeclarator(name)
										   .WithInitializer(
												SyntaxFactory.EqualsValueClause(
													SyntaxFactory.LiteralExpression(
														SyntaxKind.StringLiteralExpression,
														SyntaxFactory.Literal(id))))))
						   .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.ConstKeyword));

						using (ListPool<SyntaxTrivia>.Get(out var trivia))
						{
							if (!commented && !string.IsNullOrEmpty(comment))
							{
								if (space)
									trivia.Add(SyntaxFactory.Comment(NEW_LINE_TAG));

								trivia.Add(SyntaxFactory.Comment($"// {comment}"));

								space = false;
								commented = true;
							}

							fieldDeclaration = fieldDeclaration.WithLeadingTrivia(trivia);
						}

						classDeclaration = classDeclaration.AddMembers(fieldDeclaration);
					}

					space = true;
					return true;
				}

				return false;
			}

			void AddConstants(GeneratorConstantsNode node, List<string> pathSegments, ref bool space)
			{
				var hasAnyLeaves = node.children.Values.Any(HasLeaf);

				var currentPath = new List<string>(pathSegments) {node.name};
				var comment = string.Join(" -> ", currentPath.Skip(1));

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

							if (!comment.IsNullOrEmpty())
								trivia.Add(SyntaxFactory.Comment($"// {comment}"));

							space = false;
							handled = true;
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

				foreach (var prefix in projectSettings.abbreviations)
				{
					if (input.StartsWith(prefix) && input.Length > prefix.Length && char.IsUpper(input[prefix.Length]))
					{
						sb.Append(prefix);
						sb.Append('_');
						input = input[prefix.Length..];
						break;
					}
				}

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
#endif
