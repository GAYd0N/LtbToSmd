using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LtbToSmd.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LtbToSmd.IoCFileOps.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace LtbToSmd.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private LtbModel _ltbModel;
        public MainWindowViewModel()
        {
            _ltbModel = new LtbModel(this);
            AppendLog("Welcome to LTB to SMD converter.");
        }

        [ObservableProperty]
        public string? _fileText;

        [ObservableProperty]
        public string? _logText;

        public void AppendLog(string log)
        {
          LogText += "[" + DateTime.Now.ToString("HH:mm:FF") + "]" + log + Environment.NewLine;
        }

        [RelayCommand]
        private async Task OpenFile(CancellationToken token)
        {
            ErrorMessages?.Clear();
            try
            {
                var filesService = App.Current?.Services?.GetRequiredService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

                var file = await filesService.OpenFileAsync();
                if (file is null) return;

                foreach (var item in file.GetBasicPropertiesAsync().GetType().Name)
                {
                    
                }

                await using var readStream = await file.OpenReadAsync();
                using var reader = new StreamReader(readStream);
                FileText = await reader.ReadToEndAsync(token);


            }
            catch (Exception e)
            {
                ErrorMessages?.Add(e.Message);
            }
        }

        [RelayCommand]
        private async Task SaveFile()
        {
            ErrorMessages?.Clear();
            try
            {
                var filesService = App.Current?.Services?.GetService<IFilesService>();
                if (filesService is null) throw new NullReferenceException("Missing File Service instance.");

                var file = await filesService.SaveFileAsync();
                if (file is null) return;


                // Limit the text file to 1MB so that the demo wont lag.
                if (FileText?.Length <= 1024 * 1024 * 1)
                {
                    var stream = new MemoryStream(Encoding.Default.GetBytes((string)FileText));
                    await using var writeStream = await file.OpenWriteAsync();
                    await stream.CopyToAsync(writeStream);
                }
                else
                {
                    throw new Exception("File exceeded 1MB limit.");
                }
            }
            catch (Exception e)
            {
                ErrorMessages?.Add(e.Message);
            }
        }





    }
}
