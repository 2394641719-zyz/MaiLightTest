# MaiLightTest

[Curva G431 主控](https://github.com/QHPaeek/Mai_stm32/tree/G431)的灯光测试软件。直接通过串口控制灯板，可以单独检查灯光接线和颜色，不需要启动游戏。

## 怎么用

打开 `MaiLightTest.exe` 前，先退出游戏和其他占用主控串口的软件。

在 1P / 2P 区域点击“自动识别”，或手动填写 COM 端口，然后点击“连接”。连接成功后就可以选择按键灯、调整颜色和亮度，也可以用跑马灯检查每一路输出。

目前支持每侧八个按键 RGB 灯，以及机身、画面外圈、侧盖三路 PWM 灯光。可以分别测试 1P、2P，也可以双侧同步。断开连接时会尝试把灯熄灭。

广告牌和摄像机 RING / REC 灯属于 JVS 输出，目前还不能用这个软件测试。界面中的“已发送”表示串口数据已发出，实际是否点亮要看灯板。输出项目说明见 [输出项目核对.md](输出项目核对.md)。

发布版是 Windows x64 单文件程序，已带运行库，不需要另装 .NET。更多操作说明见 [USAGE.txt](USAGE.txt)。

## 编译

在 Windows 上安装 .NET 10 SDK，然后运行：

```powershell
.\build.ps1 -RunTests
```

成品在 `artifacts/`。这个命令会同时运行帧编码、亮度和发送调度等自测；实际灯光效果仍需接灯检查。

开发时直接运行：

```powershell
dotnet run --project .\MaiLightTest.csproj -c Release
```

## 致谢与许可

感谢 [QHPaeek/Mai_stm32](https://github.com/QHPaeek/Mai_stm32) 提供 Curva 主控项目。

本工具源码采用 [MIT](LICENSE) 许可证，版权署名为 `2394641719-zyz`。主控设计、固件及其他第三方组件保留各自授权，见 [LICENSE-NOTICE.md](LICENSE-NOTICE.md)。
