using guideXOS;
using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
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
    /// <summary>
    /// Cursor Moving
    /// </summary>
    private static Image CursorMoving;
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
        Hub.Initialize();
        HID.Initialize();
        EHCI.Initialize();

        /*
        if (HID.Keyboard != null) {
            Console.Write("[Warning] Press please press any key to validate USB keyboard ");
            bool res = Console.Wait(&USBKeyboardTest, 2000);
            Console.WriteLine();
            if (!res) {
                lock (null) {
                    USB.NumDevice--;
                    HID.Keyboard = null;
                }
            }
        }
        */
        //if (HID.Mouse != null) {
        //Console.Write("[Warning] Press please press Mouse any key to validate USB Mouse ");
        //bool res = Console.Wait(&USBMouseTest, 2000);
        //Console.WriteLine();
        //if (!res) {
        //lock (null) {
        //USB.NumDevice--;
        //HID.Mouse = null;
        //}
        //}
        //}

        USB.StartPolling();

        //Use qemu for USB debug
        //VMware won't connect virtual USB HIDs
        if (HID.Mouse == null) {
            Console.WriteLine("USB Mouse not present");
        }
        if (HID.Keyboard == null) {
            Console.WriteLine("USB Keyboard not present");
        }
#endif

        //Sized width to 512
        Cursor = new PNG(File.ReadAllBytes("Images/Cursor.png"));
        CursorMoving = new PNG(File.ReadAllBytes("Images/Grab.png"));
        Wallpaper = new PNG(File.ReadAllBytes("Images/Wallpaper1.png"));
        BitFont.Initialize();
        string CustomCharset = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        BitFont.RegisterBitFont(new BitFontDescriptor("Song", CustomCharset, File.ReadAllBytes("Fonts/Song.btf"), 16));
        FConsole = null;
        WindowManager.Initialize();
        Desktop.Initialize();
        //Serial.WriteLine("Hello World");
        //Console.WriteLine("Hello, World!");
        //Console.WriteLine("Use Native AOT (Core RT) Technology.");
        //test();
        Audio.Initialize();
        AC97.Initialize();
        if (AC97.DeviceLocated) Console.WriteLine("Device Located: " + AC97.DeviceName);
        ES1371.Initialize();
        /*
        for (; ; )
        {
            Console.WriteLine(Console.ReadLine());
        }
        */

        //Console.WriteLine("Checking Network ...");
#if NETWORK
        //Console.WriteLine("Network Available");
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
#else
        NETv4.Configure(new NETv4.IPAddress(192, 168, 1, 65), new NETv4.IPAddress(192, 168, 1, 1), new NETv4.IPAddress(255, 255, 255, 0));
#endif
        //Console.Write("Network initialized.\n");

        //Only single client is supported now!
        TCPListener tc = new TCPListener(54188);
        tc.Listen();
        while (tc.Status != TCPStatus.Established) Native.Hlt();
        string s = "hello world";
        fixed (char* c = s)
            tc.Send((byte*)c, s.Length * 2);
        tc.Close();
        while (tc.Status != TCPStatus.Closed) Native.Hlt();
        tc.Remove();

        for (; ; ) ;
#endif

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

        /*
        //This driver doesn't support drawing without update
        if(PCI.GetDevice(0x15AD, 0x0405) != null)
            Framebuffer.Graphics = new VMWareSVGAIIGraphics();
        */

        Image wall = Wallpaper;
        Wallpaper = wall.ResizeImage(Framebuffer.Width, Framebuffer.Height);
        wall.Dispose();

        Lockscreen.Run();
        FConsole = null;

        //var welcome = new Welcome(500, 250);
        _ = new Welcome(500, 250);

        rightmenu = new RightMenu();
        rightClicked = false;

        #region Animation of entering Desktop
        Framebuffer.Graphics.DrawImage((Framebuffer.Width / 2) - (Wallpaper.Width / 2), (Framebuffer.Height / 2) - (Wallpaper.Height / 2), Wallpaper, false);
        Desktop.Update(Icons.FileIcon);
        WindowManager.DrawAll();
        Framebuffer.Graphics.DrawImage(Control.MousePosition.X, Control.MousePosition.Y, Cursor);
        Image _screen = Framebuffer.Graphics.Save();
        Framebuffer.Graphics.Clear(0x0);

        int startAt = 20;

        var SizedScreens = new Image[60];
        for (int i = 0; i < SizedScreens.Length; i++) {
            if (i < startAt) continue;

            SizedScreens[i] = _screen.ResizeImage(
                //(int)(_screen.Width * (i / ((float)SizedScreens.Length))),
                (int)(_screen.Width * (Math.Sin(Math.PI * 90 / 180 * (i / ((float)SizedScreens.Length))))),
                //(int)(_screen.Height * (i / ((float)SizedScreens.Length)))
                (int)(_screen.Height * (Math.Sin(Math.PI * 90 / 180 * (i / ((float)SizedScreens.Length)))))
                );
        }

        Animation EA = new()
        {
            MinimumValue = 0,
            MaximumValue = SizedScreens.Length - 1,
            PeriodInMS = 17,
        };
        Animator.AddAnimation(EA);

        int last = -1;
        while (EA.Value < EA.MaximumValue) {
            if (EA.Value < startAt || last == EA.Value) continue;

            var img = SizedScreens[EA.Value];
            Framebuffer.Graphics.Clear(0x0);
            Framebuffer.Graphics.ADrawImage(
                (Framebuffer.Graphics.Width / 2) - (img.Width / 2),
                (Framebuffer.Graphics.Height / 2) - (img.Height / 2),
                img,
                (byte)(((EA.Value - startAt) / (float)(EA.MaximumValue - startAt)) * 255f));
            Framebuffer.Update();
            last = EA.Value;
        }
        EA.Dispose();
        Animator.DisposeAnimation(EA);

        for (int i = 0; i < SizedScreens.Length; i++) {
            if (i < startAt) continue;
            SizedScreens[i].Dispose();
        }
        SizedScreens.Dispose();
        #endregion

        NotificationManager.Initialize();

        var fileIcon = Icons.FileIcon;
        for (; ; )
        {
            #region ConsoleHotKey
            if (
                Keyboard.KeyInfo.Key == ConsoleKey.T &&
                Keyboard.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) &&
                Keyboard.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt)
                ) {
                WindowManager.MoveToEnd(FConsole);
                if (FConsole.Visible == false)
                    FConsole.Visible = true;
            }
            #endregion
            #region Right Menu
            if (Control.MouseButtons.HasFlag(MouseButtons.Right)) {
                rightClicked = true;
            } else {
                if (rightClicked == true) {
                    rightmenu.Visible = !rightmenu.Visible;
                    WindowManager.MoveToEnd(rightmenu);
                }

                rightClicked = false;
            }
            #endregion
            WindowManager.InputAll();

            Framebuffer.Graphics.DrawImage((Framebuffer.Width / 2) - (Wallpaper.Width / 2), (Framebuffer.Height / 2) - (Wallpaper.Height / 2), Wallpaper, false);
            Desktop.Update(fileIcon);
            WindowManager.DrawAll();
            NotificationManager.Update();
            /*
            ASC16.DrawString("FPS: ", 10, 10, 0xFFFFFFFF);
            ASC16.DrawString(((ulong)FPSMeter.FPS).ToString(), 42, 10, 0xFFFFFFFF);
            */
            Framebuffer.Graphics.DrawImage(Control.MousePosition.X, Control.MousePosition.Y, WindowManager.HasWindowMoving ? CursorMoving : Cursor);
            Framebuffer.Update();

            FPSMeter.Update();
        }
    }

    //private static void Console_OnWrite(char chr) {
    //Console.Write(chr.ToString());
    //}

    /// <summary>
    /// Init Console
    /// </summary>
    public static void InitConsole() {
        FConsole = new FConsole(350, 300);
    }
}