namespace MaiLightTest;

internal sealed class MainForm : Form
{
    private readonly LightModel[] models = [new(), new()];
    private readonly SerialDevice[] devices;
    private readonly ComboBox[] ports = new ComboBox[2];
    private readonly Button[] connectButtons = new Button[2];
    private readonly Label[] connectionStatus = new Label[2];
    private readonly CheckBox[] lamps = new CheckBox[8];
    private readonly TrackBar[] fetBars = new TrackBar[3];
    private readonly Label[] fetValues = new Label[3];
    private readonly ComboBox target = new();
    private readonly TrackBar brightness = new();
    private readonly Label brightnessValue = new();
    private readonly Button colorPreview = new();
    private readonly CheckBox chase = new();
    private readonly CheckBox verbose = new();
    private readonly NumericUpDown interval = new();
    private readonly TextBox log = new();
    private TabControl? outputTabs;
    private readonly System.Windows.Forms.Timer uiTimer = new() { Interval = 100 };
    private readonly System.Windows.Forms.Timer chaseTimer = new() { Interval = 180 };
    private Color selectedColor = Color.FromArgb(0, 210, 180);
    private int chaseIndex = -1;
    private bool changingUi, closing, closeReady;
    private readonly Task[] connectOperations = [Task.CompletedTask, Task.CompletedTask];

    public MainForm()
    {
        Text = "Mai Light Lab · 灯光测试台";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1120, 820);
        ClientSize = new Size(1180, 790);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(241, 245, 249);
        ForeColor = Color.FromArgb(37, 49, 64);
        AutoScaleMode = AutoScaleMode.Dpi;
        devices = [new(models[0]), new(models[1])];
        for (int i = 0; i < 2; i++)
        {
            int side = i;
            devices[i].Message += m => AddLog($"{(side == 0 ? "1P" : "2P")}  {m}");
        }
        BuildUi();
        Shown += (_, _) => RefreshPorts();
        SyncTargetControls();
        uiTimer.Tick += (_, _) => UpdateStatus();
        uiTimer.Start();
        chaseTimer.Tick += (_, _) => AdvanceChase();
        FormClosing += ClosingAsync;
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14, 12, 14, 12), ColumnCount = 1, RowCount = 4 };
        root.RowStyles.Add(new(SizeType.Absolute, 82));
        root.RowStyles.Add(new(SizeType.Absolute, 108));
        root.RowStyles.Add(new(SizeType.Percent, 100));
        root.RowStyles.Add(new(SizeType.Absolute, 126));
        Controls.Add(root);
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildConnections(), 0, 1);
        root.Controls.Add(BuildWorkArea(), 0, 2);
        root.Controls.Add(BuildLog(), 0, 3);
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(22, 38, 58), Padding = new Padding(20, 9, 20, 9), Margin = new Padding(4, 2, 4, 6) };
        panel.ColumnStyles.Add(new(SizeType.Percent, 100)); panel.ColumnStyles.Add(new(SizeType.AutoSize));
        var texts = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        texts.Controls.Add(new Label { Text = "Mai Light Lab", AutoSize = true, Font = new Font(Font.FontFamily, 18, FontStyle.Bold), ForeColor = Color.White, Margin = new(0, 0, 0, 0) });
        texts.Controls.Add(new Label { Text = "控制板灯光测试台   /   按说明书分区", AutoSize = true, ForeColor = Color.FromArgb(174, 194, 211), Margin = new(2, 0, 0, 0) });
        panel.Controls.Add(texts, 0, 0);
        var safety = new Label { Text = "●  点击连接后才会发送数据", AutoSize = true, ForeColor = Color.FromArgb(112, 226, 194), Anchor = AnchorStyles.Right, Margin = new(0, 0, 0, 0) };
        panel.Controls.Add(safety, 1, 0);
        return panel;
    }

    private Control BuildConnections()
    {
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 0, 0, 6) };
        cards.ColumnStyles.Add(new(SizeType.Percent, 50)); cards.ColumnStyles.Add(new(SizeType.Percent, 50));
        for (int i = 0; i < 2; i++)
        {
            int side = i;
            var card = Card();
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, Padding = new Padding(13, 9, 13, 7) };
            row.ColumnStyles.Add(new(SizeType.Absolute, 43)); row.ColumnStyles.Add(new(SizeType.Percent, 100)); row.ColumnStyles.Add(new(SizeType.Absolute, 38)); row.ColumnStyles.Add(new(SizeType.Absolute, 88)); row.ColumnStyles.Add(new(SizeType.Absolute, 73));
            row.RowStyles.Add(new(SizeType.Percent, 58)); row.RowStyles.Add(new(SizeType.Percent, 42));
            row.Controls.Add(new Label { Text = i == 0 ? "1P" : "2P", AutoSize = true, Anchor = AnchorStyles.Left, Font = new(Font.FontFamily, 12, FontStyle.Bold), ForeColor = i == 0 ? AccentColor : Color.FromArgb(93, 103, 218) }, 0, 0);
            ports[i] = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems, Margin = new(2, 3, 4, 3) };
            row.Controls.Add(ports[i], 1, 0);
            var refresh = new Button { Text = "↻", Dock = DockStyle.Fill, Margin = new(1, 2, 4, 2), FlatStyle = FlatStyle.Flat, ForeColor = Muted };
            refresh.FlatAppearance.BorderColor = BorderColor;
            refresh.Click += (_, _) => RefreshPorts();
            row.Controls.Add(refresh, 2, 0);
            var detect = new Button { Text = "自动识别", Dock = DockStyle.Fill, Margin = new(1, 2, 3, 2), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(243, 247, 250), ForeColor = Color.FromArgb(62, 78, 96) };
            detect.FlatAppearance.BorderColor = BorderColor;
            detect.Click += (_, _) => DetectPort(side);
            row.Controls.Add(detect, 3, 0);
            connectButtons[i] = new Button { Text = "连接", Dock = DockStyle.Fill, Margin = new(1, 2, 1, 2), BackColor = AccentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            connectButtons[i].FlatAppearance.BorderSize = 0;
            connectButtons[i].Click += (_, _) => connectOperations[side] = ToggleConnection(side);
            row.Controls.Add(connectButtons[i], 4, 0);
            connectionStatus[i] = new Label { Text = "未连接", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted, Margin = new(2, 2, 0, 0) };
            row.Controls.Add(connectionStatus[i], 1, 1);
            row.SetColumnSpan(connectionStatus[i], 4);
            card.Controls.Add(row);
            cards.Controls.Add(card, i, 0);
        }
        return cards;
    }

    private Control BuildWorkArea()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(4, 0, 4, 4), Font = new Font(Font, FontStyle.Bold) };
        outputTabs = tabs;
        var first = new TabPage("1P / 2P 灯光") { BackColor = BackColor, Padding = new Padding(0, 5, 0, 0) };
        var common = new TabPage("公共设备") { BackColor = BackColor, Padding = new Padding(0, 5, 0, 0) };
        tabs.TabPages.AddRange([first, common]);
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 0, 0, 4) };
        columns.ColumnStyles.Add(new(SizeType.Percent, 57)); columns.ColumnStyles.Add(new(SizeType.Percent, 43));
        columns.Controls.Add(BuildButtonLights(), 0, 0);
        columns.Controls.Add(BuildCabinetLights(), 1, 0);
        first.Controls.Add(columns);
        common.Controls.Add(BuildCommonOutputs());
        return tabs;
    }

    private Control BuildButtonLights()
    {
        var card = Card(new Padding(16, 13, 16, 11));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new(SizeType.Absolute, 40)); layout.RowStyles.Add(new(SizeType.Absolute, 39));
        layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 61)); layout.RowStyles.Add(new(SizeType.Absolute, 55));
        card.Controls.Add(layout);
        var title = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        title.ColumnStyles.Add(new(SizeType.Percent, 100)); title.ColumnStyles.Add(new(SizeType.Absolute, 154));
        title.Controls.Add(SectionTitle("按键灯", "8 个独立通道 · RGB"), 0, 0);
        target.DropDownStyle = ComboBoxStyle.DropDownList; target.Dock = DockStyle.Fill; target.Margin = new(2, 3, 0, 5);
        target.Items.AddRange(["只测 1P", "只测 2P", "双侧同步"]);
        target.SelectedIndex = 0;
        target.SelectedIndexChanged += (_, _) => { if (!changingUi) SyncTargetControls(); };
        title.Controls.Add(target, 1, 0);
        layout.Controls.Add(title, 0, 0);
        var selectButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new(0, 3, 0, 0) };
        selectButtons.Controls.Add(MiniButton("全选", (_, _) => SetLampSelection(true)));
        selectButtons.Controls.Add(MiniButton("取消选择", (_, _) => SetLampSelection(false)));
        selectButtons.Controls.Add(MiniButton("全灭", (_, _) => BlackoutTarget()));
        layout.Controls.Add(selectButtons, 0, 1);
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2, Padding = new Padding(0, 5, 0, 8) };
        for (int c = 0; c < 4; c++) grid.ColumnStyles.Add(new(SizeType.Percent, 25));
        for (int r = 0; r < 2; r++) grid.RowStyles.Add(new(SizeType.Percent, 50));
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            lamps[i] = new CheckBox { Appearance = Appearance.Button, Text = $"{i + 1}\n选中", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Margin = new(4), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(91, 106, 123), Checked = true, Font = new(Font.FontFamily, 10, FontStyle.Bold) };
            lamps[i].FlatAppearance.BorderColor = BorderColor;
            lamps[i].CheckedChanged += (_, _) => RefreshLampTiles();
            grid.Controls.Add(lamps[i], i % 4, i / 4);
        }
        layout.Controls.Add(grid, 0, 2);
        var colorRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Padding = new Padding(0, 2, 0, 5) };
        colorRow.ColumnStyles.Add(new(SizeType.Absolute, 112)); colorRow.ColumnStyles.Add(new(SizeType.Percent, 100)); colorRow.ColumnStyles.Add(new(SizeType.Absolute, 55)); colorRow.ColumnStyles.Add(new(SizeType.Absolute, 55)); colorRow.ColumnStyles.Add(new(SizeType.Absolute, 55));
        colorPreview.Text = "自选颜色"; colorPreview.Dock = DockStyle.Fill; colorPreview.FlatStyle = FlatStyle.Flat; colorPreview.Margin = new Padding(0, 5, 6, 5); colorPreview.Click += (_, _) => PickColor(); colorRow.Controls.Add(colorPreview, 0, 0);
        colorRow.Controls.Add(new Label { Text = "设置已选按键的颜色", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted }, 1, 0);
        AddPreset(colorRow, 2, "红", Color.Red); AddPreset(colorRow, 3, "绿", Color.Lime); AddPreset(colorRow, 4, "蓝", Color.Blue);
        layout.Controls.Add(colorRow, 0, 3);
        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 2 };
        bottom.ColumnStyles.Add(new(SizeType.Absolute, 64)); bottom.ColumnStyles.Add(new(SizeType.Percent, 100)); bottom.ColumnStyles.Add(new(SizeType.Absolute, 40)); bottom.ColumnStyles.Add(new(SizeType.Absolute, 82)); bottom.ColumnStyles.Add(new(SizeType.Absolute, 56)); bottom.ColumnStyles.Add(new(SizeType.Absolute, 58));
        bottom.RowStyles.Add(new(SizeType.Percent, 68)); bottom.RowStyles.Add(new(SizeType.Percent, 32));
        bottom.Controls.Add(new Label { Text = "总亮度", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted }, 0, 0);
        brightness.Minimum = 0; brightness.Maximum = 255; brightness.TickFrequency = 32; brightness.SmallChange = 8; brightness.LargeChange = 16; brightness.Value = 255; brightness.Dock = DockStyle.Fill; brightness.Margin = new(0, 0, 5, 0);
        brightness.Scroll += (_, _) => ApplyBrightness(); bottom.Controls.Add(brightness, 1, 0);
        bottom.Controls.Add(brightnessValue, 2, 0);
        chase.Text = "跑马灯"; chase.AutoSize = true; chase.Anchor = AnchorStyles.Left; chase.CheckedChanged += (_, _) => { if (chase.Checked) chaseTimer.Start(); else chaseTimer.Stop(); };
        bottom.Controls.Add(chase, 3, 0);
        bottom.Controls.Add(new Label { Text = "间隔", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted }, 4, 0);
        interval.Minimum = 10; interval.Maximum = 40; interval.Value = 30; interval.Increment = 5; interval.Width = 55; interval.Margin = new(0, 2, 0, 0); bottom.Controls.Add(interval, 5, 0);
        var intervalNote = new Label { Text = "10–40 ms/帧 · RGB 0–255", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted, Margin = new Padding(0, 0, 0, 0) };
        bottom.Controls.Add(intervalNote, 0, 1); bottom.SetColumnSpan(intervalNote, 6);
        layout.Controls.Add(bottom, 0, 4);
        UpdateColorPreview(); RefreshLampTiles();
        return card;
    }

    private Control BuildCabinetLights()
    {
        var card = Card(new Padding(16, 13, 16, 11));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new(SizeType.Absolute, 40)); layout.RowStyles.Add(new(SizeType.Absolute, 30)); layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.Absolute, 46)); layout.RowStyles.Add(new(SizeType.Absolute, 42)); layout.RowStyles.Add(new(SizeType.Absolute, 40));
        card.Controls.Add(layout);
        layout.Controls.Add(SectionTitle("框体灯", "三路亮度 · 0–255"), 0, 0);
        var descriptions = new Label { Text = "对应说明书的三组开关型灯区。", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted, Margin = new(2, 0, 0, 0) };
        layout.Controls.Add(descriptions, 0, 1);
        var sliders = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 5, 0, 0) };
        for (int i = 0; i < 3; i++) sliders.RowStyles.Add(new(SizeType.Percent, 33.333f));
        string[] names = ["Body  " + OutputCatalog.FetName(0), "Circle  " + OutputCatalog.FetName(1), "Side  " + OutputCatalog.FetName(2)];
        for (int i = 0; i < 3; i++)
        {
            int channel = i;
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(2, 3, 2, 1) };
            row.ColumnStyles.Add(new(SizeType.Absolute, 146)); row.ColumnStyles.Add(new(SizeType.Percent, 100)); row.ColumnStyles.Add(new(SizeType.Absolute, 44));
            row.RowStyles.Add(new(SizeType.Percent, 45)); row.RowStyles.Add(new(SizeType.Percent, 55));
            row.Controls.Add(new Label { Text = names[i], AutoSize = true, Anchor = AnchorStyles.Left, Font = new(Font.FontFamily, 9, FontStyle.Bold), ForeColor = Color.FromArgb(63, 78, 95) }, 0, 0);
            fetValues[i] = new Label { Text = "0", AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = Muted }; row.Controls.Add(fetValues[i], 2, 0);
            fetBars[i] = new TrackBar { Minimum = 0, Maximum = 255, TickFrequency = 32, SmallChange = 8, LargeChange = 16, Dock = DockStyle.Fill, Margin = new(0, 0, 0, 0), Height = 32 };
            fetBars[i].Scroll += (_, _) => { fetValues[channel].Text = fetBars[channel].Value.ToString(); SetFet(channel, (byte)fetBars[channel].Value); };
            row.Controls.Add(fetBars[i], 0, 1); row.SetColumnSpan(fetBars[i], 3);
            sliders.Controls.Add(row, 0, i);
        }
        layout.Controls.Add(sliders, 0, 2);
        var quick = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(1, 7, 0, 0) };
        quick.Controls.Add(MiniButton("全开", (_, _) => SetAllFet(255)));
        quick.Controls.Add(MiniButton("全关", (_, _) => SetAllFet(0)));
        var allOff = MiniButton("熄灭两侧", (_, _) => BlackoutBoth());
        allOff.BackColor = Color.FromArgb(255, 244, 242);
        allOff.ForeColor = Color.FromArgb(172, 75, 67);
        quick.Controls.Add(allOff);
        layout.Controls.Add(quick, 0, 3);
        var billboard = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(244, 248, 251), Margin = new Padding(0, 2, 0, 1), Padding = new Padding(10, 0, 8, 0) };
        billboard.ColumnStyles.Add(new(SizeType.Percent, 100)); billboard.ColumnStyles.Add(new(SizeType.AutoSize));
        billboard.Controls.Add(new Label { Text = "广告牌灯光 · RGB", AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Color.FromArgb(53, 70, 86) }, 0, 0);
        billboard.Controls.Add(new Label { Text = "JVS PWM · 待接入", AutoSize = true, Anchor = AnchorStyles.Right, ForeColor = Muted }, 1, 0);
        layout.Controls.Add(billboard, 0, 4);
        var foot = new Label { Text = "游戏运行时请勿连接同一串口。", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted, Padding = new Padding(2, 0, 0, 0) }; layout.Controls.Add(foot, 0, 5);
        return card;
    }

    private Control BuildCommonOutputs()
    {
        var card = Card(new Padding(20));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new(SizeType.Absolute, 54));
        layout.RowStyles.Add(new(SizeType.Absolute, 66));
        layout.RowStyles.Add(new(SizeType.Absolute, 66));
        layout.RowStyles.Add(new(SizeType.Absolute, 66));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.Controls.Add(SectionTitle("公共输出", "与 1P / 2P 灯板分离"), 0, 0);
        AddCapabilityRow(layout, 1, "摄像机 RING LED", "JVS 开关输出 3 · 设备协议待核实");
        AddCapabilityRow(layout, 2, "摄像机 REC LED", "JVS 开关输出 4 · 设备协议待核实");
        AddCapabilityRow(layout, 3, "禁止投币", "说明书标注：暂时未启用，不发送控制命令");
        layout.Controls.Add(new Label
        {
            Text = "当前可直接测试：每侧 8 个按键 RGB，以及框体机身 / 画面外圈 / 侧盖三路亮度。\n广告牌和摄像机灯走 JVS 输出链路；设备协议未确认前，仅展示状态，不会写入未知命令。",
            Dock = DockStyle.Top, AutoSize = true, ForeColor = Muted, BackColor = Color.FromArgb(244, 248, 251), Padding = new Padding(14), Margin = new Padding(0, 12, 0, 0)
        }, 0, 4);
        card.Controls.Add(layout);
        return card;
    }

    private static void AddCapabilityRow(TableLayoutPanel layout, int row, string name, string state)
    {
        var item = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(247, 250, 252), Margin = new Padding(0, 3, 0, 3), Padding = new Padding(14, 0, 14, 0) };
        item.ColumnStyles.Add(new(SizeType.Absolute, 190)); item.ColumnStyles.Add(new(SizeType.Percent, 100));
        item.Controls.Add(new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(53, 70, 86) }, 0, 0);
        item.Controls.Add(new Label { Text = state, AutoSize = true, Anchor = AnchorStyles.Left, ForeColor = Muted }, 1, 0);
        layout.Controls.Add(item, 0, row);
    }

    private Control BuildLog()
    {
        var card = Card(new Padding(12, 7, 12, 9));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new(SizeType.Percent, 100)); layout.ColumnStyles.Add(new(SizeType.Absolute, 160));
        layout.RowStyles.Add(new(SizeType.Absolute, 32)); layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "连接与发送状态", AutoSize = true, Anchor = AnchorStyles.Left, Font = new(Font.FontFamily, 9, FontStyle.Bold), ForeColor = Color.FromArgb(55, 69, 85) }, 0, 0);
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var clear = MiniButton("清空", (_, _) => log.Clear()); options.Controls.Add(clear);
        verbose.Text = "详细数据"; verbose.AutoSize = true; verbose.CheckedChanged += (_, _) => { foreach (var d in devices) d.Detailed = verbose.Checked; }; options.Controls.Add(verbose);
        layout.Controls.Add(options, 1, 0);
        log.Dock = DockStyle.Fill; log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical; log.BorderStyle = BorderStyle.None; log.BackColor = Color.FromArgb(249, 251, 253); log.Font = new Font("Consolas", 9); log.WordWrap = false;
        layout.Controls.Add(log, 0, 1); layout.SetColumnSpan(log, 2);
        card.Controls.Add(layout);
        return card;
    }

    private const string Accent = "#008E87", Border = "#DCE5EC", MutedColor = "#687B8E";
    private static Color AccentColor => ColorTranslator.FromHtml(Accent);
    private static Color Muted => ColorTranslator.FromHtml(MutedColor);
    private static Color BorderColor => ColorTranslator.FromHtml(Border);
    private static Control Card(Padding? padding = null) => new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = padding ?? new Padding(0), Margin = new Padding(4), BorderStyle = BorderStyle.FixedSingle };
    private static Label SectionTitle(string title, string subtitle) => new() { Text = title + "   " + subtitle, AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font("Microsoft YaHei UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(34, 49, 66), Margin = new Padding(0, 0, 0, 2) };
    private Button MiniButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(244, 248, 250), ForeColor = Color.FromArgb(61, 79, 97), Margin = new Padding(0, 0, 7, 0), Padding = new Padding(9, 1, 9, 1) };
        button.FlatAppearance.BorderColor = BorderColor; button.FlatAppearance.BorderSize = 1; button.Click += click;
        return button;
    }

    private void AddPreset(TableLayoutPanel row, int column, string label, Color color)
    {
        var button = new Button { Text = label, Dock = DockStyle.Fill, Margin = new(3, 2, 0, 2), BackColor = color, ForeColor = color.GetBrightness() < .5 ? Color.White : Color.Black, FlatStyle = FlatStyle.Flat };
        button.FlatAppearance.BorderSize = 0; button.Click += (_, _) => ApplyColor(color); row.Controls.Add(button, column, 0);
    }
    private void PickColor()
    {
        using var dialog = new ColorDialog { Color = selectedColor, FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) ApplyColor(dialog.Color);
    }
    private void ApplyColor(Color color)
    {
        selectedColor = Color.FromArgb(color.R, color.G, color.B);
        var selected = Enumerable.Range(0, 8).Where(i => lamps[i].Checked).ToArray();
        if (selected.Length == 0) { AddLog("请先选中要调整的按键灯。"); return; }
        foreach (int side in Targets()) models[side].SetColors(selected, selectedColor);
        chase.Checked = false;
        UpdateColorPreview(); RefreshLampTiles();
    }
    private void UpdateColorPreview() { colorPreview.BackColor = selectedColor; colorPreview.ForeColor = selectedColor.GetBrightness() < .48 ? Color.White : Color.Black; }
    private void RefreshLampTiles()
    {
        if (lamps[0] is null) return;
        var selectedSide = SelectedPreviewSide();
        var rgb = models[selectedSide].Snapshot().Rgb;
        for (int i = 0; i < 8; i++)
        {
            Color color = Color.FromArgb(rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]);
            lamps[i].Text = $"{i + 1}  {(lamps[i].Checked ? "✓ 已选" : "未选")}";
            bool lit = color.GetBrightness() > .03;
            lamps[i].BackColor = lit ? Color.FromArgb((color.R + 255 * 3) / 4, (color.G + 255 * 3) / 4, (color.B + 255 * 3) / 4) : Color.FromArgb(247, 250, 252);
            lamps[i].FlatAppearance.BorderColor = lit ? color : (lamps[i].Checked ? AccentColor : BorderColor);
            lamps[i].FlatAppearance.BorderSize = lamps[i].Checked ? 3 : 1;
            lamps[i].ForeColor = Color.FromArgb(53, 70, 86);
        }
    }
    private void SetLampSelection(bool selected) { foreach (var lamp in lamps) lamp.Checked = selected; }
    internal void SelectPreviewTab(int index) { if (outputTabs is not null && index >= 0 && index < outputTabs.TabCount) outputTabs.SelectedIndex = index; }
    private void ApplyBrightness()
    {
        if (changingUi) return;
        int value = brightness.Value;
        foreach (int side in Targets()) models[side].SetBrightness(value);
        brightnessValue.Text = value.ToString(); RefreshLampTiles();
    }
    private void SetFet(int channel, byte value) { foreach (int side in Targets()) models[side].SetFet(channel, value); }
    private void SetAllFet(byte value)
    {
        for (int channel = 0; channel < 3; channel++)
        {
            fetBars[channel].Value = value; fetValues[channel].Text = value.ToString(); SetFet(channel, value);
        }
    }
    private int[] Targets() => target.SelectedIndex switch { 1 => [1], 2 => [0, 1], _ => [0] };
    private int SelectedPreviewSide() => target.SelectedIndex == 1 ? 1 : 0;
    private void SyncTargetControls()
    {
        int side = SelectedPreviewSide();
        changingUi = true;
        try
        {
            brightness.Value = models[side].Brightness; brightnessValue.Text = brightness.Value.ToString();
            var fet = models[side].Fet;
            for (int i = 0; i < 3; i++) { fetBars[i].Value = fet[i]; fetValues[i].Text = fet[i].ToString(); }
        }
        finally { changingUi = false; }
        RefreshLampTiles();
    }
    private void BlackoutTarget()
    {
        chase.Checked = false;
        foreach (int side in Targets()) models[side].Blackout();
        foreach (var bar in fetBars) bar.Value = 0;
        foreach (var label in fetValues) label.Text = "0";
        RefreshLampTiles();
        AddLog("已将当前目标设为全灭。");
    }
    private void BlackoutBoth()
    {
        chase.Checked = false;
        foreach (var model in models) model.Blackout();
        for (int channel = 0; channel < 3; channel++)
        {
            fetBars[channel].Value = 0;
            fetValues[channel].Text = "0";
        }
        RefreshLampTiles();
        AddLog("已将 1P、2P 灯光状态都设为全灭；已连接的串口将发送更新。");
    }
    private void AdvanceChase()
    {
        chaseIndex = (chaseIndex + 1) % 8;
        foreach (int side in Targets()) { models[side].SetBrightness(brightness.Value); models[side].SetChase(chaseIndex, selectedColor); }
        RefreshLampTiles();
    }
    private void RefreshPorts()
    {
        string[] found;
        try { found = PortDiscovery.List(); }
        catch (Exception ex) { AddLog("读取串口列表失败：" + ex.Message); found = []; }
        foreach (var box in ports)
        {
            string current = box.Text;
            box.BeginUpdate(); box.Items.Clear(); box.Items.AddRange(found); box.Text = current; box.EndUpdate();
        }
        AddLog(found.Length == 0 ? "尚未发现串口。也可手动输入 COM 端口。" : "串口：" + string.Join(", ", found));
    }
    private void DetectPort(int side)
    {
        try
        {
            string? found = PortDiscovery.Detect(side);
            if (found is null) { AddLog($"未按 VID/PID 找到{(side == 0 ? "1P" : "2P")} 控制板。请确认 USB 已连接。 "); return; }
            ports[side].Text = found;
            if (!ports[side].Items.Contains(found)) ports[side].Items.Add(found);
            AddLog($"识别到{(side == 0 ? "1P" : "2P")} 控制板：{found}。点击连接后才会打开串口。");
        }
        catch (Exception ex) { AddLog("自动识别失败：" + ex.Message); }
    }
    private async Task ToggleConnection(int side)
    {
        if (closing) return;
        connectButtons[side].Enabled = false;
        try
        {
            if (devices[side].Connected) await devices[side].DisconnectAsync();
            else
            {
                string port = ports[side].Text.Trim().ToUpperInvariant();
                await devices[side].ConnectAsync(port);
                if (closing) await devices[side].DisconnectAsync();
            }
        }
        catch (Exception ex) { AddLog($"{(side == 0 ? "1P" : "2P")} 连接失败：{ex.Message}"); }
        finally { connectButtons[side].Enabled = !closing; UpdateStatus(); }
    }
    private void UpdateStatus()
    {
        if (IsDisposed) return;
        for (int i = 0; i < 2; i++)
        {
            connectionStatus[i].Text = devices[i].Connected
                ? $"已连接 {devices[i].Port}  ·  {devices[i].Sent:N0} 帧已发送 · 无设备确认"
                : "未连接";
            connectionStatus[i].ForeColor = devices[i].Connected ? AccentColor : Muted;
            if (connectButtons[i].Enabled || closing) connectButtons[i].Text = devices[i].Connected ? "断开" : "连接";
        }
        foreach (var device in devices) device.Gap = (int)interval.Value;
    }
    private void AddLog(string line)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { try { BeginInvoke(new Action<string>(AddLog), line); } catch (InvalidOperationException) { } return; }
        log.AppendText($"{DateTime.Now:HH:mm:ss}  {line}{Environment.NewLine}");
        if (log.Lines.Length > 160)
        {
            log.Lines = log.Lines[^120..]; log.SelectionStart = log.TextLength; log.ScrollToCaret();
        }
    }
    private async void ClosingAsync(object? sender, FormClosingEventArgs e)
    {
        if (closeReady) return;
        e.Cancel = true;
        if (closing) return;
        closing = true; Enabled = false; uiTimer.Stop(); chaseTimer.Stop();
        try
        {
            await Task.WhenAll(connectOperations);
            await Task.WhenAll(devices.Select(d => d.DisconnectAsync()));
        }
        finally { closeReady = true; Close(); }
    }
}
