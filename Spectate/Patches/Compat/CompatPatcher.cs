using System.Diagnostics;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace Spectate.Patches.Compat;

public static class CompatPatcher {
	/// <summary>
	/// Keep track of what targets are patched
	/// </summary>
	private static readonly HashSet<string> PatchedTargetGUIDs = new();

	/// <summary>
	/// Check if a target is patched.
	/// </summary>
	/// <param name="targetGuid">GUID of target</param>
	/// <returns>true if patched</returns>
	public static bool IsPatched(string targetGuid) => PatchedTargetGUIDs.Contains(targetGuid);

	public static void PatchAll(Harmony h) {
		Assembly? thisAsm = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Assembly;
		AccessTools.GetTypesFromAssembly(thisAsm).Do(TryPatchTarget);
		return;

		void TryPatchTarget(Type encapsulator) {
			var attr = encapsulator.GetCustomAttribute<CompatTargetAttribute>();
			if (attr == null) return;

			if (!IL2CPPChainloader.Instance.Plugins.ContainsKey(attr.TargetGuid)) {
				Logger.Info(
					$"Skipping compatibility patch [{encapsulator.FullName}] for {attr.TargetGuid} since it is not loaded.");
				return;
			}

			foreach (var method in AccessTools.GetDeclaredMethods(encapsulator)) {
				var patchAttr = method.GetCustomAttribute<CompatPatchAttribute>();
				if (patchAttr == null) {
					continue;
				}

				var targetType = AccessTools.TypeByName(patchAttr.TypeName);
				if (targetType == null) {
					Logger.Error(
						$"Failed to find type \"{patchAttr.TypeName}\" for compat patch {method.Name} in {encapsulator.FullName}!");
					continue;
				}

				var original = AccessTools.Method(targetType, patchAttr.MethodName);
#if DEBUG
				Logger.Info($"MethodInfo: {original.DeclaringType?.FullName}::{original.Name}");
#endif
				if (original == null) {
					Logger.Error(
						$"Failed to find method \"{patchAttr.TypeName}\" => \"{patchAttr.MethodName}\" for compat patch {method.Name} in {encapsulator.FullName}!");
					continue;
				}

				try {
					System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(original.MethodHandle);
				} catch (Exception e) {
					Logger.Warn($"PrepareMethod failed for {original.DeclaringType?.FullName}::{original.Name}: {e.Message}");
				}

				var prefix = patchAttr.PatchType == HarmonyPatchType.Prefix ? method : null;
				var postfix = patchAttr.PatchType == HarmonyPatchType.Postfix ? method : null;
				var transpiler = patchAttr.PatchType == HarmonyPatchType.Transpiler ? method : null;
				var finalizer = patchAttr.PatchType == HarmonyPatchType.Finalizer ? method : null;
				var ilManipulator = patchAttr.PatchType == HarmonyPatchType.ILManipulator ? method : null;

				h.Patch(original,
					prefix != null ? new HarmonyMethod(prefix) : null,
					postfix != null ? new HarmonyMethod(postfix) : null,
					transpiler != null ? new HarmonyMethod(transpiler) : null,
					finalizer != null ? new HarmonyMethod(finalizer) : null,
					ilManipulator != null ? new HarmonyMethod(ilManipulator) : null);

				Logger.Info(
					$"Patched {patchAttr.PatchType} [{method.Name}] to {patchAttr.TypeName}::{patchAttr.MethodName} for compat with {attr.TargetGuid}");
			}

			PatchedTargetGUIDs.Add(attr.TargetGuid);
		}
	}
}
