using System;
using System.Collections;
using System.Collections.Generic;
using UI.RadialBar.Extensions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace UI.RadialBar
{
	#region Property Names

	public static class RadialBarProperties
	{
		public const string OverlayColor = "OverlayColor";
		public const string InnerColor = "InnerColor";
		public const string BorderColor = "BorderColor";
		public const string EmptyColor = "EmptyColor";
		public const string SpaceColor = "SpaceColor";
		public const string SegmentCount = "SegmentCount";
		public const string RemoveSegments = "RemoveSegments";
		public const string SegmentSpacing = "SegmentSpacing";
		public const string Arc = "Arc";
		public const string Radius = "Radius";
		public const string LineWidth = "LineWidth";
		public const string Rotation = "Rotation";
		public const string Offset = "Offset";
		public const string BorderWidth = "BorderWidth";
		public const string BorderSpacing = "BorderSpacing";
		public const string RemoveBorder = "RemoveBorder";
		public const string OverlayNoiseEnabled = "OverlayNoiseEnabled";
		public const string OverlayNoiseScale = "OverlayNoiseScale";
		public const string OverlayNoiseStrength = "OverlayNoiseStrength";
		public const string OverlayNoiseOffset = "OverlayNoiseOffset";
		public const string EmptyNoiseEnabled = "EmptyNoiseEnabled";
		public const string EmptyNoiseScale = "EmptyNoiseScale";
		public const string EmptyNoiseStrength = "EmptyNoiseStrength";
		public const string EmptyNoiseOffset = "EmptyNoiseOffset";
		public const string ContentNoiseEnabled = "ContentNoiseEnabled";
		public const string ContentNoiseScale = "ContentNoiseScale";
		public const string ContentNoiseStrength = "ContentNoiseStrength";
		public const string ContentNoiseOffset = "ContentNoiseOffset";
		public const string OverlayTexture = "OverlayTexture";
		public const string OverlayTextureOpacity = "OverlayTextureOpacity";
		public const string OverlayTextureTiling = "OverlayTextureTiling";
		public const string OverlayTextureOffset = "OverlayTextureOffset";
		public const string InnerTexture = "InnerTexture";
		public const string AlignInnerTexture = "AlignInnerTexture";
		public const string InnerTextureScaleWithSegments = "InnerTextureScaleWithSegments";
		public const string InnerTextureOpacity = "InnerTextureOpacity";
		public const string InnerTextureTiling = "InnerTextureTiling";
		public const string InnerTextureOffset = "InnerTextureOffset";
		public const string BorderTexture = "BorderTexture";
		public const string AlignBorderTexture = "AlignBorderTexture";
		public const string BorderTextureScaleWithSegments = "BorderTextureScaleWithSegments";
		public const string BorderTextureOpacity = "BorderTextureOpacity";
		public const string BorderTextureTiling = "BorderTextureTiling";
		public const string BorderTextureOffset = "BorderTextureOffset";
		public const string EmptyTexture = "EmptyTexture";
		public const string AlignEmptyTexture = "AlignEmptyTexture";
		public const string EmptyTextureScaleWithSegments = "EmptyTextureScaleWithSegments";
		public const string EmptyTextureOpacity = "EmptyTextureOpacity";
		public const string EmptyTextureTiling = "EmptyTextureTiling";
		public const string EmptyTextureOffset = "EmptyTextureOffset";
		public const string SpaceTexture = "SpaceTexture";
		public const string AlignSpaceTexture = "AlignSpaceTexture";
		public const string SpaceTextureOpacity = "SpaceTextureOpacity";
		public const string SpaceTextureTiling = "SpaceTextureTiling";
		public const string SpaceTextureOffset = "SpaceTextureOffset";
		public const string InnerGradient = "InnerGradient";
		public const string InnerGradientEnabled = "InnerGradientEnabled";
		public const string ValueAsGradientTimeInner = "ValueAsGradientTimeInner";
		public const string EmptyGradient = "EmptyGradient";
		public const string EmptyGradientEnabled = "EmptyGradientEnabled";
		public const string ValueAsGradientTimeEmpty = "ValueAsGradientTimeEmpty";
		public const string VariableWidthCurve = "VariableWidthCurve";
		public const string FillClockwise = "FillClockwise";
	}

	public static class RadialBarKeywords
	{
		public const string OverlayTextureEnabled = "OverlayTextureEnabled";
		public const string InnerTextureEnabled = "InnerTextureEnabled";
		public const string BorderTextureEnabled = "BorderTextureEnabled";
		public const string EmptyTextureEnabled = "EmptyTextureEnabled";
		public const string SpaceTextureEnabled = "SpaceTextureEnabled";
	}

	#endregion

	#region UniquePBMaterialValidator

	public class UniquePBMaterialValidator
	{
		private static List<string> uniqueMaterials = new List<string>();
		private static List<RadialSegmentedBar> instances = new List<RadialSegmentedBar>();

		public static bool Validate(string materialName, RadialSegmentedBar instance)
		{
			if (!materialName.Contains(RadialSegmentedBar.MATERIAL_NAME_PREFIX))
				return false;
			Refresh();
			bool isStored = instances.Contains(instance);
			if (uniqueMaterials.Contains(materialName) && !isStored)
				return false;
			if (isStored) return true;
			uniqueMaterials.Add(materialName);
			instances.Add(instance);
			return true;
		}

		private static void Refresh()
		{
			for (int i = 0; i < uniqueMaterials.Count; i++)
			{
				if (!instances[i])
				{
					instances.RemoveAt(i);
					uniqueMaterials.RemoveAt(i);
					i--;
				}
			}
		}
	}

	#endregion

	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Radial Bars/Radial Segmented Bar")]
	public class RadialSegmentedBar : MonoBehaviour, ISegmentedRadialBar
	{
		private string oldParentName = "Player";

		[SerializeField]
		private string parentName = "Player";

		public string ParentName
		{
			get => parentName;
			set
			{
				if (Application.isPlaying)
					StatusBarsManager.RemoveHealthBar(this, false);
				parentName = value;
				if (Application.isPlaying)
					StatusBarsManager.AddHealthBar(this);
			}
		}

		private string oldHbName = "Primary";

		[SerializeField]
		private string hbName = "Primary";

		public string Name
		{
			get => hbName;
			set
			{
				if (Application.isPlaying)
					StatusBarsManager.RemoveHealthBar(this, false);
				hbName = value;
				if (Application.isPlaying)
					StatusBarsManager.AddHealthBar(this);
			}
		}

		[SerializeField]
		public float updatesPerSecond = 30;

		public float UpdatesPerSecond { get => updatesPerSecond; set => updatesPerSecond = value; }

		[SerializeField]
		private bool usingSpriteRenderer;

		public bool UsingSpriteRenderer
		{
			get => usingSpriteRenderer;
			set
			{
				usingSpriteRenderer = value;
				GenerateRequiredComponents(false);
			}
		}

		[SerializeField]
		private bool forceBuiltInShader = false;

		public bool ForceBuiltInShader
		{
			get => forceBuiltInShader;
			set
			{
				forceBuiltInShader = value;
				GenerateRequiredComponents(true);
			}
		}

		#region Properties

		[field: SerializeField] public ShaderPropertyColor InnerColor { get; private set; }
		[field: SerializeField] public ShaderPropertyColor OverlayColor { get; private set; }
		[field: SerializeField] public ShaderPropertyColor BorderColor { get; private set; }
		[field: SerializeField] public ShaderPropertyColor EmptyColor { get; private set; }
		[field: SerializeField] public ShaderPropertyColor SpaceColor { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat SegmentCount { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat RemoveSegments { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat SegmentSpacing { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat Arc { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat Radius { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat LineWidth { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat Rotation { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 Offset { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat BorderWidth { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat BorderSpacing { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat RemoveBorder { get; private set; }
		[field: SerializeField] public ShaderPropertyBool OverlayNoiseEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat OverlayNoiseScale { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat OverlayNoiseStrength { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 OverlayNoiseOffset { get; private set; }
		[field: SerializeField] public ShaderPropertyBool EmptyNoiseEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat EmptyNoiseScale { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat EmptyNoiseStrength { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 EmptyNoiseOffset { get; private set; }
		[field: SerializeField] public ShaderPropertyBool ContentNoiseEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat ContentNoiseScale { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat ContentNoiseStrength { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 ContentNoiseOffset { get; private set; }
		[field: SerializeField] public ShaderKeyword OverlayTextureEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyTexture2D OverlayTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat OverlayTextureOpacity { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 OverlayTextureTiling { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 OverlayTextureOffset { get; private set; }
		[field: SerializeField] public ShaderKeyword InnerTextureEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyTexture2D InnerTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool AlignInnerTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool InnerTextureScaleWithSegments { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat InnerTextureOpacity { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 InnerTextureTiling { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 InnerTextureOffset { get; private set; }
		[field: SerializeField] public ShaderKeyword BorderTextureEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyTexture2D BorderTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool AlignBorderTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool BorderTextureScaleWithSegments { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat BorderTextureOpacity { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 BorderTextureTiling { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 BorderTextureOffset { get; private set; }
		[field: SerializeField] public ShaderKeyword EmptyTextureEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyTexture2D EmptyTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool AlignEmptyTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool EmptyTextureScaleWithSegments { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat EmptyTextureOpacity { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 EmptyTextureTiling { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 EmptyTextureOffset { get; private set; }
		[field: SerializeField] public ShaderKeyword SpaceTextureEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyTexture2D SpaceTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyBool AlignSpaceTexture { get; private set; }
		[field: SerializeField] public ShaderPropertyFloat SpaceTextureOpacity { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 SpaceTextureTiling { get; private set; }
		[field: SerializeField] public ShaderPropertyVector2 SpaceTextureOffset { get; private set; }
		[field: SerializeField] public ShaderPropertyGradient InnerGradient { get; private set; }
		[field: SerializeField] public ShaderPropertyBool InnerGradientEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyBool ValueAsGradientTimeInner { get; private set; }
		[field: SerializeField] public ShaderPropertyGradient EmptyGradient { get; private set; }
		[field: SerializeField] public ShaderPropertyBool EmptyGradientEnabled { get; private set; }
		[field: SerializeField] public ShaderPropertyBool ValueAsGradientTimeEmpty { get; private set; }
		[field: SerializeField] public ShaderPropertyAnimationCurve VariableWidthCurve { get; private set; }
		[field: SerializeField] public ShaderPropertyBool FillClockwise { get; private set; }

		#endregion

		private Material currentMaterial;
		private SpriteRenderer spriteRenderer;
		private Image image;

		private bool materialAssigned = false;

		private Material ActiveMaterial
		{
			get
			{
				if (!usingSpriteRenderer)
				{
					return image.materialForRendering;
				}

				return currentMaterial;
			}
		}

		private string BaseMaterialName
		{
			get
			{
				if (!forceBuiltInShader && GraphicsSettings.defaultRenderPipeline &&
				    Int32.Parse(Application.unityVersion.Split('.')[0]) > 2019)
					return "RadialSegmentedBarMaterial";

				return "RadialSegmentedBarMaterialBuiltIn";
			}
		}

		public const string MATERIAL_NAME_PREFIX = "runtime_";
		private const string PLACEHOLDER_SPRITE_NAME = "RadialPlaceholderSprite";

		private Dictionary<string, IShaderProperty> properties = new Dictionary<string, IShaderProperty>();

		private void Awake()
		{
			if (Application.isPlaying)
				StatusBarsManager.AddHealthBar(this);

			InitProperties();

			GenerateRequiredComponents(true);
		}

		private void Start()
		{
			ValidateComponents();
		}

		private void Update()
		{
			if (!materialAssigned)
				return;

#if UNITY_EDITOR
			if ((oldParentName != parentName || oldHbName != hbName))
			{
				StatusBarsManager.RemoveHealthBar(this, oldParentName, oldHbName, false);
				StatusBarsManager.AddHealthBar(this);
				oldParentName = parentName;
				oldHbName = hbName;
			}
#endif
		}

		private void OnValidate()
		{
			if (properties.Count == 0)
			{
				InitProperties();
			}

			ValidateComponents();
			if (materialAssigned)
				ApplyToShader(false);
		}

		private void Reset()
		{
			if (properties.Count == 0)
			{
				InitProperties();
			}

			ValidateComponents(true);
			ResetPropertiesToDefault();
		}

		private void OnDisable()
		{
			StopAllCoroutines();
		}

		private void OnEnable()
		{
			StartCoroutine("UpdateShader");
		}

		private IEnumerator UpdateShader()
		{
			while (true)
			{
				if (materialAssigned)
				{
					ApplyToShader(false);
				}

				yield return new WaitForSecondsRealtime(1 / Mathf.Max(updatesPerSecond, 1));
			}
		}

		public void ValidateComponents(bool useExisting = false)
		{
			if (usingSpriteRenderer)
			{
				if (!spriteRenderer || spriteRenderer && (!spriteRenderer.sprite || !spriteRenderer.sharedMaterial ||
					    !spriteRenderer.sharedMaterial.name.Contains(MATERIAL_NAME_PREFIX)))
				{
					GenerateRequiredComponents(useExisting);
				}
			}
			else if (!image || image && (!image.material || !image.material.name.Contains(MATERIAL_NAME_PREFIX)))
			{
				GenerateRequiredComponents(useExisting);
			}
		}

		private void GenerateRequiredComponents(bool useExisting)
		{
			spriteRenderer = GetComponent<SpriteRenderer>();
			image = GetComponent<Image>();
			Canvas parentCanvas = GetComponentInParent<Canvas>();
			//set up image
			if (useExisting && image || useExisting && parentCanvas || !useExisting && !usingSpriteRenderer)
			{
				if (parentCanvas?.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					forceBuiltInShader = true;
				}

				usingSpriteRenderer = false;
				if (spriteRenderer) DestroyImmediate(spriteRenderer);

				if (!image)
				{
					gameObject.AddComponent(typeof(Image));
					image = GetComponent<Image>();
					//img.hideFlags = HideFlags.HideInInspector;
				}

				if (!image.sprite)
				{
					image.sprite = Resources.Load<Sprite>(PLACEHOLDER_SPRITE_NAME);
				}

				AssignMaterial(image);
			}
			//set up sprite renderer
			else
			{
				usingSpriteRenderer = true;
				if (image)
				{
					DestroyImmediate(image);
					DestroyImmediate(GetComponent<CanvasRenderer>());
				}

				if (spriteRenderer == null)
				{
					gameObject.AddComponent(typeof(SpriteRenderer));
					spriteRenderer = GetComponent<SpriteRenderer>();
					//sr.hideFlags = HideFlags.HideInInspector;
				}

				if (!spriteRenderer.sprite)
				{
					spriteRenderer.sprite = Resources.Load<Sprite>(PLACEHOLDER_SPRITE_NAME);
				}

				AssignMaterial(spriteRenderer);
			}
		}

		private void InitProperties()
		{
			bool shouldPassProps = true; //SegmentCount != null && SegmentCount.Value != 0;
			properties = new Dictionary<string, IShaderProperty>
			{
				[RadialBarProperties.InnerColor] = InnerColor = new ShaderPropertyColor(
					"_" + RadialBarProperties.InnerColor, ColorPropertyFunc, Color.white, shouldPassProps ? InnerColor : null),
				[RadialBarProperties.OverlayColor] = OverlayColor = new ShaderPropertyColor(
					"_" + RadialBarProperties.OverlayColor, ColorPropertyFunc, Color.white, shouldPassProps ? OverlayColor : null),
				[RadialBarProperties.BorderColor] = BorderColor = new ShaderPropertyColor("_" + RadialBarProperties.BorderColor,
					ColorPropertyFunc, Color.white, shouldPassProps ? BorderColor : null),
				[RadialBarProperties.EmptyColor] = EmptyColor = new ShaderPropertyColor("_" + RadialBarProperties.EmptyColor,
					ColorPropertyFunc, Color.gray, shouldPassProps ? EmptyColor : null),
				[RadialBarProperties.SpaceColor] = SpaceColor = new ShaderPropertyColor("_" + RadialBarProperties.SpaceColor,
					ColorPropertyFunc, Color.clear, shouldPassProps ? SpaceColor : null),
				[RadialBarProperties.SegmentCount] = SegmentCount = new ShaderPropertyFloat(
					"_" + RadialBarProperties.SegmentCount, FloatPropertyFunc, 5, shouldPassProps ? SegmentCount : null),
				[RadialBarProperties.RemoveSegments] = RemoveSegments = new ShaderPropertyFloat(
					"_" + RadialBarProperties.RemoveSegments, FloatPropertyFunc, 1, shouldPassProps ? RemoveSegments : null),
				[RadialBarProperties.SegmentSpacing] = SegmentSpacing = new ShaderPropertyFloat(
					"_" + RadialBarProperties.SegmentSpacing, FloatPropertyFunc, 0.02f, shouldPassProps ? SegmentSpacing : null),
				[RadialBarProperties.Arc] = Arc = new ShaderPropertyFloat("_" + RadialBarProperties.Arc, FloatPropertyFunc, 0f,
					shouldPassProps ? Arc : null),
				[RadialBarProperties.Radius] = Radius = new ShaderPropertyFloat("_" + RadialBarProperties.Radius,
					FloatPropertyFunc, .35f, shouldPassProps ? Radius : null),
				[RadialBarProperties.LineWidth] = LineWidth = new ShaderPropertyFloat("_" + RadialBarProperties.LineWidth,
					FloatPropertyFunc, .06f, shouldPassProps ? LineWidth : null),
				[RadialBarProperties.Rotation] = Rotation = new ShaderPropertyFloat("_" + RadialBarProperties.Rotation,
					FloatPropertyFunc, 0, shouldPassProps ? Rotation : null),
				[RadialBarProperties.Offset] = Offset = new ShaderPropertyVector2("_" + RadialBarProperties.Offset,
					VectorPropertyFunc, new Vector2(), shouldPassProps ? Offset : null),
				[RadialBarProperties.BorderWidth] = BorderWidth = new ShaderPropertyFloat("_" + RadialBarProperties.BorderWidth,
					FloatPropertyFunc, 0.01f, shouldPassProps ? BorderWidth : null),
				[RadialBarProperties.BorderSpacing] = BorderSpacing = new ShaderPropertyFloat(
					"_" + RadialBarProperties.BorderSpacing, FloatPropertyFunc, 0.01f, shouldPassProps ? BorderSpacing : null),
				[RadialBarProperties.RemoveBorder] = RemoveBorder = new ShaderPropertyFloat(
					"_" + RadialBarProperties.RemoveBorder, FloatPropertyFunc, 1.0f, shouldPassProps ? RemoveBorder : null),
				[RadialBarProperties.OverlayNoiseEnabled] = OverlayNoiseEnabled =
					new ShaderPropertyBool("_" + RadialBarProperties.OverlayNoiseEnabled, BoolPropertyFunc, false,
						shouldPassProps ? OverlayNoiseEnabled : null),
				[RadialBarProperties.OverlayNoiseScale] = OverlayNoiseScale =
					new ShaderPropertyFloat("_" + RadialBarProperties.OverlayNoiseScale, FloatPropertyFunc, 100,
						shouldPassProps ? OverlayNoiseScale : null),
				[RadialBarProperties.OverlayNoiseStrength] = OverlayNoiseStrength =
					new ShaderPropertyFloat("_" + RadialBarProperties.OverlayNoiseStrength, FloatPropertyFunc, 0.5f,
						shouldPassProps ? OverlayNoiseStrength : null),
				[RadialBarProperties.OverlayNoiseOffset] = OverlayNoiseOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.OverlayNoiseOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? OverlayNoiseOffset : null),
				[RadialBarProperties.EmptyNoiseEnabled] = EmptyNoiseEnabled =
					new ShaderPropertyBool("_" + RadialBarProperties.EmptyNoiseEnabled, BoolPropertyFunc, false,
						shouldPassProps ? EmptyNoiseEnabled : null),
				[RadialBarProperties.EmptyNoiseScale] = EmptyNoiseScale = new ShaderPropertyFloat(
					"_" + RadialBarProperties.EmptyNoiseScale, FloatPropertyFunc, 100, shouldPassProps ? EmptyNoiseScale : null),
				[RadialBarProperties.EmptyNoiseStrength] = EmptyNoiseStrength =
					new ShaderPropertyFloat("_" + RadialBarProperties.EmptyNoiseStrength, FloatPropertyFunc, 0.5f,
						shouldPassProps ? EmptyNoiseStrength : null),
				[RadialBarProperties.EmptyNoiseOffset] = EmptyNoiseOffset = new ShaderPropertyVector2(
					"_" + RadialBarProperties.EmptyNoiseOffset, VectorPropertyFunc, new Vector2(),
					shouldPassProps ? EmptyNoiseOffset : null),
				[RadialBarProperties.ContentNoiseEnabled] = ContentNoiseEnabled =
					new ShaderPropertyBool("_" + RadialBarProperties.ContentNoiseEnabled, BoolPropertyFunc, false,
						shouldPassProps ? ContentNoiseEnabled : null),
				[RadialBarProperties.ContentNoiseScale] = ContentNoiseScale =
					new ShaderPropertyFloat("_" + RadialBarProperties.ContentNoiseScale, FloatPropertyFunc, 100,
						shouldPassProps ? ContentNoiseScale : null),
				[RadialBarProperties.ContentNoiseStrength] = ContentNoiseStrength =
					new ShaderPropertyFloat("_" + RadialBarProperties.ContentNoiseStrength, FloatPropertyFunc, 0.5f,
						shouldPassProps ? ContentNoiseStrength : null),
				[RadialBarProperties.ContentNoiseOffset] = ContentNoiseOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.ContentNoiseOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? ContentNoiseOffset : null),
				[RadialBarKeywords.OverlayTextureEnabled] = OverlayTextureEnabled = new ShaderKeyword("OVERLAY_TEXTURE_ON",
					KeywordFunc, false, shouldPassProps ? OverlayTextureEnabled : null),
				[RadialBarProperties.OverlayTexture] = OverlayTexture = new ShaderPropertyTexture2D(
					"_" + RadialBarProperties.OverlayTexture, TexturePropertyFunc, null, shouldPassProps ? OverlayTexture : null),
				[RadialBarProperties.OverlayTextureOpacity] = OverlayTextureOpacity =
					new ShaderPropertyFloat("_" + RadialBarProperties.OverlayTextureOpacity, FloatPropertyFunc, 1,
						shouldPassProps ? OverlayTextureOpacity : null),
				[RadialBarProperties.OverlayTextureTiling] = OverlayTextureTiling =
					new ShaderPropertyVector2("_" + RadialBarProperties.OverlayTextureTiling, VectorPropertyFunc, new Vector2(1, 1),
						shouldPassProps ? OverlayTextureTiling : null),
				[RadialBarProperties.OverlayTextureOffset] = OverlayTextureOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.OverlayTextureOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? OverlayTextureOffset : null),
				[RadialBarKeywords.InnerTextureEnabled] = InnerTextureEnabled = new ShaderKeyword("INNER_TEXTURE_ON", KeywordFunc,
					false, shouldPassProps ? InnerTextureEnabled : null),
				[RadialBarProperties.InnerTexture] = InnerTexture = new ShaderPropertyTexture2D(
					"_" + RadialBarProperties.InnerTexture, TexturePropertyFunc, null, shouldPassProps ? InnerTexture : null),
				[RadialBarProperties.AlignInnerTexture] = AlignInnerTexture =
					new ShaderPropertyBool("_" + RadialBarProperties.AlignInnerTexture, BoolPropertyFunc, true,
						shouldPassProps ? AlignInnerTexture : null),
				[RadialBarProperties.InnerTextureScaleWithSegments] = InnerTextureScaleWithSegments =
					new ShaderPropertyBool("_" + RadialBarProperties.InnerTextureScaleWithSegments, BoolPropertyFunc, true,
						shouldPassProps ? InnerTextureScaleWithSegments : null),
				[RadialBarProperties.InnerTextureOpacity] = InnerTextureOpacity =
					new ShaderPropertyFloat("_" + RadialBarProperties.InnerTextureOpacity, FloatPropertyFunc, 1,
						shouldPassProps ? InnerTextureOpacity : null),
				[RadialBarProperties.InnerTextureTiling] = InnerTextureTiling =
					new ShaderPropertyVector2("_" + RadialBarProperties.InnerTextureTiling, VectorPropertyFunc, new Vector2(1, 1),
						shouldPassProps ? InnerTextureTiling : null),
				[RadialBarProperties.InnerTextureOffset] = InnerTextureOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.InnerTextureOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? InnerTextureOffset : null),
				[RadialBarKeywords.BorderTextureEnabled] = BorderTextureEnabled = new ShaderKeyword("BORDER_TEXTURE_ON", KeywordFunc,
					false, shouldPassProps ? BorderTextureEnabled : null),
				[RadialBarProperties.BorderTexture] = BorderTexture = new ShaderPropertyTexture2D(
					"_" + RadialBarProperties.BorderTexture, TexturePropertyFunc, null, shouldPassProps ? BorderTexture : null),
				[RadialBarProperties.AlignBorderTexture] = AlignBorderTexture =
					new ShaderPropertyBool("_" + RadialBarProperties.AlignBorderTexture, BoolPropertyFunc, true,
						shouldPassProps ? AlignBorderTexture : null),
				[RadialBarProperties.BorderTextureScaleWithSegments] = BorderTextureScaleWithSegments =
					new ShaderPropertyBool("_" + RadialBarProperties.BorderTextureScaleWithSegments, BoolPropertyFunc, true,
						shouldPassProps ? BorderTextureScaleWithSegments : null),
				[RadialBarProperties.BorderTextureOpacity] = BorderTextureOpacity =
					new ShaderPropertyFloat("_" + RadialBarProperties.BorderTextureOpacity, FloatPropertyFunc, 1,
						shouldPassProps ? BorderTextureOpacity : null),
				[RadialBarProperties.BorderTextureTiling] = BorderTextureTiling =
					new ShaderPropertyVector2("_" + RadialBarProperties.BorderTextureTiling, VectorPropertyFunc, new Vector2(1, 1),
						shouldPassProps ? BorderTextureTiling : null),
				[RadialBarProperties.BorderTextureOffset] = BorderTextureOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.BorderTextureOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? BorderTextureOffset : null),
				[RadialBarKeywords.EmptyTextureEnabled] = EmptyTextureEnabled = new ShaderKeyword("EMPTY_TEXTURE_ON", KeywordFunc,
					false, shouldPassProps ? EmptyTextureEnabled : null),
				[RadialBarProperties.EmptyTexture] = EmptyTexture = new ShaderPropertyTexture2D(
					"_" + RadialBarProperties.EmptyTexture, TexturePropertyFunc, null, shouldPassProps ? EmptyTexture : null),
				[RadialBarProperties.AlignEmptyTexture] = AlignEmptyTexture =
					new ShaderPropertyBool("_" + RadialBarProperties.AlignEmptyTexture, BoolPropertyFunc, true,
						shouldPassProps ? AlignEmptyTexture : null),
				[RadialBarProperties.EmptyTextureScaleWithSegments] = EmptyTextureScaleWithSegments =
					new ShaderPropertyBool("_" + RadialBarProperties.EmptyTextureScaleWithSegments, BoolPropertyFunc, true,
						shouldPassProps ? EmptyTextureScaleWithSegments : null),
				[RadialBarProperties.EmptyTextureOpacity] = EmptyTextureOpacity =
					new ShaderPropertyFloat("_" + RadialBarProperties.EmptyTextureOpacity, FloatPropertyFunc, 1,
						shouldPassProps ? EmptyTextureOpacity : null),
				[RadialBarProperties.EmptyTextureTiling] = EmptyTextureTiling =
					new ShaderPropertyVector2("_" + RadialBarProperties.EmptyTextureTiling, VectorPropertyFunc, new Vector2(1, 1),
						shouldPassProps ? EmptyTextureTiling : null),
				[RadialBarProperties.EmptyTextureOffset] = EmptyTextureOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.EmptyTextureOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? EmptyTextureOffset : null),
				[RadialBarKeywords.SpaceTextureEnabled] = SpaceTextureEnabled = new ShaderKeyword("SPACE_TEXTURE_ON", KeywordFunc,
					false, shouldPassProps ? SpaceTextureEnabled : null),
				[RadialBarProperties.SpaceTexture] = SpaceTexture = new ShaderPropertyTexture2D(
					"_" + RadialBarProperties.SpaceTexture, TexturePropertyFunc, null, shouldPassProps ? SpaceTexture : null),
				[RadialBarProperties.AlignSpaceTexture] = AlignSpaceTexture =
					new ShaderPropertyBool("_" + RadialBarProperties.AlignSpaceTexture, BoolPropertyFunc, true,
						shouldPassProps ? AlignSpaceTexture : null),
				[RadialBarProperties.SpaceTextureOpacity] = SpaceTextureOpacity =
					new ShaderPropertyFloat("_" + RadialBarProperties.SpaceTextureOpacity, FloatPropertyFunc, 1,
						shouldPassProps ? SpaceTextureOpacity : null),
				[RadialBarProperties.SpaceTextureTiling] = SpaceTextureTiling =
					new ShaderPropertyVector2("_" + RadialBarProperties.SpaceTextureTiling, VectorPropertyFunc, new Vector2(1, 1),
						shouldPassProps ? SpaceTextureTiling : null),
				[RadialBarProperties.SpaceTextureOffset] = SpaceTextureOffset =
					new ShaderPropertyVector2("_" + RadialBarProperties.SpaceTextureOffset, VectorPropertyFunc, new Vector2(),
						shouldPassProps ? SpaceTextureOffset : null),
				[RadialBarProperties.InnerGradient] = InnerGradient = new ShaderPropertyGradient(
					"_" + RadialBarProperties.InnerGradient, GradientPropertyFunc, null, shouldPassProps ? InnerGradient : null),
				[RadialBarProperties.InnerGradientEnabled] = InnerGradientEnabled =
					new ShaderPropertyBool("_" + RadialBarProperties.InnerGradientEnabled, BoolPropertyFunc, true,
						shouldPassProps ? InnerGradientEnabled : null),
				[RadialBarProperties.ValueAsGradientTimeInner] = ValueAsGradientTimeInner =
					new ShaderPropertyBool("_" + RadialBarProperties.ValueAsGradientTimeInner, BoolPropertyFunc, false,
						shouldPassProps ? ValueAsGradientTimeInner : null),
				[RadialBarProperties.EmptyGradient] = EmptyGradient = new ShaderPropertyGradient(
					"_" + RadialBarProperties.EmptyGradient, GradientPropertyFunc, null, shouldPassProps ? EmptyGradient : null),
				[RadialBarProperties.EmptyGradientEnabled] = EmptyGradientEnabled =
					new ShaderPropertyBool("_" + RadialBarProperties.EmptyGradientEnabled, BoolPropertyFunc, false,
						shouldPassProps ? EmptyGradientEnabled : null),
				[RadialBarProperties.ValueAsGradientTimeEmpty] = ValueAsGradientTimeEmpty =
					new ShaderPropertyBool("_" + RadialBarProperties.ValueAsGradientTimeEmpty, BoolPropertyFunc, false,
						shouldPassProps ? ValueAsGradientTimeEmpty : null),
				[RadialBarProperties.VariableWidthCurve] = VariableWidthCurve =
					new ShaderPropertyAnimationCurve("_" + RadialBarProperties.VariableWidthCurve, CurvePropertyFunc,
						new AnimationCurve(new[] {new Keyframe(0, 1)}), shouldPassProps ? VariableWidthCurve : null),
				[RadialBarProperties.FillClockwise] = FillClockwise = new ShaderPropertyBool(
					"_" + RadialBarProperties.FillClockwise, BoolPropertyFunc, false, shouldPassProps ? FillClockwise : null)
			};
		}

		#region Getters and Setters

		public bool GetShaderProperty<T>(string propertyName, out ShaderProperty<T> shaderProperty)
		{
			if (properties[propertyName] is ShaderProperty<T> p)
			{
				shaderProperty = p;
				return true;
			}

			shaderProperty = null;
			return false;
		}

		public bool GetShaderKeyword(string propertyName, out ShaderKeyword shaderKeyword)
		{
			if (properties[propertyName] is ShaderKeyword p)
			{
				shaderKeyword = p;
				return true;
			}

			shaderKeyword = null;
			return false;
		}

		public bool GetShaderPropertyValue<T>(string propertyName, out T value)
		{
			if (properties[propertyName] is ShaderProperty<T> p)
			{
				value = p.Value;
				return true;
			}

			value = default;
			return false;
		}

		public bool SetShaderPropertyValue<T>(string propertyName, T value)
		{
			if (properties[propertyName] is ShaderProperty<T> p)
			{
				p.Value = value;
				return true;
			}

			return false;
		}

		public bool GetShaderKeywordValue(string propertyName, out bool value)
		{
			if (properties[propertyName] is ShaderKeyword p)
			{
				value = p.Value;
				return true;
			}

			value = default;
			return false;
		}

		public bool SetShaderKeywordValue(string propertyName, bool value)
		{
			if (properties[propertyName] is ShaderKeyword p)
			{
				p.Value = value;
				return true;
			}

			return false;
		}

		#endregion

		#region Property Functions

		private bool BoolPropertyFunc(int id, bool setInShader, bool value)
		{
			if (materialAssigned && !setInShader)
				return Convert.ToBoolean(ActiveMaterial.GetFloat(id));
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetFloat(id, value ? 1 : 0);
			}

			return default;
		}

		private float FloatPropertyFunc(int id, bool setInShader, float value)
		{
			if (materialAssigned && !setInShader)
				return ActiveMaterial.GetFloat(id);
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetFloat(id, value);
			}

			return default;
		}

		private Color ColorPropertyFunc(int id, bool setInShader, Color value)
		{
			if (materialAssigned && !setInShader)
				return ActiveMaterial.GetColor(id);
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetColor(id, value);
			}

			return default;
		}

		private Vector2 VectorPropertyFunc(int id, bool setInShader, Vector2 value)
		{
			if (materialAssigned && !setInShader)
				return ActiveMaterial.GetVector(id);
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetVector(id, value);
			}

			return default;
		}

		private Texture2D TexturePropertyFunc(int id, bool setInShader, Texture2D value)
		{
			if (materialAssigned && !setInShader)
				return (Texture2D) ActiveMaterial.GetTexture(id);
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetTexture(id, value);
			}

			return default;
		}

		private Gradient GradientPropertyFunc(int id, bool setInShader, Gradient value)
		{
			if (materialAssigned && !setInShader)
				return value;
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetTexture(id, value.ToTexture2D());
			}

			return default;
		}

		private AnimationCurve CurvePropertyFunc(int id, bool setInShader, AnimationCurve value)
		{
			if (materialAssigned && !setInShader)
				return value;
			if (materialAssigned && setInShader)
			{
				ActiveMaterial.SetTexture(id, value.ToTexture2D());
			}

			return default;
		}

		private bool KeywordFunc(string id, bool setInShader, bool value)
		{
			if (materialAssigned && !setInShader)
				return ActiveMaterial.IsKeywordEnabled(id);
			if (materialAssigned && setInShader && value)
				ActiveMaterial.EnableKeyword(id);
			else if (materialAssigned && setInShader && !value)
				ActiveMaterial.DisableKeyword(id);
			return false;
		}

		#endregion

		private void AssignMaterial(Image image)
		{
			if (image.material && UniquePBMaterialValidator.Validate(image.material.name, this))
			{
				currentMaterial = image.material;
				materialAssigned = true;
				return;
			}

			//get material
			var mat = Resources.Load<Material>(BaseMaterialName);

			if (mat != null && image != null)
			{
				//generate and apply the material
				currentMaterial = new Material(mat);
				currentMaterial.name = MATERIAL_NAME_PREFIX + Guid.NewGuid().ToString();
				image.material = currentMaterial;
				materialAssigned = true;
				ApplyToShader(true);
#if UNITY_EDITOR
				//the scene needs to be saved
				if (!Application.isPlaying)
					EditorSceneManager.MarkSceneDirty(SceneManager
					   .GetActiveScene());
#endif
			}
		}

		void AssignMaterial(SpriteRenderer r)
		{
			if (r.sharedMaterial && UniquePBMaterialValidator.Validate(r.sharedMaterial.name, this))
			{
				currentMaterial = r.sharedMaterial;
				materialAssigned = true;
				return;
			}

			//get resources
			Material mat = Resources.Load<Material>(BaseMaterialName);
			Sprite sprite = Resources.Load<Sprite>(PLACEHOLDER_SPRITE_NAME);

			if (mat != null && r != null)
			{
				//make sure the sprite will render the shader correctly
				if (r.sprite == null && sprite != null)
				{
					r.sprite = sprite;
				}

				r.drawMode = SpriteDrawMode.Simple;

				//generate and apply the material
				currentMaterial = new Material(mat);
				currentMaterial.name = MATERIAL_NAME_PREFIX + Guid.NewGuid().ToString();
				r.sharedMaterial = currentMaterial;
				materialAssigned = true;
				ApplyToShader(true);
#if UNITY_EDITOR
				//the scene needs to be saved
				if (!Application.isPlaying && !EditorUtility.IsPersistent(this))
					EditorSceneManager.MarkSceneDirty(SceneManager
					   .GetActiveScene());
#endif
			}
		}

		void ApplyToShader(bool ignoreDirty)
		{
			foreach (var property in properties)
			{
				property.Value.ApplyToShader(ignoreDirty);
			}
		}

		public void ResetPropertiesToDefault()
		{
			foreach (var item in properties)
			{
				item.Value.ResetToDefault();
			}

			ApplyToShader(false);
		}

		public void SetSegmentCount(float value)
		{
			SegmentCount.Value = Mathf.Max(0, value);
		}

		public void SetRemovedSegments(float value)
		{
			RemoveSegments.Value = Mathf.Clamp(value, 0, SegmentCount.Value);
		}

		public void SetPercent(float value)
		{
			float cVal = Mathf.Clamp(value, 0, 1);
			RemoveSegments.Value = (1 - cVal) * SegmentCount.Value;
		}

		public void AddRemoveSegments(float value)
		{
			RemoveSegments.Value += value;
			RemoveSegments.Value = Mathf.Clamp(RemoveSegments.Value, 0, SegmentCount.Value);
		}

		public void AddRemovePercent(float value)
		{
			RemoveSegments.Value += value * SegmentCount.Value;
			RemoveSegments.Value = Mathf.Clamp(RemoveSegments.Value, 0, SegmentCount.Value);
		}

		public string GetParentName()
		{
			return parentName;
		}

		public string GetName()
		{
			return hbName;
		}

		public float GetRotation()
		{
			return Rotation.Value;
		}

		public void SetSpacing(float value)
		{
			SegmentSpacing.Value = value;
		}

		public void SetRotation(float value)
		{
			Rotation.Value = value;
		}
	}
}
