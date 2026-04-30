using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using LtbToSmd.ViewModels;


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

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.Files)) return;

            var items = e.Data.GetFiles();
            if (items is null || !items.Any()) return;

            if (DataContext is not MainWindowViewModel vm) return;

            // 只处理第一个拖拽项
            var first = items.First();

            if (first is IStorageFolder folder)
            {
                var localPath = folder.TryGetLocalPath();
                if (localPath is null) return;
                vm.SetInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.PATH);
            }
            else if (first is IStorageFile file)
            {
                var localPath = file.TryGetLocalPath();
                if (localPath is null) return;
                vm.SetInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.FILE);
            }
        }

    }
}