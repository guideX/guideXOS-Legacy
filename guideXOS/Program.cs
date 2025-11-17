using guideXOS;
using guideXOS.DefaultApps;
using guideXOS.DockableWidgets;
using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using guideXOS.OS;
using System.Drawing;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Windows.Forms;
/// <summary>
/// Program
/// </summary>
unsafe class Program {
    /// <summary>
    /// Main
    /// </summary>
    static void Main() {
    }
    /// <summary>
    /// DLL Import
    /// </summary>
    [DllImport("*")]
    public static extern void test();
    /// <summary>
    /// Cusor
    /// </summary>
    private static Image Cursor;
    private static Image CursorMoving;
    private static Image CursorBusy;
    /// <summary>
    /// Wallpaper
    /// </summary>
    public static Image Wallpaper;
    /// <summary>
    /// USB Mouse Test
    /// </summary>
    /// <returns></returns>
    private static bool USBMouseTest() {
        HID.GetMouse(HID.Mouse, out _ /*sbyte AxisX*/, out _ /*sbyte AxisY*/, out var Buttons);
        return Buttons != MouseButtons.None;
    }
    /// <summary>
    /// USB Keyboard Test
    /// </summary>
    /// <returns></returns>
    private static bool USBKeyboardTest() {
        HID.GetKeyboard(HID.Keyboard, out var ScanCode, out _/*var Key*/);
        return ScanCode != 0;
    }
    /// <summary>
    /// KMain
    /// </summary>
    [RuntimeExport("KMain")]
    static void KMain() {
        Animator.Initialize();

        // Initialize legacy PS/2 input first so VirtualBox (default PS/2 devices) works out-of-the-box.
        // This provides keyboard IRQ1 (0x21) and mouse IRQ12 (0x2C) handling even without USB HID.
        try { PS2Keyboard.Initialize(); } catch { }
        try { PS2Mouse.Initialise(); } catch { }
        // Initialize VMware absolute pointer backdoor if present (no-op on other hypervisors)
        try { VMwareTools.Initialize(); } catch { }

#if USBDebug
        Hub.Initialize();
        HID.Initialize();
        EHCI.Initialize();
        //USB.StartPolling();

        //Use qemu for USB debug
        //VMware won't connect virtual USB HIDs
        if (HID.Mouse == null)
        {
            Console.WriteLine("USB Mouse not present");
        }
        if (HID.Keyboard == null)
        {
            Console.WriteLine("USB Keyboard not present");
        }

        for(; ; )
        {
            if (HID.Mouse != null)
            {
                HID.GetMouseThings(HID.Mouse, out sbyte AxisX, out sbyte AxisY, out var Buttons);
                if (AxisX != 0 && AxisY != 0)
                {
                    Console.WriteLine($"X:{AxisX} Y:{AxisY}");
                }
            }
            if(HID.Keyboard != null) 
            {
                HID.GetKeyboard(HID.Keyboard, out var ScanCode, out var Key);
                if(ScanCode != 0)
                {
                    Console.WriteLine($"ScanCode:{ScanCode}");
                }
            }
        }
#else
        try {
            Hub.Initialize();
            HID.Initialize();
            EHCI.Initialize();
            USB.StartPolling();
        } catch { /* USB stack is optional; continue boot */ }

        try {
            if (HID.Mouse == null) {
                Console.WriteLine("USB Mouse not present");
            }
            if (HID.Keyboard == null) {
                Console.WriteLine("USB Keyboard not present");
            }
        } catch { }
#endif

        //Sized width to 512
        try { Cursor = new PNG(File.ReadAllBytes("Images/Cursor.png")); } catch { Cursor = new Image(16,16); }
        try { CursorMoving = new PNG(File.ReadAllBytes("Images/Grab.png")); } catch { CursorMoving = Cursor; }
        try { CursorBusy = new PNG(File.ReadAllBytes("Images/Busy.png")); } catch { CursorBusy = Cursor; }
        //try { Wallpaper = new PNG(File.ReadAllBytes("Images/tronporche.png")); } catch { Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height); }
        BitFont.Initialize();
        string CustomCharset = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        //BitFont.RegisterBitFont(new BitFontDescriptor("Song", CustomCharset, File.ReadAllBytes("Fonts/Song.btf"), 16));
        BitFont.RegisterBitFont(new BitFontDescriptor("Enludo", CustomCharset, File.ReadAllBytes("Fonts/enludo.btf"), 16));
        FConsole = null;
        WindowManager.Initialize();
        Desktop.Initialize();
        Firewall.Initialize();
        Audio.Initialize();
        AC97.Initialize();
        if (AC97.DeviceLocated) Console.WriteLine("Device Located: " + AC97.DeviceName);
        ES1371.Initialize();
#if NETWORK
        Console.WriteLine("[NET] Initializing network subsystem...");
        try {
            NETv4.Initialize();
            Intel825xx.Initialize();
            RTL8111.Initialize();
        } catch {
            Console.WriteLine("[NET] Network driver initialization error");
        }
        
        // Only try DHCP if a network driver was found
        if (NETv4.Sender != null) {
            Console.WriteLine("[NET] Network driver found");
            Console.WriteLine("[NET] Skipping automatic DHCP (use 'netinit' command in console)");
            // Skip DHCP during boot to prevent hanging
            // User can run 'netinit' command in FConsole to configure network manually
        } else {
            Console.WriteLine("[NET] No network hardware detected");
        }
#endif

        // Apply saved display mode before wallpaper resize
        DisplayManager.ApplySavedResolution();

        SMain();
    }

#if NETWORK
    private static void Client_OnData(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            Console.Write((char)data[i]);
        }
        Console.WriteLine();
    }

    public static byte[] ToASCII(string s) 
    {
        byte[] buffer = new byte[s.Length];
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)s[i];
        return buffer;
    }
#endif
    public static bool rightClicked;
    public static FConsole FConsole;
    public static RightMenu rightmenu;
    public static PerformanceWidget perfWidget;

    public static void SMain() {
        Framebuffer.TripleBuffered = true;

        Image wall = Wallpaper;
        try {
            if (wall != null) {
                Wallpaper = wall.ResizeImage(Framebuffer.Width, Framebuffer.Height);
                wall.Dispose();
            } else {
                // Create default wallpaper with teal gradient (top to bottom)
                Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height);
                
                // Teal gradient colors - lighter at top, darker at bottom
                uint topColor = 0xFF5FD4C4;      // Light teal/cyan
                uint bottomColor = 0xFF0D7D77;   // Darker teal
                
                int topR = (int)((topColor >> 16) & 0xFF);
                int topG = (int)((topColor >> 8) & 0xFF);
                int topB = (int)(topColor & 0xFF);
                
                int bottomR = (int)((bottomColor >> 16) & 0xFF);
                int bottomG = (int)((bottomColor >> 8) & 0xFF);
                int bottomB = (int)(bottomColor & 0xFF);
                
                // Create vertical gradient
                for (int y = 0; y < Wallpaper.Height; y++) {
                    float t = (float)y / Wallpaper.Height;
                    int r = (int)(topR + (bottomR - topR) * t);
                    int g = (int)(topG + (bottomG - topG) * t);
                    int b = (int)(topB + (bottomB - topB) * t);
                    uint color = (uint)(0xFF000000 | (r << 16) | (g << 8) | b);
                    
                    for (int x = 0; x < Wallpaper.Width; x++) {
                        Wallpaper.RawData[y * Wallpaper.Width + x] = (int)color;
                    }
                }
            }
        } catch { 
            // Fallback: create wallpaper with teal gradient
            Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height);
            
            uint topColor = 0xFF5FD4C4;      // Light teal/cyan
            uint bottomColor = 0xFF0D7D77;   // Darker teal
            
            int topR = (int)((topColor >> 16) & 0xFF);
            int topG = (int)((topColor >> 8) & 0xFF);
            int topB = (int)(topColor & 0xFF);
            
            int bottomR = (int)((bottomColor >> 16) & 0xFF);
            int bottomG = (int)((bottomColor >> 8) & 0xFF);
            int bottomB = (int)(bottomColor & 0xFF);
            
            for (int y = 0; y < Wallpaper.Height; y++) {
                float t = (float)y / Wallpaper.Height;
                int r = (int)(topR + (bottomR - topR) * t);
                int g = (int)(topG + (bottomG - topG) * t);
                int b = (int)(topB + (bottomB - topB) * t);
                uint color = (uint)(0xFF000000 | (r << 16) | (g << 8) | b);
                
                for (int x = 0; x < Wallpaper.Width; x++) {
                    Wallpaper.RawData[y * Wallpaper.Width + x] = (int)color;
                }
            }
        }

        //Lockscreen.Run();
        FConsole = null;

        // Ensure context menu exists
        if (rightmenu == null) {
            rightmenu = new RightMenu();
            rightmenu.Visible = false;
        }

        // Create performance widget (initially visible)
        perfWidget = new PerformanceWidget();
        perfWidget.Visible = false; // Don't show standalone - will be in container
        WindowManager.MoveToEnd(perfWidget);

        // Create clock widget positioned below performance widget
        var clockWidget = new guideXOS.DockableWidgets.Clock(
            perfWidget.X,  // Same X position as performance widget
            perfWidget.Y + perfWidget.Height + 10  // Below performance widget with 10px gap
        );
        clockWidget.Visible = false; // Don't show standalone - will be in container
        WindowManager.MoveToEnd(clockWidget);

        // Create monitor widget for system charts
        var monitorWidget = new guideXOS.DockableWidgets.Monitor();
        monitorWidget.Visible = false; // Don't show standalone - will be in container
        WindowManager.MoveToEnd(monitorWidget);

        // Create a container and dock all widgets together
        var widgetContainer = new WidgetContainer(
            Framebuffer.Width - 220,  // Position more to the left (was -160)
            80  // Y position from top
        );
        widgetContainer.AddWidget(perfWidget);
        widgetContainer.AddWidget(clockWidget);
        widgetContainer.AddWidget(monitorWidget);
        widgetContainer.Visible = true;
        WindowManager.MoveToEnd(widgetContainer);

        // Automatically show console on startup
        FConsole = new FConsole(160, 120);
        FConsole.Visible = true;
        WindowManager.MoveToEnd(FConsole);

        // Show login screen immediately after unlocking
        // var login = new guideXOS.GUI.LoginDialog();
        // WindowManager.MoveToEnd(login);
        // login.Visible = true;

        //var welcome = new Welcome(500, 250);

        //It freezes here too
        //WindowManager.EnablePerfTracking();

        //Console.WriteLine("Draw Start");
        int lastMouseX = Control.MousePosition.X;
        int lastMouseY = Control.MousePosition.Y;
        ulong lastMoveTick = Timer.Ticks;
        const ulong ActiveMoveMs = 100; // stay responsive for 100ms after a move

        for (; ; ) {
            // Per-frame input pass for all windows
            WindowManager.MouseHandled = false;
            WindowManager.InputAll();
            WindowManager.FlushPendingCreates();

            // Service audio playback from main loop
            WAVPlayer.DoPlay();

            //clear screen
            Framebuffer.Graphics.Clear(0x0);
            //draw carpet or wallpaper
            if (Wallpaper != null)
                Framebuffer.Graphics.DrawImage((Framebuffer.Width / 2) - (Wallpaper.Width / 2), (Framebuffer.Height / 2) - (Wallpaper.Height / 2), Wallpaper);
            //Inspects the system to see if the user has right clicked there is a small difference between these two functions
            // Show desktop context menu only when right-click happened and no other window consumed the mouse.
            if (Control.MouseButtons.HasFlag(MouseButtons.Right) && !rightClicked && !WindowManager.MouseHandled) {
                rightClicked = true;
                rightmenu.X = Control.MousePosition.X;
                rightmenu.Y = Control.MousePosition.Y;
                WindowManager.MoveToEnd(rightmenu);
                rightmenu.Visible = true;
            } else if (!Control.MouseButtons.HasFlag(MouseButtons.Right)) {
                rightClicked = false;
            }
            int iconSize = 48;
            Desktop.Update(
                Icons.DocumentIcon(iconSize), 
                Icons.FolderIcon(iconSize), 
                Icons.ImageIcon(iconSize), 
                Icons.AudioIcon(iconSize),
                iconSize
            );
            //Desktop.Draw();
            
            // Draw windows in layers to control z-order:
            // 1. Regular windows (except Task Manager)
            WindowManager.DrawAllExceptTaskManager();
            
            // 2. Workspace switcher (if visible) - appears on top of regular windows
            if (Desktop.Taskbar != null) {
                Desktop.Taskbar.DrawWorkspaceSwitcher();
            }
            
            // 3. Task Manager (always on top)
            WindowManager.DrawTaskManager();
            
            //draw cursor
            var img = Control.MouseButtons.HasFlag(MouseButtons.Left) ? CursorMoving : Cursor;
            if (img != null) Framebuffer.Graphics.DrawImage(Control.MousePosition.X, Control.MousePosition.Y, img);
            //refresh screen
            Framebuffer.Update();
            // Mouse responsiveness throttling: if mouse moved recently, keep minimal sleep (0) for max responsiveness.
            // When idle, yield a bit to lower CPU usage.
            int mx = Control.MousePosition.X; int my = Control.MousePosition.Y;
            if (mx != lastMouseX || my != lastMouseY) {
                lastMouseX = mx; lastMouseY = my; lastMoveTick = Timer.Ticks;
                Thread.Sleep(0);
            } else {
                ulong age = (Timer.Ticks >= lastMoveTick) ? (Timer.Ticks - lastMoveTick) : 0UL;
                if (age < ActiveMoveMs) Thread.Sleep(0); else Thread.Sleep(2);
            }
         }
     }
 }