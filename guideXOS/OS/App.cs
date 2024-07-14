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
            var icon = new PNG(File.ReadAllBytes("Images/tools.png"));
            _apps.Add(new App("Calculator", new PNG(File.ReadAllBytes("Images/calculator.png"))));
            _apps.Add(new App("Clock", icon));
            _apps.Add(new App("Paint", icon));
            _apps.Add(new App("Console", icon));
            _apps.Add(new App("Monitor", icon));
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
                        case "Console":
                            Program.FConsole.Visible = true;
                            _apps[i].AppObject = Program.FConsole;
                            b = true;
                            break;
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
        public string Name(int id) {
            return _apps[id].Name;
        }
        /// <summary>
        /// Icon
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Image Icon(int id) {
            return _apps[id].Icon;
        }
        #endregion
    }
}