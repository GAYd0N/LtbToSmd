# AGENTS.md — LtbToSmd 项目指南

## 工作规则

1. **方案先行**：在编写任何代码之前，请先描述你的方案并等待批准。如果需求不明确，在编写任何代码之前务必提出澄清问题。
2. **任务分解**：如果一项任务需要修改超过 3 个文件，请先停下来，将其分解成更小的任务。
3. **代码后检查**：编写代码后，列出可能出现的问题，并建议相应的测试用例来覆盖这些问题。
4. **Bug 修复流程**：当发现 bug 时，首先要编写一个能够重现该 bug 的测试，然后不断修复它，直到测试通过为止。
5. **持续改进**：每次我纠正你之后，就在 AGENTS.md 文件中添加一条新规则，这样就不会再发生这种情况了。
6. 使用**简体中文**思考和交流。

## 常用命令

- **构建项目**：`dotnet build LtbToSmd.sln`
- **发布 x64 单文件**：`.\build\publish.ps1`（自动读取 .csproj 版本号，输出到 `publish/v{version}/`）
- **手动发布**：`dotnet publish LtbToSmd/LtbToSmd.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish`
- **运行项目**：`dotnet run --project LtbToSmd/LtbToSmd.csproj`

## 版本管理

- 版本号**唯一来源**：`LtbToSmd.csproj` 中的 `<Version>` 属性
- 程序启动时通过反射读取程序集版本（`Assembly.GetExecutingAssembly().GetName().Version`）
- 窗口标题显示为：`"LtbToSmd v{version}"`
- About 页面显示版本号
- 发布流程：
  1. 修改 `.csproj` 中的 `<Version>`
  2. 运行 `.\build\publish.ps1` 构建发布
  3. 执行 `git tag v{version}` && `git push origin v{version}`
  4. 在 GitHub Releases 上传发布包

当前项目没有测试框架。如需添加测试，建议使用 xUnit + Avalonia.Headless。

## 项目架构概览

LtbToSmd 是一个基于 **Avalonia UI 11.2 + .NET 8** 的桌面应用程序，采用 **MVVM 模式** 构建。主要功能是将 CrossFire 游戏的 LTB 模型文件转换为标准 SMD（StudioMDL）格式，以便在建模软件中使用。

### 技术栈
- **UI 框架**：Avalonia 11.2.1（Fluent 主题，支持 Windows/Linux/macOS）
- **MVVM 工具**：CommunityToolkit.Mvvm 8.2.1（源生成器，`[ObservableProperty]` 和 `[RelayCommand]`）
- **DI 容器**：Microsoft.Extensions.DependencyInjection 9.0
- **压缩库**：LZMA-SDK 22.1.1（用于解压 LTB 文件）
- **数据绑定**：编译绑定（`CompiledBinding`）——启用 `AvaloniaUseCompiledBindingsByDefault`

### 目录结构

```
LtbToSmd/
├── Models/          # 数据模型与接口
│   ├── ILogger.cs           # 日志接口（解耦 Model 与 ViewModel）
│   ├── ILtbConversionConfig.cs  # LTB 转换配置接口
│   ├── CMeshData.cs         # 网格数据类
│   ├── CBoneData.cs         # 骨骼数据类
│   ├── CFramedata.cs        # 帧数据类
│   ├── CAnimData.cs         # 动画数据类
│   └── LTBMeshType.cs       # 网格类型枚举
├── ViewModels/      # ViewModel 层
│   ├── MainWindowViewModel.cs  # 主窗口 ViewModel（实现 ILogger + ILtbConversionConfig）
│   └── ViewModelBase.cs        # 基类
├── Views/           # 视图层（Avalonia XAML + code-behind）
│   ├── MainWindow.axaml     # 主窗口 XAML 布局
│   └── MainWindow.axaml.cs  # 代码后置
├── Services/        # 服务接口 + 业务逻辑实现
│   ├── IFilesService.cs        # 文件对话框抽象接口
│   ├── FileService.cs          # IFilesService 实现
│   ├── ILocalizationService.cs # 本地化服务接口
│   ├── LocalizationService.cs  # 本地化服务实现
│   ├── LTBModel.cs             # LTB 文件解析与 SMD 转换核心服务
│   ├── IDtxService.cs          # DTX 转换服务接口
│   └── DtxService.cs           # DTX → PNG/BMP/TGA 转换实现
├── App.axaml / App.axaml.cs  # 应用入口，DI 注册
├── ViewLocator.cs            # ViewModel → View 映射器
└── Program.cs                # Avalilla 桌面启动入口
```

### 核心数据流

1. **用户选择输入**：通过 `MainWindowViewModel.BrowseForInputPathCommand` 弹出文件/文件夹选择器，通过 `IFilesService` 获取路径。
2. **配置选项**：用户切换拆分手臂、拆分 SMD、提取动画、强制动画原点、生成 QC 等开关。
3. **开始转换**：`StartConvertCommand` 遍历输入路径中所有 `.ltb` 文件，调用 `LtbModel.ConvertToSmd()`。
4. **LTBModel 处理管线**：
   - 读取文件头 → 检查是否 LZMA 压缩 → 解压（如果需要）
   - 解析网格数据（`Parse_mesh` → `Parse_submesh` → `Parse_vertices`）
   - 计算权重（`Calc_weightsets`）
   - 解析骨骼（`parse_skeleton`）
   - 计算父子骨骼关系（`Clac_Par_Bone`）
   - 解析动画（`Parse_animation`）
   - 输出 SMD 模型文件 + 可选的动画文件和 QC 文件
5. **输出**：生成 `.smd` 文件（参考模型的骨骼/网格/三角形数据）、`.smd` 动画文件、`.qc` 编译配置文件。

### 关键内部数据结构

LTBModel 包含四个私有内部类用于表示文件结构：
- **`CMeshData`**：网格名称、顶点/法线/UV/权重/三角形索引列表
- **`CBoneData`**：骨骼名称、父索引、子骨骼数量、4x4 变换矩阵
- **`CAnimData`**：动画名称、关键帧列表、逐帧位置/四元数数据
- **`CFramedata`**：单骨骼在动画中的位置列表和四元数列表

### 依赖注入

DI 在 `App.OnFrameworkInitializationCompleted` 中设置，当前仅注册了 `IFilesService → FilesService`。ViewModel 直接通过 `new MainWindowViewModel()` 构造（非 DI 注入），通过 `App.Current.Services.GetRequiredService<IFilesService>()` 在命令中解析服务。

### 当前状态

- **LTB2SMD**：功能完整
- **DTX2PNG**：功能完整（支持 DTX → PNG/BMP/TGA，含 Indexed BMP、自动缩放、批量转换）
- **转换选项**：支持拆分手臂（索引 >= 2 的网格分文件）、按 2000 顶点分块 SMD、动画提取、QC 生成、强制动画原点等
