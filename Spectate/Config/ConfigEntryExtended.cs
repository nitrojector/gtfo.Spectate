using BepInEx.Configuration;

namespace Spectate.Config;

public enum ConfigEntryRule {
	Min,
	Max,
}

public class ConfigEntryExtended<T> {
	private Dictionary<ConfigEntryRule, T> _rules = new();

	private ConfigEntry<T> _entry;

	public T Value {
		get => _entry.Value;
		set => _entry.Value = Enforce(value);
	}

	public object BoxedValue {
		get => _entry.BoxedValue;
		set => _entry.BoxedValue = Enforce((T)value);
	}

	public ConfigEntryExtended(ConfigEntry<T> entry) {
		_entry = entry;
	}

	public bool AddRule(ConfigEntryRule rule, T ruleValue) {
		if (_rules.TryAdd(rule, ruleValue)) {
			Enforce(rule);
			return true;
		}

		return false;
	}

	private void Enforce() {
		T val = Enforce(_entry.Value);
		if (val != null && !val.Equals(_entry.Value)) {
			_entry.Value = val;
		}
	}

	private T Enforce(T val) {
		foreach (var (rule, ruleVal) in _rules) {
			switch (rule) {
				case ConfigEntryRule.Min:
					if (Comparer<T>.Default.Compare(val, ruleVal) < 0) {
						val = ruleVal;
					}

					break;
				case ConfigEntryRule.Max:
					if (Comparer<T>.Default.Compare(val, ruleVal) > 0) {
						val = ruleVal;
					}

					break;
			}
		}

		return val;
	}

	private void Enforce(ConfigEntryRule rule) {
		T val = _entry.Value;
		val = Enforce(val, rule);
		if (val != null && !val.Equals(_entry.Value)) {
			_entry.Value = val;
		}
	}

	private T Enforce(T val, ConfigEntryRule rule) {
		if (!_rules.TryGetValue(rule, out var ruleVal)) {
			return val;
		}

		switch (rule) {
			case ConfigEntryRule.Min:
				if (Comparer<T>.Default.Compare(val, ruleVal) < 0) {
					val = ruleVal;
				}

				break;
			case ConfigEntryRule.Max:
				if (Comparer<T>.Default.Compare(val, ruleVal) > 0) {
					val = ruleVal;
				}

				break;
		}

		return val;
	}

	public static implicit operator ConfigEntryExtended<T>(ConfigEntry<T> entry) {
		return new ConfigEntryExtended<T>(entry);
	}
}
