using Fusumity.Editor.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UI;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public static partial class ContentConstantGenerator
	{
		private const string NAMESPACE_SEPARATOR = ".";
		private const string ROOT_NODE = "ROOT";
		private const string CONSTANTS_NAME = "Constants";
		private const string NEW_LINE_TAG = "{NEW_LINE}";

		internal static void Generate(Type valueType, IEnumerable<IUniqueContentEntryScriptableObject> collection,
			ConstantsAttribute attribute = null, bool fullLog = false)
		{
			if (collection.IsNullOrEmpty())
				return;

			attribute ??= valueType.GetCustomAttribute<ConstantsAttribute>();

			if (attribute == null)
			{
				var scrObjType = collection
					.First()
					.GetType();
				attribute = scrObjType.GetCustomAttribute<ConstantsAttribute>();
			}

			if (attribute == null)
			{
				GUIDebug.LogWarning("ConstantsAttribute not found");
				return;
			}

			var name = BuildGenericSafeName(valueType, out var postfix);
			var className = postfix.IsNullOrEmpty() ? name : name[..^postfix.Length];
			for (int i = 0; i < projectSettings.removeEndings.Length; i++)
			{
				var ending = projectSettings.removeEndings[i];
				if (className.EndsWith(ending))
				{
					className = className[..^ending.Length];
					break;
				}
			}

			if (attribute is { ReplaceForClassName: not null })
			{
				var from = attribute.ReplaceForClassName.Value.from;
				var to = attribute.ReplaceForClassName.Value.to;
				className = className.Replace(from, to);
			}

			className += postfix;

			var rootName = nameof(Content);
			var @namespace = valueType.Namespace;

			className += projectSettings.classNameEnding;

			ConstantsOutput output = null;
			var namespaceByOutput = @namespace;
			if (!@namespace.IsNullOrEmpty()
				&& !projectSettings.namespaceToOutput.TryGetValue(@namespace, out output))
			{
				string bestMatch = null;
				foreach (var (t, tOutput) in projectSettings.namespaceToOutput)
				{
					if (!@namespace.StartsWith(t))
						continue;
					if (bestMatch != null && t.Length <= bestMatch.Length)
						continue;
					bestMatch = t;
					output = tOutput;
				}

				namespaceByOutput = bestMatch;
			}

			output ??= new ConstantsOutput
			{
				asmdef = projectSettings.output.asmdef,
				folderPath = projectSettings.output.folderPath,
				trimGeneratePath = projectSettings.output.trimGeneratePath,
			};

			if (attribute.HasCustomizedOutputPath)
			{
				output.trimGeneratePath = true;
				output.asmdef = null;
				namespaceByOutput = @namespace;
			}

			if (!attribute.OutputPath.IsNullOrEmpty())
			{
				output.folderPath = attribute.OutputPath;
			}
			else if (attribute.RespectExistingOutputPath)
			{
				var existingPath = AssetDatabaseUtility.FindScriptPath(className);
				if (!existingPath.IsNullOrEmpty())
				{
					output.folderPath = Path.GetDirectoryName(existingPath).SlashSafe();
				}
			}
			else if (attribute.TypeOutputPath != null)
			{
				var typePath = AssetDatabaseUtility.FindScriptPath(attribute.TypeOutputPath);
				output.folderPath = Path.GetDirectoryName(typePath).SlashSafe();
			}
			else if (attribute.UseAppliedTypeOutputPath)
			{
				var typePath = AssetDatabaseUtility.FindScriptPath(valueType);
				output.folderPath = Path.GetDirectoryName(typePath).SlashSafe();
			}

			var folderPath = output.folderPath;

			if (output.asmdef && !output.asmdef.value.IsNullOrEmpty())
			{
				var asmdefName = $"{output.asmdef.value}.asmdef";
				var asmdefFilePath = Path.Combine(folderPath, asmdefName);
				var asmdefFileInfo = new FileInfo(asmdefFilePath);
				if (!asmdefFileInfo.Exists)
				{
					var constantsAsmdefPath = asmdefFileInfo.FullName;
					var text = $@"
					{{
						""name"": ""{projectSettings.output.asmdef.value}.{CONSTANTS_NAME}"",
						""rootNamespace"": """",
						""references"": [],
						""includePlatforms"": [],
						""excludePlatforms"": [],
						""allowUnsafeCode"": false,
						""overrideReferences"": false,
						""precompiledReferences"": [],
						""autoReferenced"": false,
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
					ContentDebug.Log($"Generated .asmdef: {constantsAsmdefPath}", asmdefAsset);
				}
			}

			var generateFolderPath = @namespace;

			if (!namespaceByOutput.IsNullOrEmpty() && output.trimGeneratePath)
			{
				generateFolderPath = generateFolderPath.Remove(namespaceByOutput)
					.Remove(NAMESPACE_SEPARATOR);
			}

			var folder = generateFolderPath?.Replace(NAMESPACE_SEPARATOR, "/") ?? string.Empty;

			var fullPath = Path.Combine(folderPath, folder);

			if (!Directory.Exists(fullPath))
				Directory.CreateDirectory(fullPath);

			var fileName = !projectSettings.scriptFileNamePostfix.IsNullOrEmpty()
				? $"{className}.{projectSettings.scriptFileNamePostfix}.cs"
				: $"{className}.cs";
			var path = Path.Combine(fullPath, fileName);

			var existingData = File.Exists(path) ? File.ReadAllText(path) : null;

			var root = new GeneratorConstantsNode { name = ROOT_NODE };

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
						current.children[part] = new GeneratorConstantsNode { name = part };
					current = current.children[part];
				}

				current.fullPath = id;
			}

			var compilationUnit = SyntaxFactory.CompilationUnit();

			@namespace = !@namespace.IsNullOrEmpty()
				? @namespace
				: $"{rootName}.{CONSTANTS_NAME}";

			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace))
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

			var separator = "Assets/";
			var unityPath = separator + path.Split(separator)[^1];
			var textAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(unityPath);

			if (existingData == code)
			{
				if (fullLog)
					ContentDebug.Log($"Unchanged constants: {unityPath}", textAsset);
				return;
			}

			using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
			{
				writer.NewLine = "\n";
				writer.Write(code);
			}

			AssetDatabase.ImportAsset(unityPath);

			var prefix = existingData == null ? "Generated" : "Updated";
			ContentDebug.Log($"{prefix} constants: {unityPath}", textAsset);

			bool TryAddCustomConstants(ref bool space)
			{
				if (attribute != null && !attribute.CustomConstants.IsNullOrEmpty())
				{
					var commented = false;
					var comment = "Custom";

					foreach (var constant in attribute.CustomConstants)
					{
						var split = constant.Split(ConstantsAttribute.CUSTOM_CONSTANT_SEPARATOR);

						var id = split[^1];
						var customName = split.Length > 1;
						var name = customName ? split[0] : id.ToUpper();

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

				var currentPath = new List<string>(pathSegments) { node.name };
				var comment = string.Join(" -> ", currentPath.Skip(1));

				var handled = false;

				foreach (var child in node.children.Values.OrderBy(c => c.name))
				{
					if (child.fullPath.IsNullOrEmpty())
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

					space = child.children.Count == 0;
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
					char? next = i + 1 < input.Length ? input[i + 1] : (char?)null;

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

		private static string BuildGenericSafeName(Type t, out string postfix)
		{
			const string SEPARATOR = "";
			const string SEPARATOR_AND = "";

			string PrefixNested(Type x)
			{
				var parts = new Stack<string>();
				for (var cur = x; cur != null && cur.IsNested; cur = cur.DeclaringType)
				{
					parts.Push(CutTick(cur.Name));
				}

				return parts.Count > 0 ? string.Join("_", parts) + "_" : string.Empty;
			}

			string CutTick(string n)
			{
				var i = n.IndexOf('`');
				return i >= 0 ? n[..i] : n;
			}

			string Core(Type x, out string postfix)
			{
				postfix = null;

				if (x.IsGenericType)
				{
					var defName = CutTick(x.Name);
					var args = x.GetGenericArguments().Select(Select);
					postfix = SEPARATOR + string.Join(SEPARATOR_AND, args);
					return defName + postfix;
				}

				if (x.IsGenericTypeDefinition)
				{
					var defName = CutTick(x.Name);
					var pars = x.GetGenericArguments().Select(a => a.Name);
					postfix = SEPARATOR + string.Join(SEPARATOR_AND, pars);
					return defName + postfix;
				}

				return CutTick(x.Name);
			}

			string Select(Type x) => Core(x, out _);

			string MakeIdentifier(string s)
			{
				var sb = new System.Text.StringBuilder(s.Length);
				foreach (var ch in s)
					sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');

				// Идентификатор не должен начинаться с цифры
				if (sb.Length > 0 && char.IsDigit(sb[0]))
					sb.Insert(0, '_');

				return sb.ToString();
			}

			var prefix = t.IsNested ? PrefixNested(t) : string.Empty;
			return MakeIdentifier(prefix + Core(t, out postfix));
		}
	}

	internal class GeneratorConstantsNode
	{
		public string name;
		public Dictionary<string, GeneratorConstantsNode> children = new();
		public string fullPath; //id
	}
}
