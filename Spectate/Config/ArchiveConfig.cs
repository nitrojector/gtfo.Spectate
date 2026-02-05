using TheArchive.Core.Attributes.Feature;
using TheArchive.Core.Attributes.Feature.Members;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Settings;

namespace Spectate.Config;

[EnableFeatureByDefault]
public class ArchiveConfig : Feature {
	public override string Name => "Spectate";
	public override FeatureGroup Group => FeatureGroups.Special;
	public override string Description => "Adjust Spectate configurations";

	public static bool Enabled { get; private set; }

	[FeatureConfig] public static SpectateConfig Settings { get; set; }

	public class SpectateConfig {
		[FSHeader("Settings Behavior")]
		[FSDisplayName("Archive Settings are Authoritative")]
		[FSDescription("Whether Spectate should prioritize using settings defined in TheArchive " +
		               "over those defined in the Spectate config file.\n" +
		               "\n" +
		               "If Enabled:\n" +
		               "This will cause Spectate to overwrite all configs where TheArchive is launched with " +
		               "and disable hot-reloading of the Spectate config file.\n" +
		               "\n" +
		               "If Disabled:\n" +
		               "Settings in TheArchive will be updated when the game is launched per profile specific config files. " +
		               "Changes to the Spectate config file will be reflected in TheArchive's settings. vice versa.")]
		public bool ArchiveSettingsAreAuthoritative { get; set; } = false;
	}

	public override void OnFeatureSettingChanged(FeatureSetting _) {
	}

	public override void OnEnable() {
		Enabled = true;
	}

	public override void OnDisable() {
		Enabled = false;
	}
}
