using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace YumeChan.NetRunner.Plugins.Infrastructure.Filesystem;

public class PluginWebAssetsProvider : IFileProvider
{
	private readonly IFileProvider _fileProvider;

	public PluginWebAssetsProvider(string pluginsDirectory)
	{
		_fileProvider = new PhysicalFileProvider(pluginsDirectory);
	}

	/// <summary>
	/// Translates a relative web path of format /{pluginName}/{path*} to a relative file path of format {pluginName}/wwwroot/{path*}.
	/// </summary>
	public static string? TranslateWebPathToFileSystemPath(string webPath)
	{
		string[] parts = webPath.Split('/');
		
		if (parts.Length < 2)
		{
			// Invalid path, just bypass it.
			return null;
		}

		string pluginName = parts[1];
		string filePath = string.Join("/", parts.Skip(2));

		return $"{pluginName}/wwwroot/{filePath}";
	}
	
	public IDirectoryContents GetDirectoryContents(string subpath) => _fileProvider.GetDirectoryContents(TranslateWebPathToFileSystemPath(subpath));

	public IFileInfo GetFileInfo(string subpath) => _fileProvider.GetFileInfo(TranslateWebPathToFileSystemPath(subpath));

	public IChangeToken Watch(string filter) => _fileProvider.Watch(filter);
}