using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using LtbToSmd.Models;
using LtbToSmd.Services;
using Avalonia.Platform.Storage;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;


namespace LtbToSmd.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, ILogger, ILtbConversionConfig
    {
        private readonly LtbModel m_LtbModel;
        private readonly ILocalizationService _localization;

        public MainWindowViewModel() : this(CreateDefaultLocalization())
        {
        }

        public MainWindowViewModel(ILocalizationService localization)
        {
            _localization = localization;
            _localization.PropertyChanged += OnLocalizationChanged;
            m_LtbModel = new LtbModel(this, this);
            m_inputFiles = new List<string>();
            _languageItems = new ObservableCollection<LanguageItem>
            {
                new("中文", "zh-CN"),
                new("English", "en-US"),
            };
            _selectedLanguage = _languageItems.FirstOrDefault(l => l.Code == _localization.CurrentCulture.Name)
                                ?? _languageItems[0];
        }

        private static ILocalizationService CreateDefaultLocalization()
        {
            var services = App.Current?.Services;
            return services?.GetRequiredService<ILocalizationService>() ?? new LocalizationService();
        }

        private void OnLocalizationChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILocalizationService.CurrentCulture))
            {
                RefreshAllLocalizedProperties();
                OnPropertyChanged(nameof(CurrentLanguage));
            }
        }

        private void RefreshAllLocalizedProperties()
        {
            OnPropertyChanged(nameof(Localized_Title));
            OnPropertyChanged(nameof(Localized_TabLtb2Smd));
            OnPropertyChanged(nameof(Localized_TabDtx2Png));
            OnPropertyChanged(nameof(Localized_TabAbout));
            OnPropertyChanged(nameof(Localized_InputFile));
            OnPropertyChanged(nameof(Localized_InputFolder));
            OnPropertyChanged(nameof(Localized_InputWatermark));
            OnPropertyChanged(nameof(Localized_BtnBrowse));
            OnPropertyChanged(nameof(Localized_OutputLabel));
            OnPropertyChanged(nameof(Localized_OutputWatermark));
            OnPropertyChanged(nameof(Localized_ConfigOptions));
            OnPropertyChanged(nameof(Localized_ConfigSeparateArm));
            OnPropertyChanged(nameof(Localized_ConfigSeparateSmd));
            OnPropertyChanged(nameof(Localized_ConfigExtractAnim));
            OnPropertyChanged(nameof(Localized_ConfigForceAnimOrigin));
            OnPropertyChanged(nameof(Localized_ConfigGenerateQC));
            OnPropertyChanged(nameof(Localized_ConfigAutoCreateOutput));
            OnPropertyChanged(nameof(Localized_ConfigSeparateFolder));
            OnPropertyChanged(nameof(Localized_LogWatermark));
            OnPropertyChanged(nameof(Localized_BtnConvert));
            OnPropertyChanged(nameof(Localized_BtnCancel));
            OnPropertyChanged(nameof(Localized_DtxWip));
            OnPropertyChanged(nameof(Localized_AboutLink));
            OnPropertyChanged(nameof(Localized_AboutLanguage));
        }

        // ---- Localized properties ----
        public string Localized_Title => _localization["window.title"];
        public string Localized_TabLtb2Smd => _localization["tab.ltb2smd"];
        public string Localized_TabDtx2Png => _localization["tab.dtx2png"];
        public string Localized_TabAbout => _localization["tab.about"];
        public string Localized_InputFile => _localization["input.file"];
        public string Localized_InputFolder => _localization["input.folder"];
        public string Localized_InputWatermark => _localization["input.watermark"];
        public string Localized_BtnBrowse => _localization["browse"];
        public string Localized_OutputLabel => _localization["output.folder_label"];
        public string Localized_OutputWatermark => _localization["output.watermark"];
        public string Localized_ConfigOptions => _localization["config.options"];
        public string Localized_ConfigSeparateArm => _localization["config.separate_arm"];
        public string Localized_ConfigSeparateSmd => _localization["config.separate_smd"];
        public string Localized_ConfigExtractAnim => _localization["config.extract_anim"];
        public string Localized_ConfigForceAnimOrigin => _localization["config.force_anim_origin"];
        public string Localized_ConfigGenerateQC => _localization["config.generate_qc"];
        public string Localized_ConfigAutoCreateOutput => _localization["config.auto_create_output"];
        public string Localized_ConfigSeparateFolder => _localization["config.separate_folder_per_ltb"];
        public string Localized_LogWatermark => _localization["log.watermark"];
        public string Localized_BtnConvert => _localization["convert.button"];
        public string Localized_BtnCancel => _localization["cancel.button"];
        public string Localized_DtxWip => _localization["dtx.wip"];
        public string Localized_AboutLink => _localization["about.link"];
        public string Localized_AboutLanguage => _localization["about.language"];

        // ---- Language switching ----
        public class LanguageItem
        {
            public string Display { get; }
            public string Code { get; }
            public LanguageItem(string display, string code) { Display = display; Code = code; }
        }

        [ObservableProperty]
        private ObservableCollection<LanguageItem> _languageItems;

        [ObservableProperty]
        private LanguageItem? _selectedLanguage;

        partial void OnSelectedLanguageChanged(LanguageItem? value)
        {
            if (value is null) return;
            _localization.CurrentCulture = new CultureInfo(value.Code);
            OnPropertyChanged(nameof(CurrentLanguage));
        }

        public string CurrentLanguage => _localization.CurrentCulture.Name switch
        {
            "zh-CN" => "中文",
            _ => "English"
        };

        bool ILtbConversionConfig.IsBatch => SelectedInputType == InputPathType.PATH;

        #region LTB2SMD

        #region LogText
        [ObservableProperty]
        public string? _logText;

        public void PrintLog(string log)
        {
            LogText += ("[" + DateTime.Now.ToString("HH:mm:ss:ff") + "]" + log.TrimEnd('\n', '\r') + Environment.NewLine);
        }

        string ILogger.GetString(string key, params object?[] args)
        {
            return _localization.GetFormatted(key, args);
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
        public bool _isAllowChangeCreateSeparateFolder = true;

        [ObservableProperty]
        public InputPathType _selectedInputType;

        partial void OnSelectedInputTypeChanged(InputPathType value)
        {
            if (value == InputPathType.PATH)
            {
                if (Path.Exists(InputPath))
                {
                    InputPath = Path.GetDirectoryName(InputPath);
                    IsCreateSeparateFolders = true;
                }
                IsAllowChangeCreateSeparateFolder = false;
                IsCreateSeparateFolders = true;
            }
            else if (!File.Exists(InputPath))
            {
                IsAllowChangeCreateSeparateFolder = true;
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
        public bool _isGenerateQCEnabled = false;
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
                    PrintLog(((ILogger)this).GetString("msg.input_file", file.Path));

                }
                else if (SelectedInputType == InputPathType.PATH)
                {
                    var folder = await filesService.OpenFolderAsync();
                    if (folder is null) return;

                    InputPath = folder.TryGetLocalPath();
                    PrintLog(((ILogger)this).GetString("msg.input_folder", folder.Path));
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
                PrintLog(((ILogger)this).GetString("msg.output_folder", folder.Path));
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
                Console.WriteLine(((ILogger)this).GetString("msg.load_error", ex.Message));
            }
        }
        #endregion

        public void SetInputPathFromDrop(string localPath, InputPathType type)
        {
            if (!IsAllowChangeInput) return;

            SelectedInputType = type;
            InputPath = localPath;
        }

        #region FileConversion
        List<string> m_inputFiles;

        private bool _isConvertCanceled = true;

        [ObservableProperty]
        public bool _isAllowConvert = true;
        [ObservableProperty]
        public bool _isConverting = false;

        // 取消转换
        private readonly CancellationTokenSource _cts = new();

        [ObservableProperty]
        public bool _isForceAnimOrigin = true;

        [RelayCommand]
        private void CancelConvert()
        {
            _isConvertCanceled = true;
        }

        [RelayCommand]
        private void StartConvert()
        {
            if (IsConverting == true)
                return;
            m_inputFiles.Clear();
            if (string.IsNullOrEmpty(InputPath))
            {
                PrintLog(((ILogger)this).GetString("msg.select_first"));
                return;
            }
            IsAllowChangeInput = false;
            IsAllowChangeOutput = false;
            IsAllowConvert = false;
            GetFileListFromPath(InputPath);
            int fileCount = m_inputFiles.Count;
            if (fileCount == 0)
            {
                PrintLog(((ILogger)this).GetString("msg.no_ltb_files"));
                return;
            }
            IsConverting = true;
            foreach (var file in m_inputFiles)
            {
                PrintLog(((ILogger)this).GetString("msg.converting", file));
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

        [RelayCommand]
        private void Hyperlink_PointerPressed()
        {
            // 打开链接
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/GAYd0N/LtbToSmd",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
        }
        #endregion

        //Work in progress
        #region DTX2PNG



        #endregion
    }
}
