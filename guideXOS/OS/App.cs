using guideXOS.DefaultApps;
using guideXOS.FS;
using guideXOS.GUI;
using guideXOS.Misc;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace guideXOS.OS {
    /// <summary>
    /// App
    /// </summary>
    public class App {
        #region "private variables"
        /// <summary>
        /// Name
        /// </summary>
        private string _name { get; set; }
        /// <summary>
        /// Icon
        /// </summary>
        private Image _icon { get; set; }
        /// <summary>
        /// App Object
        /// </summary>
        private Object _appObject { get; set; }
        #endregion
        #region "public variables"
        /// <summary>
        /// App
        /// </summary>
        /// <param name="name"></param>
        public App(string name, Image icon) {
            _name = name;
            _icon = icon;
        }
        /// <summary>
        /// Name
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }
        /// <summary>
        /// Icon
        /// </summary>
        public Image Icon {
            get {
                return _icon;
            }
        }
        /// <summary>
        /// App Object
        /// </summary>
        public Object AppObject {
            get {
                return _appObject;
            }
            set {
                _appObject = value;
            }
        }
        #endregion
    }
    /// <summary>
    /// App Collection
    /// </summary>
    public class AppCollection {
        #region "private variables"
        /// <summary>
        /// Apps
        /// </summary>
        private List<App> _apps;
        #endregion
        #region "public variables"
        /// <summary>
        /// App Collection
        /// </summary>
        public AppCollection() {
            _apps = new List<App>();
            LoadDefaultApps();
        }
        /// <summary>
        /// Load Default Apps
        /// </summary>
        private void LoadDefaultApps() {
            var iconWidth = 32;
            var path = "Images/BlueVelvet/" + iconWidth.ToString() + "/";
            var icon = new PNG(File.ReadAllBytes(path + "documents.png"));
            _apps.Add(new App("Calculator", new PNG(File.ReadAllBytes(path + "calculator.png"))));
            _apps.Add(new App("Clock", new PNG(File.ReadAllBytes(path + "calendar.png"))));
            _apps.Add(new App("Paint", new PNG(File.ReadAllBytes(path + "image.png"))));
            _apps.Add(new App("Console", new PNG(File.ReadAllBytes(path + "edit.png"))));
            _apps.Add(new App("Monitor", icon));
            _apps.Add(new App("Lock", new PNG(File.ReadAllBytes(path + "lock.png"))));
            _apps.Add(new App("Notepad", new PNG(File.ReadAllBytes(path + "notepad.png"))));
            _apps.Add(new App("TaskManager", new PNG(File.ReadAllBytes(path + "applications.png"))));
            _apps.Add(new App("Devices", new PNG(File.ReadAllBytes(path + "configure.png"))));
            // New apps (use tools icon fallback if specific icons missing)
            //Image browserIcon; 
            Image ircIcon; 
            //Image audioIcon; 
            //Image ftpIcon;
            //try { browserIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/web.png")); } catch { browserIcon = icon; }
            try { ircIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/chat.png")); } catch { ircIcon = icon; }
            //try { audioIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/music.png")); } catch { audioIcon = icon; }
            //try { ftpIcon = new PNG(File.ReadAllBytes("Images/BlueVelvet/32/network.png")); } catch { ftpIcon = icon; }
            //_apps.Add(new App("Anomalocaris", browserIcon));
            _apps.Add(new App("nexIRC", ircIcon));
            //_apps.Add(new App("Audica", audioIcon));
            //_apps.Add(new App("Gorganopsid", ftpIcon));
        }
        /// <summary>
        /// Load
        /// </summary>
        /// <param name="name"></param>
        public bool Load(string name) {
            var b = false;
            guideXOS.GUI.NotificationManager.Add(new Nofity("Loading App: " + name));
            for (int i = 0; i < _apps.Count; i++) {
                if (_apps[i].Name == name) {
                    switch (name) {
                        case "Devices":
                            _apps[i].AppObject = new Devices(400, 300);
                            b = true;
                            break;
                        case "Lock":
                            Lockscreen.Run();
                            b = true;
                            break;
                        case "Calculator":
                            _apps[i].AppObject = new Calculator(300, 500);
                            b = true;
                            break;
                        case "Monitor":
                            _apps[i].AppObject = new Monitor(200, 450);
                            b = true;
                            break;
                        case "Clock":
                            _apps[i].AppObject = new Clock(650, 500);
                            b = true;
                            break;
                        case "Paint":
                            _apps[i].AppObject = new Paint(500, 200);
                            b = true;
                            break;
                        case "Notepad":
                            _apps[i].AppObject = new Notepad(360, 200);
                            b = true;
                            break;
                        case "Console":
                            if (Program.FConsole == null) Program.FConsole = new FConsole(160, 120);
                            _apps[i].AppObject = Program.FConsole;
                            b = true;
                            break;
                        case "TaskManager":
                            _apps[i].AppObject = new TaskManager(500, 500);
                            b = true;
                            break;
                        case "Anomalocaris":
                            _apps[i].AppObject = new Anomalocaris(220, 180);
                            b = true;
                            break;
                        case "nexIRC":
                            _apps[i].AppObject = new nexIRC(260, 220);
                            b = true;
                            break;
                        case "Audica":
                            _apps[i].AppObject = new Audica(300, 240);
                            b = true;
                            break;
                        case "Gorganopsid":
                            _apps[i].AppObject = new Gorganopsid(340, 260);
                            b = true;
                            break;
                    }
                    if (b) {
                        // record recents
                        RecentManager.AddProgram(_apps[i].Name, _apps[i].Icon);
                    }
                }
            }
            return b;
        }
        /// <summary>
        /// Add
        /// </summary>
        /// <param name="app"></param>
        public void Add(App app) {
            _apps.Add(app);
        }
        /// <summary>
        /// Length
        /// </summary>
        public int Length {
            get {
                return _apps.Count;
            }
        }
        /// <summary>
        /// Name
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Name(int id) { return _apps[id].Name; }
        /// <summary>
        /// Icon
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Image Icon(int id) { return _apps[id].Icon; }
        #endregion
    }
}