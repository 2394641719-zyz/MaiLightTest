# MaiLightTest / Mai Light Lab

面向 **Curva 开源主控** 的独立灯光测试工具。上游项目：[QHPaeek/Mai_stm32](https://github.com/QHPaeek/Mai_stm32)，当前适配参考 G431 分支。感谢 Curva 项目及其维护者。

Windows x64 灯光测试台，直接测试 Affine / MAI USB-CDC 控制板，无需运行游戏或加载游戏 DLL。

## 使用

运行发布包里的 `MaiLightTest.exe`。自包含单文件版无需额外安装 .NET 运行库。先退出游戏和其他占用串口的软件，再自动识别或输入 1P/2P COM 端口，点击“连接”后才会发送数据。

支持每侧八键 RGB 灯、机身三路 FET PWM、亮度、预设颜色及跑马灯。断开时会尝试发送全灭帧。程序显示的发送计数不代表控制板确认接收。

广告牌、摄像机 RING/REC 等 JVS 输出当前仅展示分类，协议未确认，不发送猜测命令。“禁止投币”不提供控制。输出项目见 [输出项目核对.md](输出项目核对.md)，操作说明见 [USAGE.txt](USAGE.txt)。

## 构建

需要 Windows、.NET 10 SDK 和 Windows Desktop targeting pack。

```powershell
.\build.ps1 -RunTests
```

脚本发布 Windows x64 自包含单文件程序到 `artifacts/`，并运行内置协议/模型自测。也可指定 `-OutputDir`。

开发运行：

```powershell
dotnet run --project .\MaiLightTest.csproj -c Release
```

单独运行自测：

```powershell
dotnet run --project .\MaiLightTest.csproj -c Release -- --self-test .\artifacts\self-test-report.txt
```

自测不连接硬件，不能替代现场灯光效果验证。当前测试板未接灯，实体效果仍待验证。

## 协议与源码

串口原生帧中，命令 `0x14` 携带24字节 RGB，`0x16` 携带3字节 PWM，并有100ms心跳。发送间隔可设为10–40ms。

- `MainForm.cs`：界面与操作。
- `SerialDevice.cs`：设备发现和串口发送。
- `Protocol.cs`：灯光模型、帧编码和发送调度。
- `OutputCatalog.cs`：输出项目分类。
- `Program.cs`：启动、自测和预览入口。

## 发布准备

源码目录不包含游戏文件、固件、用户设备配置或第三方可执行文件。构建产物可上传到 GitHub Release。本项目采用 [MIT 许可证](LICENSE)。第三方文件的授权范围见 [LICENSE-NOTICE.md](LICENSE-NOTICE.md)。
