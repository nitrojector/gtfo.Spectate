using HarmonyLib;
using JetBrains.Annotations;

namespace Spectate.Patches.Compat;

[AttributeUsage(AttributeTargets.Method)]
public class CompatPatchAttribute : Attribute {
	public HarmonyPatchType PatchType { get; }
	public string TypeName { get; }
	public string MethodName { get; }

	public CompatPatchAttribute(HarmonyPatchType patchType, string typeName, string methodName) {
		PatchType = patchType;
		TypeName = typeName;
		MethodName = methodName;
	}
}
