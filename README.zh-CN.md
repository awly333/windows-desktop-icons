# Desktop Icons

Windows 桌面图标布局管理工具，按显示器配置保存和还原图标位置。ReIcon 的开源替代品，界面简洁。

[English README](README.md)

## 功能

- 保存和还原命名的桌面布局
- 按显示器配置自动归类（自动识别当前显示器指纹）
- 系统托盘快速操作菜单
- 可选**开机自启**
- 可选**显示器变更时自动还原** — 接插显示器或更改分辨率后自动套用上次布局

## 系统要求

- Windows 10 21H1（内部版本 19041）或更高，x64
- 无需单独安装 .NET — 安装包已自带运行时

## 安装

从 [Releases 页面](https://github.com/awly333/windows-desktop-icons/releases) 下载最新的 `DesktopIcons-Setup-x.y.z.exe` 并运行。

安装时可选择安装范围：

- **仅当前用户**（默认）：安装到 `%LocalAppData%\Programs\Desktop Icons`，无需管理员权限
- **所有用户**：安装到 `Program Files\Desktop Icons`，需要管理员权限

用户数据保存在 `%LocalAppData%\DesktopIcons\`（布局文件和设置）。卸载时不会删除用户数据。

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

```
src\
  DesktopIcons.Core\   # 共享库（桌面交互、显示器指纹、存储）
  DesktopIcons.Cli\    # CLI 工具：dump / apply / save / restore / list / delete
  DesktopIcons.App\    # WinUI 3 桌面应用（unpackaged，自包含）
installer\             # Inno Setup 脚本
tools\                 # build-icon.ps1、build-installer.ps1
```

## 许可证

MIT — 详见 [LICENSE](LICENSE)。
