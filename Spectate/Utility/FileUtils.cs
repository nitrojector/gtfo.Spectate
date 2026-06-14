namespace Spectate.Utility;

public static class FileUtils {
	/// <summary>
	/// Enumerates all direct subdirectories of <paramref name="dir"/> that contain a file
	/// named <paramref name="fileName"/>, yielding the full path to that file.
	/// </summary>
	public static IEnumerable<string> FileInEachSubdirectory(string dir, string fileName) {
		foreach (var subDir in Directory.EnumerateDirectories(dir)) {
			var path = Path.Combine(subDir, fileName);
			if (File.Exists(path)) yield return path;
		}
	}

	/// <summary>
	/// Explores all files in <paramref name="dir"/> and its subdirectories,
	/// yielding the full path to those that have the given <paramref name="extension"/>.
	/// </summary>
	/// <param name="dir">directory to walk</param>
	/// <param name="extension">extension to match</param>
	public static IEnumerable<string> FilesWithExtensionRecursive(string dir, string extension) {
		if (!Directory.Exists(dir)) yield break;
		var ext = extension.StartsWith(".") ? extension : $".{extension}";
		foreach (var file in Directory.EnumerateFiles(dir, $"*{ext}", SearchOption.AllDirectories))
			yield return file;
	}
}
