namespace Spectate.Network;

public readonly record struct PlugVersion {
	public readonly byte Major = 0;
	public readonly byte Minor = 0;
	public readonly byte Patch = 0;

	public PlugVersion() {}

	public PlugVersion(byte major, byte minor, byte patch) {
		Major = major;
		Minor = minor;
		Patch = patch;
	}

	public PlugVersion(string version) {
		string[] parts = version.Split('.');
		if (parts.Length != 3) {
			Logger.Error($"Invalid version string '{version}', expected format 'major.minor.patch'.");
			return;
		}

		if (!byte.TryParse(parts[0], out byte major)) {
			Logger.Error($"Invalid major version '{parts[0]}' in version string '{version}'.");
			return;
		}
		if (!byte.TryParse(parts[1], out byte minor)) {
			Logger.Error($"Invalid minor version '{parts[1]}' in version string '{version}'.");
			return;
		}
		if (!byte.TryParse(parts[2], out byte patch)) {
			Logger.Error($"Invalid patch version '{parts[2]}' in version string '{version}'.");
			return;
		}

		Major = major;
		Minor = minor;
		Patch = patch;
	}

	public PlugVersion(byte[] version) {
		if (version.Length < 3) {
			Logger.Error($"Invalid version byte array of length {version.Length}, expected at least 3.");
			return;
		}

		Major = version[0];
		Minor = version[1];
		Patch = version[2];
	}

	public PlugVersion(byte[] data, int offset) {
		if (data.Length < offset + 3) {
			Logger.Error($"Invalid version byte array of length {data.Length} with offset {offset}, expected at least {offset + 3}.");
			return;
		}

		Major = data[offset];
		Minor = data[offset + 1];
		Patch = data[offset + 2];
	}

	public byte[] ToByteArray() {
		return new[] { Major, Minor, Patch };
	}

	public static bool operator > (PlugVersion a, PlugVersion b) {
		if (a.Major != b.Major) return a.Major > b.Major;
		if (a.Minor != b.Minor) return a.Minor > b.Minor;
		return a.Patch > b.Patch;
	}

	public static bool operator <(PlugVersion a, PlugVersion b) {
		if (a.Major != b.Major) return a.Major < b.Major;
		if (a.Minor != b.Minor) return a.Minor < b.Minor;
		return a.Patch < b.Patch;
	}

	public override string ToString() {
		return $"{Major}.{Minor}.{Patch}";
	}
}
