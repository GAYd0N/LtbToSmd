using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LtbToSmd.IoCFileOps.Services;
using Microsoft.Extensions.DependencyInjection;
using LtbToSmd.Models;
using Avalonia.Platform.Storage;

namespace LtbToSmd.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private LtbModel _ltbModel;
        public MainWindowViewModel()
        {
            _ltbModel = new LtbModel(this);
            AddLog("Welcome to LTB to SMD converter.");
        }

        #region LogText
        [ObservableProperty]
        public string? _logText;

        public void AddLog(string log)
        {
            LogText += ("[" + DateTime.Now.ToString("HH:mm:ss:ff") + "]" + log + Environment.NewLine);
        }

        public void ClearLog()
        {
            LogText = string.Empty;
        }
        #endregion

        #region FileOperations
        public void AddInputFile(string file) {
            _ltbModel.AddInputFile(file);
        }

        [ObservableProperty]
        public InputPathType _selectedInputType;

        partial void OnSelectedInputTypeChanged(InputPathType value)
        {
            if (value is InputPathType.PATH && File.Exists(InputPath))
            {
                InputPath = Path.GetDirectoryName(InputPath);
            }
            else if (!File.Exists(InputPath))
            {
                InputPath = string.Empty;
            }
        }

        public enum InputPathType
        {
            FILE = 0,
            PATH
        }

        [ObservableProperty]
        public string? _inputPath;  // file or folder 

        partial void OnInputPathChanged(string? value)
        {
            if (OutputPath == null)
            {
                OutputPath = Path.GetDirectoryName(value) + "\\output";
            }
        }

        [ObservableProperty]
        public string? _outputPath; // folder

        [RelayCommand]
        private async Task BrowseForInputPath(CancellationToken token)
        {
            try
            {
                var filesService = App.Current?.Services?.GetRequiredService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

                if (SelectedInputType == InputPathType.FILE)
                {
                    var file = await filesService.OpenFileAsync();
                    if (file is null) return;

                    InputPath = file.TryGetLocalPath();
                    AddLog("Input file: " + file.Path);
                }
                else if (SelectedInputType == InputPathType.PATH)
                {
                    var folder = await filesService.OpenFolderAsync();
                    if (folder is null) return;

                    InputPath = folder.TryGetLocalPath();
                    AddLog("Input folder: " + folder.Path);
                }
            }
            catch (Exception e)
            {
                AddLog(e.Message);
            }
        }

        [RelayCommand]
        private async Task BrowseForOutputPath(CancellationToken token)
        {
            try
            {
                var filesService = App.Current?.Services?.GetRequiredService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");
                var folder = await filesService.OpenFolderAsync();
                if (folder is null) return;

                OutputPath = folder.TryGetLocalPath();
                AddLog("Output folder: " + folder.Path);
            }
            catch (Exception e)
            {
                AddLog(e.Message);
            }
        }

        //[RelayCommand]
        //private async Task SaveFile()
        //{
        //    try
        //    {
        //        var filesService = App.Current?.Services?.GetService<IFilesService>();
        //        if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

        //        var file = await filesService.SaveFileAsync();
        //        if (file is null) return;


        //        // Limit the text file to 1MB so that the demo wont lag.

        //        var stream = new MemoryStream(Encoding.Default.GetBytes((string)FileText));
        //        await using var writeStream = await file.OpenWriteAsync();
        //        await stream.CopyToAsync(writeStream);
        //    }
        //    catch (Exception e)
        //    {
        //        AppendLog(e.Message);
        //    }
        //}

        //private void LoadFileFromPath(string path)
        //{
        //    if (File.Exists(path))
        //    {
        //        SelectedInputType = InputPathType.FILE;
        //        using var reader = new StreamReader(path);
        //        //FileText = reader.ReadToEnd();
        //    }
        //    else if (Directory.Exists(path))
        //    {
        //        SelectedInputType = InputPathType.PATH;
        //        AddLog("路径指向一个文件夹，无法加载文件。");
        //    }
        //    else
        //    {
        //        AddLog("路径既不是文件也不是文件夹。");
        //    }
        //}
        #endregion


    }
}
