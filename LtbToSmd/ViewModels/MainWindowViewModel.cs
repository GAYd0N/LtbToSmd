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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Avalonia.Rendering;


namespace LtbToSmd.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly LtbModel m_LtbModel;

        public MainWindowViewModel()
        {
            m_LtbModel = new LtbModel(this);
            m_inputFiles = new List<string>();
            PrintLog("Welcome to LTB to SMD converter.");
        }

        #region LogText
        [ObservableProperty]
        public string? _logText;

        public void PrintLog(string log)
        {
            LogText += ("[" + DateTime.Now.ToString("HH:mm:ss:ff") + "]" + log + Environment.NewLine);
        }

        public void ClearLog()
        {
            LogText = string.Empty;
        }
        #endregion

        #region FileOperations

        [ObservableProperty]
        public bool _isAllowChangeInput = true;

        [ObservableProperty]
        public bool _isAllowChangeOutput = true;

        [ObservableProperty]
        public InputPathType _selectedInputType;

        partial void OnSelectedInputTypeChanged(InputPathType value)
        {
            if (value == InputPathType.PATH && Path.Exists(InputPath))
            {
                InputPath = Path.GetDirectoryName(InputPath);
            }
            else if (!File.Exists(InputPath))
            {
                InputPath = string.Empty;
                OutputPath = string.Empty;
            }
        }

        public enum InputPathType
        {
            FILE = 0,
            PATH
        }

        [ObservableProperty]
        public bool _isSeparateSmdEnabled = true;
        [ObservableProperty]
        public bool _isSeparateArmEnabled = true;
        [ObservableProperty]
        public bool _isExtractAnimEnabled = true;
        [ObservableProperty]
        public bool _isCalcKeyFramesEnabled = true;
        [ObservableProperty]
        public bool _isGenerateQCEnabled = false;
        [ObservableProperty]
        public string? _maxAnimFrame = "auto";
        [ObservableProperty]
        public bool _isCreateOutputFolder = true;
        [ObservableProperty]
        public bool _isCreateSeparateFolders = true;

        [ObservableProperty]
        public string? _inputPath;  // file or folder 

        partial void OnInputPathChanged(string? value)
        {
            if (Path.Exists(InputPath) == false)
                return;

            if (IsCreateOutputFolder == true)
            {
                if (SelectedInputType == InputPathType.FILE)
                {
                    OutputPath = Path.GetDirectoryName(value) + "\\output";
                }
                else if (SelectedInputType == InputPathType.PATH)
                {
                    OutputPath = value + "\\output";
                }
            }
            else if (IsCreateOutputFolder == false) 
            {
                if (SelectedInputType == InputPathType.FILE)
                {
                    OutputPath = Path.GetDirectoryName(value);
                }
                else if (SelectedInputType == InputPathType.PATH)
                {
                    OutputPath = value;
                }
            }
        }

        partial void OnIsCreateOutputFolderChanged(bool value)
        {
            if (string.IsNullOrEmpty(OutputPath))
                return;
            if (value == true)
            {
                OutputPath = OutputPath + "\\output";
            }
            else
            {
                string pattern = @"[\\/]output$";
                OutputPath = Regex.Replace(OutputPath, pattern, string.Empty);
            }
        }

        [ObservableProperty]
        public string? _outputPath; // folder

        [RelayCommand]
        private async Task BrowseForInputPath(CancellationToken token)
        {
            try
            {
                if (!IsAllowChangeInput) return;

                var filesService = App.Current?.Services?.GetRequiredService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

                if (SelectedInputType == InputPathType.FILE)
                {
                    var file = await filesService.OpenFileAsync();
                    if (file is null) return;

                    InputPath = file.TryGetLocalPath();
                    PrintLog("Input file: " + file.Path);
                    
                }
                else if (SelectedInputType == InputPathType.PATH)
                {
                    var folder = await filesService.OpenFolderAsync();
                    if (folder is null) return;

                    InputPath = folder.TryGetLocalPath();
                    PrintLog("Input folder: " + folder.Path);
                }
            }
            catch (Exception e)
            {
                PrintLog(e.Message);
            }
        }

        [RelayCommand]
        private async Task BrowseForOutputPath(CancellationToken token)
        {
            try
            {
                if (!IsAllowChangeOutput) return;

                var filesService = App.Current?.Services?.GetRequiredService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");
                var folder = await filesService.OpenFolderAsync();
                if (folder is null) return;

                OutputPath = folder.TryGetLocalPath();
                PrintLog("Output folder: " + folder.Path);
                OnInputPathChanged(OutputPath);
            }
            catch (Exception e)
            {
                PrintLog(e.Message);
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

        //        var stream = new MemoryStream(Encoding.Default.GetBytes((string)FileText));
        //        await using var writeStream = await file.OpenWriteAsync();
        //        await stream.CopyToAsync(writeStream);
        //    }
        //    catch (Exception e)
        //    {
        //        AppendLog(e.Message);
        //    }
        //}

        private void GetFileListFromPath(string path)
        {
            try
            {
                if (SelectedInputType == InputPathType.PATH)
                { 
                    // 获取目录中所有扩展名为 .ltb 的文件
                    string[] files = Directory.GetFiles(path, "*.ltb");

                    // 提取文件名（不带路径）并添加到 List 中
                    foreach (var file in files)
                    {
                        m_inputFiles.Add(Path.GetFileName(file));
                    }
                }
                else if (SelectedInputType == InputPathType.FILE)
                {
                    m_inputFiles.Add(Path.GetFileName(path));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("加载文件名时出错: " + ex.Message);
            }
        }
        #endregion

        #region FileConversion
        List<string> m_inputFiles;

        private bool _isConvertCanceled = true;

        [ObservableProperty]
        public bool _isAllowConvert = true;
        [ObservableProperty]
        public bool _isConverting = false;

        // 取消转换
        private readonly CancellationTokenSource _cts = new();

        [RelayCommand]
        private void CancelConvert()
        {
            _isConvertCanceled = true;
        }

        [RelayCommand]
        private void StartConvert()
        {
            if (_isConverting == true)
                return;
            m_inputFiles.Clear();
            if (string.IsNullOrEmpty(InputPath))
            {
                PrintLog("Please select a file or folder first.");
                return;
            }
            IsAllowChangeInput = false;
            IsAllowChangeOutput = false;
            IsAllowConvert = false;
            GetFileListFromPath(InputPath);
            int fileCount = m_inputFiles.Count;
            if (fileCount == 0)
            {
                PrintLog("No .ltb files found in the input folder.");
                return;
            }
            IsConverting = true;
            foreach (var file in m_inputFiles)
            {
                PrintLog("Converting " + file + "...");
                ConvertToSmd(file, _cts.Token);
            }
            IsAllowChangeInput = true;
            IsAllowChangeOutput = true;
            IsAllowConvert = true;
            IsConverting = false;
        }

        private void ConvertToSmd(string file, CancellationToken token)
        {
            m_LtbModel.ConvertToSmd(file, token);
        }

        #endregion
    }
}
