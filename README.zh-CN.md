# Desktop Icons

Windows 桌面图标布局管理工具，按显示器配置保存和还原图标位置。它是 ReIcon 的开源替代品，界面简洁。

[English](README.md)

## 功能

- 保存和还原命名的桌面布局
- 按显示器配置自动分类存储布局（自动识别当前显示器指纹）
- 托盘菜单快捷控制
- 可选 **开机启动**
- 可选 **图标移动时自动还原**：当桌面图标位置偏离已保存布局时，自动重新套用最近一次还原的布局

## 系统要求

- Windows 10 21H1（内部版本 19041）或更高，x64
- 无需单独安装 .NET，安装包为自包含发布

## 安装

从 [Releases 页面](https://github.com/awly333/windows-desktop-icons/releases) 下载最新的 `DesktopIcons-Setup-x.y.z.exe` 并运行。

安装时可选择安装范围：

- **仅当前用户**（默认）：安装到 `%LocalAppData%\Programs\Desktop Icons`，无需管理员权限
- **所有用户**：安装到 `Program Files\Desktop Icons`，需要管理员权限

标准 Inno Setup 安装向导里可以修改安装目录。

用户数据存放在 `%LocalAppData%\DesktopIcons\`（布局和设置）。卸载不会删除这些数据。

## 从源码构建

```powershell
# 安装依赖
winget install Microsoft.DotNet.SDK.8
winget install JRSoftware.InnoSetup

# 直接运行
dotnet build src\DesktopIcons.App\DesktopIcons.App.csproj -c Debug
.\src\DesktopIcons.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\DesktopIcons.App.exe

# 构建安装包
powershell.exe -ExecutionPolicy Bypass -File tools\build-installer.ps1
# 输出：installer-output\DesktopIcons-Setup-x.y.z.exe
```

## 项目结构

```text
src\
  DesktopIcons.Core\   # 共享库（桌面互操作、显示器指纹、存储）
  DesktopIcons.Cli\    # CLI 工具：dump / apply / save / restore / list / delete
  DesktopIcons.App\    # WinUI 3 桌面应用（unpackaged，自包含）
installer\             # Inno Setup 脚本
tools\                 # build-icon.ps1、build-installer.ps1
```

## CLI 命令行工具

除了图形界面外，还提供独立的命令行工具。

**通过 Scoop 安装（无需管理员权限）：**

```powershell
scoop install https://raw.githubusercontent.com/awly333/windows-desktop-icons/main/scoop/desktop-icons.json
```

**命令列表：**

```text
di save <名称>        保存当前桌面布局
di restore <名称>     还原已保存布局
di list               列出当前显示器配置下的布局
di list --all         列出所有显示器配置下的布局
di delete <名称>      删除布局
di --version          显示版本
```

布局名称支持空格。布局会按显示器配置隔离存储，切换显示器配置时不会互相覆盖。

**高级用法：**

```text
di dump <路径>        将当前布局导出到指定 JSON 文件
di apply <路径>       从指定 JSON 文件还原布局
```

## 许可证

MIT，详见 [LICENSE](LICENSE)。
