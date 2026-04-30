using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LtbToSmd.Services;

public class FilesService : IFilesService
{
    private readonly Window _target;
    private readonly ILocalizationService _localization;

    public FilesService(Window target) : this(target, CreateDefaultLocalization())
    {
    }

    public FilesService(Window target, ILocalizationService localization)
    {
        _target = target;
        _localization = localization;
    }

    private static ILocalizationService CreateDefaultLocalization()
    {
        return App.Current?.Services?.GetRequiredService<ILocalizationService>() ?? new LocalizationService();
    }

    public async Task<IStorageFile?> OpenFileAsync()
    {
        var fileTypes = new List<FilePickerFileType>
            {
                new FilePickerFileType(_localization["filter.ltb_model"])
                {
                    Patterns = new[] { "*.ltb" }
                },
                new FilePickerFileType(_localization["filter.dtx_image"])
                {
                    Patterns = new[] { "*.dtx"}
                }
            };
        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = _localization["dialog.open_file"],
            AllowMultiple = true,
            FileTypeFilter = fileTypes
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFolder?> OpenFolderAsync()
    {
        var folder = await _target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = _localization["dialog.open_folder"],
            AllowMultiple = false
        });

        return folder.Any() ? folder[0] : null;
    }

}