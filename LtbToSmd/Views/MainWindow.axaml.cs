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
            DtxLogTextBox.TextChanged += OnLogTextChanged;
        }

        private void OnLogTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.Text is not { Length: > 0 }) return;
            if (DataContext is not MainWindowViewModel vm || !vm.IsAutoScroll) return;
            textBox.CaretIndex = textBox.Text.Length;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.Files)) return;

            var items = e.Data.GetFiles();
            if (items is null || !items.Any()) return;

            if (DataContext is not MainWindowViewModel vm) return;

            // 只处理第一个拖拽项
            var first = items.First();
            var localPath = first.TryGetLocalPath();
            if (localPath is null) return;

            if (first is IStorageFolder)
            {
                // 文件夹：仅在当前标签页为 LTB(0) 或 DTX(1) 时设置路径
                if (MainTabControl?.SelectedIndex == 1)
                    vm.SetDtxInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.PATH);
                else if (MainTabControl?.SelectedIndex == 0)
                    vm.SetInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.PATH);
                // 关于标签页 → 不动作
            }
            else if (first is IStorageFile)
            {
                var ext = System.IO.Path.GetExtension(localPath)?.ToLowerInvariant();
                if (ext == ".ltb")
                {
                    MainTabControl.SelectedIndex = 0;
                    vm.SetInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.FILE);
                }
                else if (ext == ".dtx")
                {
                    MainTabControl.SelectedIndex = 1;
                    vm.SetDtxInputPathFromDrop(localPath, MainWindowViewModel.InputPathType.FILE);
                }
                // 其他扩展名 → 不动作
            }
        }

    }
}