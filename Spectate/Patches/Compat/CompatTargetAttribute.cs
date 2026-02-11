using JetBrains.Annotations;

namespace Spectate.Patches.Compat;

[AttributeUsage(AttributeTargets.Class)]
public class CompatTargetAttribute : Attribute {
	public string TargetGuid { get; }

	public CompatTargetAttribute(string targetGuid) {
		TargetGuid = targetGuid;
	}
}
