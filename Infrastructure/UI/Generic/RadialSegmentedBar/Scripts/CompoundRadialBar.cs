using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.RadialBar
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Radial Bars/Compound Radial Bar")]
	public class CompoundRadialBar : MonoBehaviour, ISegmentedRadialBar
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
		private List<ISegmentedRadialBar> healthBars;

		[Space]
		[SerializeField]
		bool segmentsToggle = true;

		[Range(0, 1)]
		[SerializeField]
		private float value;

		[SerializeField]
		private float removedSegments = 0;

		[SerializeField]
		bool fillFrontToBack = true;

		[SerializeField]
		bool useGradient = false;

		[SerializeField]
		Gradient gradient;

		[Space]
		[SerializeField]
		private List<string> healthBarNames;

		private void Awake()
		{
			if (Application.isPlaying)
				StatusBarsManager.AddHealthBar(this);
		}

		private void Start()
		{
			Populate();
			InvokeRepeating(nameof(Validate), 0, 0.25f);
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (Application.isPlaying && (oldParentName != parentName || oldHbName != hbName))
			{
				StatusBarsManager.RemoveHealthBar(this, oldParentName, oldHbName, false);
				StatusBarsManager.AddHealthBar(this);
				oldParentName = parentName;
				oldHbName = hbName;
			}
#endif
		}

		private void Validate()
		{
			if (healthBars == null || transform.childCount != healthBars.Count)
			{
				Populate();
			}
		}

		private void OnValidate()
		{
			Populate();
			if (healthBars != null)
			{
				if (segmentsToggle)
					SetRemovedSegments(removedSegments);
				else
				{
					SetPercent(value);
				}
			}
		}

		private void Populate()
		{
			healthBars = new List<ISegmentedRadialBar>();
			healthBarNames = new List<string>();
			foreach (Transform healthBar in transform)
			{
				var hb = healthBar.GetComponent<RadialSegmentedBar>();
				hb.ParentName = "";
				hb.Name = "";
				healthBars.Add(hb);
				healthBarNames.Add(healthBar.gameObject.name);
			}

			if (!fillFrontToBack)
			{
				healthBars.Reverse();
				healthBarNames.Reverse();
			}
		}

		public void AddRemovePercent(float value)
		{
			this.value = Mathf.Clamp01(this.value + value);
			SetPercent(this.value);
		}

		public string GetName()
		{
			return hbName;
		}

		public string GetParentName()
		{
			return parentName;
		}

		public void SetPercent(float value)
		{
			float totalSegmentCount = TotalSegmentCount();
			SetRemovedSegments(totalSegmentCount - totalSegmentCount * Mathf.Clamp01(value));
		}

		public void AddRemoveSegments(float value)
		{
			removedSegments = Mathf.Clamp(removedSegments + value, 0, TotalSegmentCount());
			SetRemovedSegments(removedSegments);
		}

		public void SetRemovedSegments(float value)
		{
			if (useGradient && value != 0)
			{
				foreach (var healthBar in healthBars)
				{
					var hb = healthBar as RadialSegmentedBar;
					hb.InnerColor.Value = gradient.Evaluate(1 - Mathf.Clamp(value / TotalSegmentCount(), 0, 1));
				}
			}

			float remSegCount = value;
			for (int i = healthBars.Count - 1; i >= 0; i--)
			{
				var hb = healthBars[i];
				if (remSegCount == 0)
				{
					hb.SetRemovedSegments(0);
					break;
				}

				hb.SetRemovedSegments(remSegCount);
				float currentHBSegCount = SegmentCount(hb);
				remSegCount = Mathf.Clamp(remSegCount - currentHBSegCount, 0, float.MaxValue);
			}
		}

		float TotalSegmentCount()
		{
			return healthBars.Aggregate(0, (sum, next) => sum += (int) (next as RadialSegmentedBar).SegmentCount.Value);
		}

		float SegmentCount(ISegmentedRadialBar hb)
		{
			return (hb as RadialSegmentedBar).SegmentCount.Value;
		}

		#region unused methods

		public bool SetShaderKeywordValue(string propertyName, bool value)
		{
			return false;
		}

		public bool SetShaderPropertyValue<T>(string propertyName, T value)
		{
			return false;
		}

		public void SetSegmentCount(float value)
		{
		}

		public bool GetShaderKeyword(string propertyName, out ShaderKeyword shaderKeyword)
		{
			shaderKeyword = null;
			return false;
		}

		public bool GetShaderKeywordValue(string propertyName, out bool value)
		{
			value = false;
			return false;
		}

		public bool GetShaderProperty<T>(string propertyName, out ShaderProperty<T> shaderProperty)
		{
			shaderProperty = null;
			return false;
		}

		public bool GetShaderPropertyValue<T>(string propertyName, out T value)
		{
			value = default;
			return false;
		}

		#endregion
	}
}
