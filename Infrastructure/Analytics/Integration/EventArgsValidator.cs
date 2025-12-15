using System.Text.RegularExpressions;

namespace Analytics.Integration
{
	public class EventArgsValidator
	{
		private readonly IEventArgsValidationRule[] _rules;

		public EventArgsValidator(params IEventArgsValidationRule[] rules)
		{
			_rules = rules;
		}

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (var rule in _rules)
			{
				if (!rule.IsValid(args, out error))
					return false;
			}

			error = null;
			return true;
		}
	}

	public interface IEventArgsValidationRule
	{
		bool IsValid(in AnalyticsEventPayload args, out string error);
	}

	public class IdValidCharactersRule : IEventArgsValidationRule
	{
		private readonly Regex _validateIdRegex;

		public IdValidCharactersRule(Regex validateIdRegex) => _validateIdRegex = validateIdRegex;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			if (!_validateIdRegex.IsMatch(args.id))
			{
				error = "invalid event id";
				return false;
			}

			error = null;
			return true;
		}
	}

	public class ParamIdValidCharactersRule : IEventArgsValidationRule
	{
		private readonly Regex _validateIdRegex;

		public ParamIdValidCharactersRule(Regex validateIdRegex) => _validateIdRegex = validateIdRegex;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (var parameter in args.parameters.Keys)
			{
				if (!_validateIdRegex.IsMatch(parameter))
				{
					error = $"invalid parameter id '{parameter}'";
					return false;
				}
			}

			error = null;
			return true;
		}
	}

	public class IdNotUseReservedPrefixesRule : IEventArgsValidationRule
	{
		private readonly string[] _reservedPrefixes;

		public IdNotUseReservedPrefixesRule(params string[] reservedPrefixes) => _reservedPrefixes = reservedPrefixes;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (string reservedPrefix in _reservedPrefixes)
			{
				if (args.id.StartsWith(reservedPrefix))
				{
					error = $"used reserved prefix {reservedPrefix}";
					return false;
				}
			}

			error = null;
			return true;
		}
	}

	public class ParamIdNotUseReservedPrefixesRule : IEventArgsValidationRule
	{
		private readonly string[] _reservedPrefixes;

		public ParamIdNotUseReservedPrefixesRule(params string[] reservedPrefixes) => _reservedPrefixes = reservedPrefixes;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (var parameter in args.parameters.Keys)
			{
				foreach (string reservedPrefix in _reservedPrefixes)
				{
					if (args.id.StartsWith(parameter))
					{
						error = $"used reserved prefix '{reservedPrefix}' for parameter '{parameter}'";
						return false;
					}
				}
			}

			error = null;
			return true;
		}
	}

	public class ParamsCountRule : IEventArgsValidationRule
	{
		private readonly int _maxParamsCount;

		public ParamsCountRule(int maxParamsCount) => _maxParamsCount = maxParamsCount;
		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			error = args.parameters.Count > _maxParamsCount ? "too many parameters count" : null;
			return error == null;
		}
	}

	public class IdNotUsedReservedNamesRule : IEventArgsValidationRule
	{
		private readonly string[] _reservedNames;

		public IdNotUsedReservedNamesRule(params string[] reservedNames) => _reservedNames = reservedNames;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (string reservedName in _reservedNames)
			{
				if (args.id == reservedName)
				{
					error = "used reserved name";
					return false;
				}
			}

			error = null;
			return true;
		}
	}

	public class ParamIdNotUsedReservedNamesRule : IEventArgsValidationRule
	{
		private readonly string[] _reservedNames;

		public ParamIdNotUsedReservedNamesRule(params string[] reservedNames) => _reservedNames = reservedNames;

		public bool IsValid(in AnalyticsEventPayload args, out string error)
		{
			foreach (var parameter in args.parameters.Keys)
			{
				foreach (string reservedName in _reservedNames)
				{
					if (parameter == reservedName)
					{
						error = $"used reserved name for parameter '{parameter}'";
						return false;
					}
				}
			}

			error = null;
			return true;
		}
	}
}
