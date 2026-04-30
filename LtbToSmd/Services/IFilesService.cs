using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace LtbToSmd.Services;

public interface IFilesService
{
    public Task<IStorageFile?> OpenFileAsync();
    public Task<IStorageFile?> OpenDtxFileAsync();
    public Task<IStorageFolder?> OpenFolderAsync();
}
