using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;


namespace LtbToSmd.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LogTextBox.TextChanged += OnLogTextChanged;
        }

        private void OnLogTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text is { Length: > 0 })
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
        //private async void OnDrop(object? sender, DragEventArgs e)
        //{
        //    if (!e.Data.Contains(DataFormats.Files)) return;

        //    var items = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();
        //    var files = new List<IStorageFile>();

        //    foreach (var item in items)
        //    {
        //        if (item is IStorageFile file)
        //            files.Add(file);
        //        else if (item is IStorageFolder folder)
        //            files.AddRange(await FileHelper.ConvertFoldersToFilesAsync(new[] { folder }));
        //    }

        //    if (DataContext is MainViewModel store) _ = store.Import(files);
        //}

    }
}