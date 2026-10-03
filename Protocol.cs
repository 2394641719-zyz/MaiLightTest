namespace MaiLightTest;

internal record LightState(byte[] Rgb, byte[] Fet);
internal record Command(byte Id, byte[] Payload)
{
    public byte[] Encode()
    {
        if (Payload.Length > 60) throw new ArgumentOutOfRangeException(nameof(Payload));
        byte[] result = new byte[Payload.Length + 4];
        result[0] = 0xff;
        result[1] = Id;
        result[2] = (byte)Payload.Length;
        Payload.CopyTo(result, 3);
        result[^1] = (byte)result[..^1].Sum(b => (int)b);
        return result; // USB CDC has no escaping; FF/FD are literal payload bytes.
    }
}

internal sealed class SendSchedule
{
    private long lastFrame = -10000, lastHeartbeat = -10000, lastRgb = -10000, lastFet = -10000;
    private byte[]? rgb, fet;
    private bool preferRgb = true;

    public Command? Next(long now, LightState desired, int gap)
    {
        gap = Math.Clamp(gap, 10, 40);
        if (now - lastFrame < gap) return null;
        Command? command;
        if (now - lastHeartbeat >= 100)
        {
            lastHeartbeat = now;
            command = new(0x11, []);
        }
        else
        {
            bool rgbDue = rgb is null || !rgb.AsSpan().SequenceEqual(desired.Rgb) || now - lastRgb >= 1000;
            bool fetDue = fet is null || !fet.AsSpan().SequenceEqual(desired.Fet) || now - lastFet >= 1000;
            if (rgbDue && (preferRgb || !fetDue))
            {
                rgb = (byte[])desired.Rgb.Clone();
                lastRgb = now;
                preferRgb = false;
                command = new(0x14, rgb);
            }
            else if (fetDue)
            {
                fet = (byte[])desired.Fet.Clone();
                lastFet = now;
                preferRgb = true;
                command = new(0x16, fet);
            }
            else return null;
        }
        lastFrame = now;
        return command;
    }
}

internal sealed class LightModel
{
    private readonly object gate = new();
    private readonly Color[] colors = Enumerable.Repeat(Color.Black, 8).ToArray();
    private readonly byte[] fet = new byte[3];
    private int brightness = 64;
    public void SetColors(IEnumerable<int> indices, Color color)
    {
        lock (gate) foreach (int i in indices) colors[i] = color;
    }
    public void SetChase(int index, Color color)
    {
        lock (gate) { Array.Fill(colors, Color.Black); colors[index] = color; }
    }
    public void SetBrightness(int value) { lock (gate) brightness = Math.Clamp(value, 0, 255); }
    public int Brightness { get { lock (gate) return brightness; } }
    public byte[] Fet { get { lock (gate) return (byte[])fet.Clone(); } }
    public void SetFet(int index, byte value) { lock (gate) fet[index] = value; }
    public void Blackout() { lock (gate) { Array.Fill(colors, Color.Black); Array.Clear(fet); } }
    public LightState Snapshot()
    {
        lock (gate)
        {
            var rgb = new byte[24];
            for (int i = 0; i < 8; i++)
            {
                rgb[i * 3] = (byte)(colors[i].R * brightness / 255);
                rgb[i * 3 + 1] = (byte)(colors[i].G * brightness / 255);
                rgb[i * 3 + 2] = (byte)(colors[i].B * brightness / 255);
            }
            return new(rgb, (byte[])fet.Clone());
        }
    }
}
