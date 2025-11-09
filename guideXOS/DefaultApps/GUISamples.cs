using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
namespace guideXOS.DefaultApps {
    // Demonstration window for GUI controls and callbacks
    internal class GUISamples : Window {
        private GXMScriptWindow _demo1;
        private GXMScriptWindow _demo2;
        private GXMScriptWindow _demo3;
        public GUISamples(int x,int y):base(x,y,540,420){ Title="GUI Samples"; ShowInTaskbar=true; ShowMinimize=true; ShowMaximize=false; ShowTombstone=true; ShowRestore=true; BuildSamples(); }
        private void BuildSamples(){
            // Sample 1: Buttons + callbacks
            _demo1 = new GXMScriptWindow("Buttons", 240, 200);
            _demo1.AddLabel("Button callbacks", 12, 12);
            _demo1.AddButton(1,"Hello",12,50,80,28);
            _demo1.AddButton(2,"Open Notepad",12,90,140,28);
            _demo1.AddOnClick(1,"MSG","Hello button pressed");
            _demo1.AddOnClick(2,"OPENAPP","Notepad");
            // Position near parent
            _demo1.X = this.X + 12; _demo1.Y = this.Y + 60;
            // Sample 2: List + dropdown + change events
            _demo2 = new GXMScriptWindow("Lists", 260, 220);
            _demo2.AddLabel("Select item or color",12,12);
            _demo2.AddList(10,12,50,120,120,"Alpha;Beta;Gamma;Delta");
            _demo2.AddDropdown(20,150,50,100,24,"Red;Green;Blue;Orange");
            _demo2.AddOnChange(10,"MSG","List picked: $VALUE");
            _demo2.AddOnChange(20,"MSG","Color: $VALUE");
            _demo2.X = this.X + this.Width - _demo2.Width - 12; _demo2.Y = this.Y + 60;
            // Sample 3: Mixed actions
            _demo3 = new GXMScriptWindow("Mixed", 260, 200);
            _demo3.AddLabel("Close via button",12,12);
            _demo3.AddButton(5,"Close All",12,50,100,28);
            _demo3.AddOnClick(5,"CLOSE","ignored");
            _demo3.X = this.X + (this.Width/2) - (_demo3.Width/2); _demo3.Y = this.Y + this.Height - _demo3.Height - 20;
            // Ensure windows at end of z-order and visible
            WindowManager.MoveToEnd(_demo1); _demo1.Visible=true;
            WindowManager.MoveToEnd(_demo2); _demo2.Visible=true;
            WindowManager.MoveToEnd(_demo3); _demo3.Visible=true;
        }
        public override void OnDraw(){ base.OnDraw(); int pad=12; WindowManager.font.DrawString(X+pad,Y+pad,"GUI Control Samples:"); WindowManager.font.DrawString(X+pad,Y+pad+24,"Buttons, Lists, Dropdowns & callbacks."); }
    }
}
