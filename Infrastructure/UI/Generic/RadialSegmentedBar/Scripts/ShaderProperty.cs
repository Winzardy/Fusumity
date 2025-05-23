using System;
using UI.RadialBar.Extensions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.RadialBar
{
	public interface IShaderProperty
	{
		void Reset();
		void ResetToDefault();
		bool ApplyToShader(bool ignoreDirty);
	}

	[Serializable]
	public class ShaderKeyword : IShaderProperty
	{
		[SerializeField]
		private bool _value;

		[SerializeField]
		private bool _isDirty;

		public bool Value
		{
			get { return _value; }
			set
			{
				_isDirty = true;
				_value = value;
			}
		}

		public bool IsDirty { get { return _isDirty; } set { _isDirty = value; } }

		private readonly string _keywordId;
		private Func<string, bool, bool, bool> _keywordFunc;
		private bool _defaultValue;

		public ShaderKeyword(string keywordId, Func<string, bool, bool, bool> keywordFunc, bool value = false,
			ShaderKeyword ancestor = null)
		{
			_keywordId = keywordId;
			_keywordFunc = keywordFunc;
			if (ancestor != null)
				_value = ancestor.Value;
			else
			{
				_value = value;
			}

			_defaultValue = value;
			IsDirty = false;
		}

		public void Reset()
		{
			_value = _keywordFunc(_keywordId, false, false);
		}

		public void ResetToDefault()
		{
			Value = _defaultValue;
		}

		public bool ApplyToShader(bool ignoreDirty)
		{
			if (IsDirty || ignoreDirty)
			{
				_keywordFunc(_keywordId, true, _value);
				IsDirty = false;
				return true;
			}

			return false;
		}
	}

	[Serializable]
	public class ShaderProperty<T> : IShaderProperty
	{
		[SerializeField]
		//[GenericColorUsage(true, true)]
		protected T _value;

		[SerializeField]
		protected bool _isDirty;

		public T Value
		{
			get { return _value; }
			set
			{
				_isDirty = true;
				_value = value;
			}
		}

		public bool IsDirty { get { return _isDirty; } set { _isDirty = value; } }
		protected readonly int _propertyId;
		protected Func<int, bool, T, T> _propertyFunc;
		protected T _defaultValue;

		public ShaderProperty(string propertyName, Func<int, bool, T, T> propertyFunc, T value = default, ShaderProperty<T> ancestor = null)
		{
			_propertyId = Shader.PropertyToID(propertyName);
			_propertyFunc = propertyFunc;
			_defaultValue = value;
			if (ancestor != null)
				_value = ancestor.Value;
			else
			{
				_value = value;
			}

			IsDirty = false;
		}

		public void Reset()
		{
			_value = _propertyFunc(_propertyId, false, default);
			IsDirty = false;
		}

		public virtual void ResetToDefault()
		{
			Value = _defaultValue;
		}

		public bool ApplyToShader(bool ignoreDirty)
		{
			if (IsDirty || ignoreDirty)
			{
				_propertyFunc(_propertyId, true, _value);
				IsDirty = false;
				return true;
			}

			return false;
		}
	}

	#region specific classes for unity serialization

	[Serializable]
	public class ShaderPropertyFloat : ShaderProperty<float>
	{
		public ShaderPropertyFloat(string propertyName, Func<int, bool, float, float> propertyFunc, float value = 0,
			ShaderPropertyFloat ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
		}
	}

	[Serializable]
	public class ShaderPropertyBool : ShaderProperty<bool>
	{
		public ShaderPropertyBool(string propertyName, Func<int, bool, bool, bool> propertyFunc, bool value = false,
			ShaderPropertyBool ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
		}
	}

	[Serializable]
	public class ShaderPropertyVector2 : ShaderProperty<Vector2>
	{
		public ShaderPropertyVector2(string propertyName, Func<int, bool, Vector2, Vector2> propertyFunc, Vector2 value = new Vector2(),
			ShaderPropertyVector2 ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
		}
	}

	[Serializable]
	public class ShaderPropertyColor : ShaderProperty<Color>
	{
		public ShaderPropertyColor(string propertyName, Func<int, bool, Color, Color> propertyFunc, Color value = new Color(),
			ShaderPropertyColor ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
		}
	}

	[Serializable]
	public class ShaderPropertyTexture2D : ShaderProperty<Texture2D>
	{
		public ShaderPropertyTexture2D(string propertyName, Func<int, bool, Texture2D, Texture2D> propertyFunc, Texture2D value = null,
			ShaderPropertyTexture2D ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
			if (ancestor != null)
			{
				_value = ancestor.Value;
			}
		}

		public override void ResetToDefault()
		{
		}
	}

	[Serializable]
	public class ShaderPropertyGradient : ShaderProperty<Gradient>
	{
		public ShaderPropertyGradient(string propertyName, Func<int, bool, Gradient, Gradient> propertyFunc, Gradient value = null,
			ShaderPropertyGradient ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
			if (value != null)
			{
				_value = value.Clone();
			}

			if (ancestor != null && ancestor.Value != null)
			{
				_value = ancestor.Value.Clone();
			}
		}

		public override void ResetToDefault()
		{
		}
	}

	[Serializable]
	public class ShaderPropertyAnimationCurve : ShaderProperty<AnimationCurve>
	{
		public ShaderPropertyAnimationCurve(string propertyName, Func<int, bool, AnimationCurve, AnimationCurve> propertyFunc,
			AnimationCurve value = null, ShaderPropertyAnimationCurve ancestor = null) : base(propertyName, propertyFunc, value, ancestor)
		{
			if (value != null)
			{
				_defaultValue = value.Clone();
				_value = value.Clone();
			}

			if (ancestor != null && ancestor.Value != null)
			{
				_value = ancestor.Value.Clone();
			}
		}

		public override void ResetToDefault()
		{
			Value = _defaultValue.Clone();
		}
	}

	#endregion

	#region custom property drawers for shader properties

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ShaderKeyword))]
	public class ShaderKeywordPropertyDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var valProp = property.FindPropertyRelative("_value");
			var isDirtyProp = property.FindPropertyRelative("_isDirty");
			GUIContent newLabel = new GUIContent(label);
#if UNITY_2019
			string text = label.text;
			int index = text.IndexOf('>');
			if (index > 0)
				newLabel.text = text.Substring(1, index - 1);
#endif

			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(position, valProp, newLabel);
			if (EditorGUI.EndChangeCheck())
			{
				isDirtyProp.boolValue = true;
			}
		}
	}

	public abstract class ShaderPropertyPropertyDrawer<T> : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var valProp = property.FindPropertyRelative("_value");
			var isDirtyProp = property.FindPropertyRelative("_isDirty");
			GUIContent newLabel = new GUIContent(label);
#if UNITY_2019
			string text = label.text;
			int index = text.IndexOf('>');
			if (index > 0)
				newLabel.text = text.Substring(1, index - 1);
#endif
			EditorGUI.BeginChangeCheck();
			EditorGUI.PropertyField(position, valProp, newLabel);
			if (EditorGUI.EndChangeCheck())
			{
				isDirtyProp.boolValue = true;
			}
		}
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyFloat))]
	public class ShaderPropertyFloatPropertyDrawer : ShaderPropertyPropertyDrawer<float>
	{
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyBool))]
	public class ShaderPropertyBoolPropertyDrawer : ShaderPropertyPropertyDrawer<bool>
	{
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyVector2))]
	public class ShaderPropertyVector2PropertyDrawer : ShaderPropertyPropertyDrawer<Vector2>
	{
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyColor))]
	public class ShaderPropertyColorPropertyDrawer : ShaderPropertyPropertyDrawer<Color>
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var valProp = property.FindPropertyRelative("_value");
			var isDirtyProp = property.FindPropertyRelative("_isDirty");
			GUIContent newLabel = new GUIContent(label);
#if UNITY_2019
			string text = label.text;
			int index = text.IndexOf('>');
			if (index > 0)
				newLabel.text = text.Substring(1, index - 1);
#endif

			var value = valProp.colorValue;
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = valProp.hasMultipleDifferentValues;
			value = EditorGUI.ColorField(position, newLabel, value, true, true, true);
			if (EditorGUI.EndChangeCheck())
			{
				valProp.colorValue = value;
				isDirtyProp.boolValue = true;
			}
		}
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyTexture2D))]
	public class ShaderPropertyTexture2DPropertyDrawer : ShaderPropertyPropertyDrawer<Texture2D>
	{
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyGradient))]
	public class ShaderPropertyGradientPropertyDrawer : ShaderPropertyPropertyDrawer<Gradient>
	{
	}

	[CustomPropertyDrawer(typeof(ShaderPropertyAnimationCurve))]
	public class ShaderPropertyAnimationCurvePropertyDrawer : ShaderPropertyPropertyDrawer<AnimationCurve>
	{
	}
#endif

	#endregion

	#region CustomAttributes

	//public class GenericColorUsageAttribute : PropertyAttribute {
	//	//
	//	// Summary:
	//	//     If false then the alpha bar is hidden in the ColorField and the alpha value is
	//	//     not shown in the Color Picker.
	//	public readonly bool showAlpha = true;

	//	//
	//	// Summary:
	//	//     If set to true the Color is treated as a HDR color.
	//	public readonly bool hdr = false;

	//       public GenericColorUsageAttribute(bool showAlpha, bool hdr) {
	//           this.showAlpha = showAlpha;
	//           this.hdr = hdr;
	//       }
	//}

	#endregion

	#region custom property drawers for custom attributes

	//[CustomPropertyDrawer(typeof(GenericColorUsageAttribute))]
	//public class GenericColorUsagePropertyDrawer : PropertyDrawer {
	//       public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
	//		if (fieldInfo.FieldType != typeof(Color)) {
	//			EditorGUI.PropertyField(position, property, label);
	//			return;
	//           }

	//		EditorGUI.ColorField(position, property, new GUIContent("asdfasdf"));
	//       }
	//   }

	#endregion
}
