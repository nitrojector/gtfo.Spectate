namespace Spectate.Config;

[Flags]
public enum eDevOpts {
	None = 0,
	AllowSpectatingAnytime = 1 << 0,
	ExperimentalFeatures = 1 << 1,
	CompatPatchUsingReflection = 1 << 2,
}
