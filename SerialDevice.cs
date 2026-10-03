using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace MaiLightTest;

internal static class PortDiscovery
{
    public static string[] List()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
        return key?.GetValueNames().Select(n => key.GetValue(n)?.ToString())
            .OfType<string>().Distinct().OrderBy(p => p.Length).ThenBy(p => p).ToArray() ?? [];
    }
    public static string? Detect(int board)
    {
        using var usb = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
        if (usb is null) return null;
        string pid = board == 0 ? "PID_52A5" : "PID_52A6";
        var present = List();
        foreach (string name in usb.GetSubKeyNames().Where(n => n.Contains("VID_AFF1", StringComparison.OrdinalIgnoreCase) && n.Contains(pid, StringComparison.OrdinalIgnoreCase)))
        {
            using var device = usb.OpenSubKey(name);
            if (device is null) continue;
            foreach (string instance in device.GetSubKeyNames())
            {
                using var parameters = device.OpenSubKey(instance + @"\Device Parameters");
                var port = parameters?.GetValue("PortName")?.ToString();
                if (port is not null && present.Contains(port)) return port;
            }
        }
        return null;
    }
}

internal sealed class SerialDevice(LightModel model)
{
    private CancellationTokenSource? cancellation;
    private Task? worker;
    private volatile bool connected, turnOff;
    private long sent;
    public volatile int Gap = 30;
    public volatile bool Detailed;
    public bool Connected => connected;
    public long Sent => Interlocked.Read(ref sent);
    public string Port { get; private set; } = "";
    public event Action<string>? Message;

    public async Task ConnectAsync(string port)
    {
        if (worker is not null) await DisconnectAsync();
        if (!System.Text.RegularExpressions.Regex.IsMatch(port, "^COM[1-9][0-9]*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            throw new ArgumentException("请输入有效串口名，例如 COM11。");
        var handle = await Task.Run(() => NativeSerial.Open(port));
        Port = port;
        Interlocked.Exchange(ref sent, 0);
        turnOff = false;
        cancellation = new();
        var token = cancellation.Token;
        connected = true;
        Message?.Invoke($"已连接 {port}（115200 / 8N1）");
        worker = Task.Run(async () =>
        {
            var schedule = new SendSchedule();
            var input = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var command = schedule.Next(Environment.TickCount64, model.Snapshot(), Gap);
                    if (command is not null)
                    {
                        var frame = command.Encode();
                        NativeSerial.Write(handle, frame);
                        Interlocked.Increment(ref sent);
                        if (Detailed) Message?.Invoke("TX " + Convert.ToHexString(frame));
                    }
                    NativeSerial.Drain(handle, input); // Drain unsolicited touch reports; they are not LED ACKs.
                    await Task.Delay(5, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Message?.Invoke("通信中断：" + e.Message); }
            finally
            {
                if (turnOff)
                {
                    try
                    {
                        await Task.Delay(40);
                        NativeSerial.Write(handle, new Command(0x14, new byte[24]).Encode());
                        await Task.Delay(40);
                        NativeSerial.Write(handle, new Command(0x16, new byte[3]).Encode());
                        Message?.Invoke("已发送熄灯指令（控制板不回传确认）");
                    }
                    catch { Message?.Invoke("断开时未能发送熄灯指令。"); }
                }
                handle.Dispose();
                connected = false;
                Message?.Invoke("已断开 " + Port);
            }
        });
    }

    public async Task DisconnectAsync()
    {
        turnOff = true;
        cancellation?.Cancel();
        if (worker is not null) await worker;
        worker = null;
        cancellation?.Dispose();
        cancellation = null;
    }
}

internal static class NativeSerial
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Dcb
    {
        public uint Length, BaudRate, Flags;
        public ushort Reserved, XonLim, XoffLim;
        public byte ByteSize, Parity, StopBits, XonChar, XoffChar, ErrorChar, EofChar, EvtChar;
        public ushort Reserved1;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Timeouts { public uint Interval, ReadMultiplier, ReadConstant, WriteMultiplier, WriteConstant; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetCommState(SafeFileHandle handle, ref Dcb dcb);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCommState(SafeFileHandle handle, ref Dcb dcb);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetCommTimeouts(SafeFileHandle handle, ref Timeouts timeouts);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool WriteFile(SafeFileHandle handle, byte[] bytes, uint size, out uint written, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(SafeFileHandle handle, [Out] byte[] bytes, uint size, out uint read, IntPtr overlapped);
    private static void Check(bool success) { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }

    public static SafeFileHandle Open(string port)
    {
        var handle = CreateFile(@"\\.\" + port, 0xc0000000, 0, IntPtr.Zero, 3, 0, IntPtr.Zero);
        if (handle.IsInvalid) { var error = new Win32Exception(Marshal.GetLastWin32Error()); handle.Dispose(); throw error; }
        try
        {
            var dcb = new Dcb { Length = (uint)Marshal.SizeOf<Dcb>() };
            Check(GetCommState(handle, ref dcb));
            dcb.BaudRate = 115200; dcb.Flags = 0x1011; dcb.ByteSize = 8; dcb.Parity = 0; dcb.StopBits = 0;
            Check(SetCommState(handle, ref dcb));
            var timeouts = new Timeouts { Interval = uint.MaxValue, ReadConstant = 1, WriteConstant = 100 };
            Check(SetCommTimeouts(handle, ref timeouts));
            return handle;
        }
        catch { handle.Dispose(); throw; }
    }
    public static void Write(SafeFileHandle handle, byte[] frame)
    {
        Check(WriteFile(handle, frame, (uint)frame.Length, out uint count, IntPtr.Zero));
        if (count != frame.Length) throw new IOException("串口写入不完整。");
    }
    public static void Drain(SafeFileHandle handle, byte[] buffer) => Check(ReadFile(handle, buffer, (uint)buffer.Length, out _, IntPtr.Zero));
}
