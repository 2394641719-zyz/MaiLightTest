namespace MaiLightTest;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--self-test")
            return SelfTest.Run(args.Length > 1 ? args[1] : null);
        if (args.Length > 1 && args[0] == "--preview")
        {
            ApplicationConfiguration.Initialize();
            using var form = new MainForm();
            if (args.Length > 2 && int.TryParse(args[2], out int tab)) form.SelectPreviewTab(tab);
            if (args.Length > 4 && int.TryParse(args[3], out int width) && int.TryParse(args[4], out int height))
                form.ClientSize = new Size(width, height);
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-32000, -32000);
            form.Shown += (_, _) =>
            {
                Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(args[1]);
                form.BeginInvoke(form.Close);
            };
            Application.Run(form);
            return 0;
        }
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}

internal static class SelfTest
{
    public static int Run(string? output)
    {
        try
        {
            var fet = new byte[] { 0xff, 0xfd, 0x7b };
            var frame = new Command(0x16, fet).Encode();
            Assert(frame.SequenceEqual(new byte[] { 0xff, 0x16, 3, 0xff, 0xfd, 0x7b, 0x8f }), "FET frame checksum and literal FF/FD");
            var rgb = Enumerable.Range(0, 24).Select(i => (byte)i).ToArray();
            var led = new Command(0x14, rgb).Encode();
            Assert(led.Length == 28 && led[1] == 0x14 && led[2] == 24 && led[3] == 0 && led[26] == 23, "RGB command framing");
            Assert(led[^1] == unchecked((byte)led[..^1].Sum(x => x)), "RGB checksum");
            var model = new LightModel();
            model.SetColors(new[] { 0, 7 }, Color.FromArgb(255, 128, 64));
            Assert(model.Snapshot().Rgb.SequenceEqual(new byte[] { 64, 32, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 64, 32, 16 }), "brightness and per-button mapping");
            model.SetBrightness(255);
            Assert(model.Snapshot().Rgb[0] == 255, "brightness maximum");
            var schedule = new SendSchedule();
            var state = new LightModel().Snapshot();
            Assert(schedule.Next(0, state, 30)?.Id == 0x11, "initial heartbeat");
            Assert(schedule.Next(20, state, 30) is null, "minimum send interval");
            Assert(schedule.Next(100, state, 30)?.Id == 0x11, "heartbeat deadline");
            Assert(schedule.Next(130, state, 30)?.Id == 0x14, "RGB scheduled after heartbeat");
            Assert(schedule.Next(160, state, 30)?.Id == 0x16, "PWM alternates with RGB");
            Assert(schedule.Next(190, state, 30) is null, "unchanged data is suppressed");
            Assert(schedule.Next(220, state, 30)?.Id == 0x11, "heartbeat remains prioritized");
            Assert(OutputCatalog.Zones.Length == 13, "manual output catalogue count");
            Assert(OutputCatalog.Zones.Count(z => z.Route == OutputRoute.SerialRgb) == 2, "two button groups");
            Assert(OutputCatalog.Zones.Count(z => z.Route == OutputRoute.SerialFet) == 6, "three FET channels per board");
            Assert(OutputCatalog.Zones.Count(z => z.Route == OutputRoute.JvsPwmUnverified) == 2, "two JVS PWM billboards");
            Assert(OutputCatalog.Zones.Count(z => z.Route == OutputRoute.JvsSwitchUnverified) == 2, "two JVS camera switch outputs");
            Assert(OutputCatalog.Zones.Single(z => z.Route == OutputRoute.DisabledByManual).Name == "禁止投币", "coin inhibit is disabled in manual");
            File.WriteAllText(output ?? "self-test-report.txt", "PASS: framing, checksum, literal payload bytes, per-button color, brightness, heartbeat priority, interval, alternating LED frames, duplicate suppression, 13 manual output categories");
            return 0;
        }
        catch (Exception e)
        {
            File.WriteAllText(output ?? "self-test-report.txt", "FAIL: " + e);
            return 1;
        }
    }
    private static void Assert(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
