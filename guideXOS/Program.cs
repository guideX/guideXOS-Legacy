using guideXOS;
using guideXOS.DefaultApps;
using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
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
        try { Wallpaper = new PNG(File.ReadAllBytes("Images/tronporche.png")); } catch { Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height); }
        BitFont.Initialize();
        string CustomCharset = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        BitFont.RegisterBitFont(new BitFontDescriptor("Song", CustomCharset, File.ReadAllBytes("Fonts/Song.btf"), 16));
        FConsole = null;
        WindowManager.Initialize();
        Desktop.Initialize();
        Audio.Initialize();
        AC97.Initialize();
        if (AC97.DeviceLocated) Console.WriteLine("Device Located: " + AC97.DeviceName);
        ES1371.Initialize();
#if NETWORK
        NETv4.Initialize();
        Intel825xx.Initialize();
        RTL8111.Initialize();
#if true
        //Console.Write("Trying to get ip config from DHCP server...\n");
        bool ares = NETv4.DHCPDiscover();
        if (!ares)
        {
            Console.Write("DHCP discovery failed\n");
            for (; ; ) Native.Hlt();
        }
#endif
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

    public static void SMain() {
        Framebuffer.TripleBuffered = true;

        Image wall = Wallpaper;
        try {
            if (wall != null) {
                Wallpaper = wall.ResizeImage(Framebuffer.Width, Framebuffer.Height);
                wall.Dispose();
            } else {
                Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height);
            }
        } catch { Wallpaper = new Image(Framebuffer.Width, Framebuffer.Height); }

        //Lockscreen.Run();
        FConsole = null;

        // Ensure context menu exists
        if (rightmenu == null) {
            rightmenu = new RightMenu();
            rightmenu.Visible = false;
        }

        // Show login screen immediately after unlocking
        // var login = new guideXOS.GUI.LoginDialog();
        // WindowManager.MoveToEnd(login);
        // login.Visible = true;

        var welcome = new Welcome(500, 250);

        //Console.WriteLine("Draw Start");
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
            if (Control.MouseButtons.HasFlag(MouseButtons.Right) && !rightClicked) {
                rightClicked = true;
                rightmenu.X = Control.MousePosition.X;
                rightmenu.Y = Control.MousePosition.Y;
                WindowManager.MoveToEnd(rightmenu);
                rightmenu.Visible = true;
            } else if (!Control.MouseButtons.HasFlag(MouseButtons.Right)) rightClicked = false;
            Desktop.Update(Icons.DocumentIcon);
            //Desktop.Draw();
            WindowManager.DrawAll();
            //draw cursor
            var img = Control.MouseButtons.HasFlag(MouseButtons.Left) ? CursorMoving : Cursor;
            if (img != null) Framebuffer.Graphics.DrawImage(Control.MousePosition.X, Control.MousePosition.Y, img);
            //refresh screen
            Framebuffer.Update();
            // yield a bit to avoid tight spin
            Thread.Sleep(1);
        }
    }
}