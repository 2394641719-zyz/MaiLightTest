namespace MaiLightTest;

internal enum OutputRoute { SerialRgb, SerialFet, JvsPwmUnverified, JvsSwitchUnverified, DisabledByManual }

internal sealed record OutputZone(string Name, int? Board, OutputRoute Route, int? Channel = null);

// Manual section 4.2.3 describes thirteen output-test entries. Keep the
// catalogue separate from the transport: a listed output is not necessarily
// reachable through the LED board's serial port.
internal static class OutputCatalog
{
    public static readonly OutputZone[] Zones =
    [
        new("按键灯光", 0, OutputRoute.SerialRgb),
        new("框体机身灯光", 0, OutputRoute.SerialFet, 0),
        new("画面外圈灯光", 0, OutputRoute.SerialFet, 1),
        new("侧盖灯光", 0, OutputRoute.SerialFet, 2),
        new("广告牌灯光", 0, OutputRoute.JvsPwmUnverified),
        new("按键灯光", 1, OutputRoute.SerialRgb),
        new("框体机身灯光", 1, OutputRoute.SerialFet, 0),
        new("画面外圈灯光", 1, OutputRoute.SerialFet, 1),
        new("侧盖灯光", 1, OutputRoute.SerialFet, 2),
        new("广告牌灯光", 1, OutputRoute.JvsPwmUnverified),
        new("摄像机 RING LED", null, OutputRoute.JvsSwitchUnverified, 3),
        new("摄像机 REC LED", null, OutputRoute.JvsSwitchUnverified, 4),
        new("禁止投币", null, OutputRoute.DisabledByManual),
    ];

    public static string FetName(int channel) => Zones.First(z => z.Board == 0 && z.Route == OutputRoute.SerialFet && z.Channel == channel).Name;
}
