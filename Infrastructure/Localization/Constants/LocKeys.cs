//TODO: нужно перенести в игровую ассамблею после добавления автогенерации
//Обычно он лежит где-то в игровой ассамблеи, потому что он частный случай для каждого отдельного проекта
//Можно кстате добавить проверку что какие-то теги есть в локализации но их не обрабатывают, типо в LocTags не нашлось константы
namespace Localization
{
	//Пример
	public class LocKeys
	{
		public const string REWARD = "reward";
		public const string RECEIVE = "receive";

		public const string OK = "OK";
		public const string YES = "yes";
		public const string NO = "no";

		/// <summary>
		/// Версия {version}-{platform}
		/// </summary>
		public const string VERSION = "version";

		/// <summary>
		/// Внимание!
		/// </summary>
		public const string ATTENTION = "attention";

		/// <summary>
		/// Настройки
		/// </summary>
		public const string SETTINGS_HEADER = "settings_header";

		/// <summary>
		/// Информация
		/// </summary>
		public const string BUTTON_INFO = "button_info";

		public const string BUTTON_SWAP = "button_swap";
		public const string BUTTON_REMOVE = "button_remove";
		public const string BUTTON_APPLY = "button_add";
		public const string BUTTON_REPLACE = "button_replace";
		public const string BUTTON_BUY = "button_buy";
		public const string BUTTON_UPGRADE = "button_upgrade";
		public const string BUTTON_MAX_UPGRADE = "button_max_upgrade";
		public const string BUTTON_QUICK_PLAY = "button_quick_play";

		/// <summary>
		/// Оценить
		/// </summary>
		public const string BUTTON_RATE = "button_rate";

		/// <summary>
		/// День {number}
		/// </summary>
		public const string LABEL_REWARD_DAY = "label_reward_day";

		public const string CONTEXTDIALOG_SURRENDER_DESCRIPTION = "surrender_decriprion";
		public const string CONTEXTDIALOG_SURRENDER_CONFIRM = "confirm_surrender";

		public const string CONTEXTDIALOG_PURCHASE_TITLE = "contextdialog_purchase_title";
		public const string CONTEXTDIALOG_PURCHASE_MESSAGE = "contextdialog_purchase_message";

		public const string WINDOW_LEVEL_UP_CHOOSE_NEW_SPELL = "window_level_up_title_choose_spell";
		public const string WINDOW_LEVEL_UP_CHOOSE_UPGRADE = "window_level_up_title_choose_uprade";
		public const string WINDOW_LEVEL_UP_CHOOSE_SPELL_PROPERTY = "window_level_up_title_choose_spell_property";

		/// <summary>
		/// Новый уровень
		/// </summary>
		public const string NEW_LEVEL = "new_level";

		public const string ADVERTISING_TOKEN = "advertising_token";

		public const string AUTH_REMOTE_USER_EXIST = "auth_remote_user_exist";

		public const string NO_SERVER_CONNECTION = "no_server_connection";
		public const string APP_UPDATE_REQUIRED = "app_update_required";
	}
}
