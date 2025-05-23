using System;
using System.Collections;
using Fusumity.Utility;
using Sapientia.Reflection;
using Sapientia.ServiceManagement;
using Sirenix.OdinInspector;
using UI.Editor;

namespace UI.Popups.Editor
{
	public class UIDispatcherEditorPopupTab : IUIDispatcherEditorTab
	{
		private UIPopupDispatcher _dispatcher => ServiceLocator<UIPopupDispatcher>.Instance;
		int IUIDispatcherEditorTab.Order => 2;
		public string Title => "Popups";

		[ShowInInspector, HideLabel, InlineButton(nameof(TryShowPopupEditor), " Show ")]
		[ValueDropdown(nameof(GetPopupTypes))]
		[OnValueChanged(nameof(PopupTypeChanged))]
		public Type popupType;

		[LabelText("Force")]
		public bool popupShowForce;

		[PolymorphicDrawerSettings(ReadOnlyIfNotNullReference = true)]
		[ShowInInspector, ShowIf(nameof(popupArgs), null), LabelText("Args")]
		public IPopupArgs popupArgs;

		private void PopupTypeChanged()
		{
			popupArgs = null;

			var baseType = popupType?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];

			if (type == typeof(EmptyPopupArgs))
				return;

			popupArgs = type.CreateInstance<IPopupArgs>();
		}

		private void TryShowPopupEditor()
		{
			if (popupType == null)
			{
				GUIDebug.LogError("Выберите тип экрана!");
				return;
			}

			_dispatcher?.GetType()
			   .GetMethod(nameof(_dispatcher.Show))?
			   .MakeGenericMethod(popupType)
			   .Invoke(_dispatcher, new object[]
				{
					popupArgs,
					popupShowForce
				});
		}

		[PropertySpace(10, 0)]
		[Button("Hide")]
		private void HidePopupEditor()
		{
			_dispatcher.TryHideCurrent();
		}

		private IEnumerable GetPopupTypes()
		{
			var types = ReflectionUtility.GetAllTypes<IPopup>(false);
			foreach (var type in types)
			{
				var name = type.Name
				   .Replace("Popup", string.Empty);

				yield return new ValueDropdownItem(name, type);
			}
		}
	}
}
