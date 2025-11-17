using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System.Windows.Forms;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// Comprehensive demonstration window for GXM GUI controls and callbacks
    /// Shows all available features: buttons, labels, lists, dropdowns, and event handling
    /// </summary>
    internal class GUISamples : Window {
        private GXMScriptWindow _demo1;
        private GXMScriptWindow _demo2;
        private GXMScriptWindow _demo3;
        private GXMScriptWindow _demo4;
        
        private bool _clickLatch;
        private int _btnW = 180;
        private int _btnH = 32;
        private bool _samplesBuilt = false;
        private int _framesSinceVisible = 0;
        private int _framesAfterBuild = 0; // Track frames after building to gradually show windows
        
        public GUISamples(int x, int y) : base(x, y, 640, 480) {
            Title = "GXM GUI Samples";
            ShowMinimize = true;
            ShowMaximize = true;
            ShowInTaskbar = true;
            ShowTombstone = true;
            IsResizable = true;
            // Don't build samples in constructor - wait for a few frames after becoming visible
        }
        
        private void BuildSamples() {
            if (_samplesBuilt) return;
            _samplesBuilt = true;
            
            // Create the demo window but DON'T show it yet
            // We'll show it in OnDraw() after a few more frames
            
            // Sample 1: Basic buttons + click callbacks
            _demo1 = new GXMScriptWindow("Button Demo", 280, 220);
            _demo1.AddLabel("Click the buttons below:", 12, 12);
            _demo1.AddButton(1, "Show Message", 12, 50, 120, 28);
            _demo1.AddButton(4, "Close Window", 12, 88, 120, 28);
            _demo1.AddOnClick(1, "MSG", "Hello from GXM scripting!");
            _demo1.AddOnClick(4, "CLOSE", "");
            _demo1.X = this.X + 12;
            _demo1.Y = this.Y + 60;
            
            // DON'T SET VISIBLE YET - will be done in OnDraw
            // This allows WindowManager to properly register the window first
            WindowManager.MoveToEnd(_demo1);
            // _demo1.Visible = true;  // REMOVED - will be set later
            
            // COMMENT OUT OTHER WINDOWS FOR NOW - testing with just one
            /*
            // Sample 2: List view + change events
            _demo2 = new GXMScriptWindow("ListView Demo", 300, 280);
            _demo2.AddLabel("Select an item from the list:", 12, 12);
            _demo2.AddList(10, 12, 50, 270, 160, "Alpha;Beta;Gamma;Delta;Epsilon;Zeta;Eta;Theta");
            _demo2.AddOnChange(10, "MSG", "You selected: $VALUE");
            _demo2.AddButton(99, "Close", 12, 220, 100, 28);
            _demo2.AddOnClick(99, "CLOSE", "");
            _demo2.X = this.X + this.Width - _demo2.Width - 12;
            _demo2.Y = this.Y + 60;
            
            // Sample 3: Dropdown + change events
            _demo3 = new GXMScriptWindow("Dropdown Demo", 320, 240);
            _demo3.AddLabel("Choose a color:", 12, 12);
            _demo3.AddDropdown(20, 12, 50, 200, 28, "Red;Green;Blue;Yellow;Orange;Purple;Pink;Cyan;Magenta;Brown");
            _demo3.AddOnChange(20, "MSG", "Color selected: $VALUE");
            _demo3.AddLabel("Choose an app to launch:", 12, 90);
            _demo3.AddDropdown(21, 12, 120, 200, 28, "Notepad;Calculator;Paint;Console;Clock");
            _demo3.AddOnChange(21, "OPENAPP", "$VALUE");
            _demo3.AddButton(98, "Close", 12, 180, 100, 28);
            _demo3.AddOnClick(98, "CLOSE", "");
            _demo3.X = this.X + (this.Width / 2) - (_demo3.Width / 2);
            _demo3.Y = this.Y + this.Height - _demo3.Height - 20;
            
            // Sample 4: Combined demo - all features
            _demo4 = new GXMScriptWindow("All Features", 420, 380);
            _demo4.AddLabel("Comprehensive GXM Feature Demo", 12, 12);
            
            // Buttons section
            _demo4.AddLabel("Buttons:", 12, 40);
            _demo4.AddButton(30, "Message", 12, 65, 90, 26);
            _demo4.AddButton(31, "Notepad", 110, 65, 90, 26);
            _demo4.AddButton(32, "Paint", 208, 65, 90, 26);
            _demo4.AddOnClick(30, "MSG", "Button clicked!");
            _demo4.AddOnClick(31, "OPENAPP", "Notepad");
            _demo4.AddOnClick(32, "OPENAPP", "Paint");
            
            // List section
            _demo4.AddLabel("List Selection:", 12, 105);
            _demo4.AddList(40, 12, 130, 180, 100, "Item 1;Item 2;Item 3;Item 4;Item 5");
            _demo4.AddOnChange(40, "MSG", "List: $VALUE");
            
            // Dropdown section
            _demo4.AddLabel("Dropdown:", 210, 105);
            _demo4.AddDropdown(50, 210, 130, 190, 28, "Option A;Option B;Option C;Option D");
            _demo4.AddOnChange(50, "MSG", "Dropdown: $VALUE");
            
            // Action buttons
            _demo4.AddButton(60, "Launch Calculator", 210, 180, 190, 30);
            _demo4.AddButton(61, "Launch Clock", 210, 220, 190, 30);
            _demo4.AddOnClick(60, "OPENAPP", "Calculator");
            _demo4.AddOnClick(61, "OPENAPP", "Clock");
            
            // Close button
            _demo4.AddButton(97, "Close All Demos", 12, 340, 180, 28);
            _demo4.AddOnClick(97, "CLOSE", "");
            
            _demo4.X = this.X + 12;
            _demo4.Y = this.Y + this.Height - _demo4.Height - 20;
            */
            
            // Show only first window immediately
            if (_demo1 != null) {
                WindowManager.MoveToEnd(_demo1);
                _demo1.Visible = true;
            }
        }
        
        public override void OnInput() {
            base.OnInput();
            
            if (!Visible || IsMinimized)
                return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool left = Control.MouseButtons.HasFlag(MouseButtons.Left);
            
            int pad = 12;
            int btnX = X + pad;
            int btnY = Y + 80;
            int btnGap = 8;
            
            if (left) {
                if (!_clickLatch) {
                    // Create new demo button
                    if (mx >= btnX && mx <= btnX + _btnW && 
                        my >= btnY && my <= btnY + _btnH) {
                        CreateNewDemo();
                        _clickLatch = true;
                    }
                    
                    // Reload demos button
                    btnY += _btnH + btnGap;
                    if (mx >= btnX && mx <= btnX + _btnW && 
                        my >= btnY && my <= btnY + _btnH) {
                        ReloadDemos();
                        _clickLatch = true;
                    }
                    
                    // Hide all button
                    btnY += _btnH + btnGap;
                    if (mx >= btnX && mx <= btnX + _btnW && 
                        my >= btnY && my <= btnY + _btnH) {
                        HideAllDemos();
                        _clickLatch = true;
                    }
                    
                    // Show all button
                    btnY += _btnH + btnGap;
                    if (mx >= btnX && mx <= btnX + _btnW && 
                        my >= btnY && my <= btnY + _btnH) {
                        ShowAllDemos();
                        _clickLatch = true;
                    }
                }
            } else {
                _clickLatch = false;
            }
        }
        
        public override void OnDraw() {
            base.OnDraw();
            
            // Build samples after a few frames when window is visible
            // This ensures the parent window is fully registered and drawn at least once
            if (!_samplesBuilt && Visible) {
                _framesSinceVisible++;
                if (_framesSinceVisible >= 3) {
                    BuildSamples();
                }
            }
            
            // Show demo windows after they've been registered for a few frames
            if (_samplesBuilt && _framesAfterBuild < 20) {
                _framesAfterBuild++;
                
                // Show demo1 after 3 frames from building
                if (_framesAfterBuild == 3 && _demo1 != null && !_demo1.Visible) {
                    _demo1.Visible = true;
                }
                
                // Future: show demo2, demo3, demo4 at frames 8, 13, 18
            }
            
            int pad = 12;
            int y = Y + pad;
            
            // Title
            WindowManager.font.DrawString(X + pad, y, "GXM GUI Scripting Samples");
            y += WindowManager.font.FontSize + 8;
            
            // Description
            string desc = "This window demonstrates GXM GUI scripting capabilities.";
            WindowManager.font.DrawString(X + pad, y, desc, Width - pad * 2, WindowManager.font.FontSize * 2);
            y += WindowManager.font.FontSize * 2 + 12;
            
            // Add test message
            y += 20;
            WindowManager.font.DrawString(X + pad, y, "[TEST MODE: Only showing 1 demo window]");
            y += WindowManager.font.FontSize + 4;
            WindowManager.font.DrawString(X + pad, y, "Window will appear after 3 frames from creation.");
            y += WindowManager.font.FontSize + 4;
            if (_framesAfterBuild > 0) {
                WindowManager.font.DrawString(X + pad, y, $"Frames after build: {_framesAfterBuild}");
            }
            
            // Control buttons
            int btnX = X + pad;
            int btnY = Y + 80;
            int btnGap = 8;
            
            // Button: Create New Demo
            uint col1 = UI.ButtonFillColor(btnX, btnY, _btnW, _btnH, 0xFF3A3A3A, 0xFF444444, 0xFF4A4A4A);
            Framebuffer.Graphics.FillRectangle(btnX, btnY, _btnW, _btnH, col1);
            WindowManager.font.DrawString(btnX + 12, btnY + (_btnH / 2 - WindowManager.font.FontSize / 2), 
                "Create Test Window");
            btnY += _btnH + btnGap;
            
            // Button: Reload Demos
            uint col2 = UI.ButtonFillColor(btnX, btnY, _btnW, _btnH, 0xFF3A3A3A, 0xFF444444, 0xFF4A4A4A);
            Framebuffer.Graphics.FillRectangle(btnX, btnY, _btnW, _btnH, col2);
            WindowManager.font.DrawString(btnX + 12, btnY + (_btnH / 2 - WindowManager.font.FontSize / 2), 
                "Reload All Demos");
            btnY += _btnH + btnGap;
            
            // Button: Hide All
            uint col3 = UI.ButtonFillColor(btnX, btnY, _btnW, _btnH, 0xFF3A3A3A, 0xFF444444, 0xFF4A4A4A);
            Framebuffer.Graphics.FillRectangle(btnX, btnY, _btnW, _btnH, col3);
            WindowManager.font.DrawString(btnX + 12, btnY + (_btnH / 2 - WindowManager.font.FontSize / 2), 
                "Hide All Demos");
            btnY += _btnH + btnGap;
            
            // Button: Show All
            uint col4 = UI.ButtonFillColor(btnX, btnY, _btnW, _btnH, 0xFF3A3A3A, 0xFF444444, 0xFF4A4A4A);
            Framebuffer.Graphics.FillRectangle(btnX, btnY, _btnW, _btnH, col4);
            WindowManager.font.DrawString(btnX + 12, btnY + (_btnH / 2 - WindowManager.font.FontSize / 2), 
                "Show All Demos");
            
            // Info text
            y = btnY + _btnH + 20;
            WindowManager.font.DrawString(X + pad, y, 
                "Each demo window shows different GXM features.", 
                Width - pad * 2, WindowManager.font.FontSize * 3);
            y += WindowManager.font.FontSize + 8;
            WindowManager.font.DrawString(X + pad, y, 
                "Interact with controls to test callbacks and events.", 
                Width - pad * 2, WindowManager.font.FontSize * 3);
        }
        
        private void CreateNewDemo() {
            var testWin = new GXMScriptWindow("Test Window", 360, 260);
            testWin.AddLabel("Quick Test Window", 12, 12);
            testWin.AddButton(1, "Show Alert", 12, 50, 120, 28);
            testWin.AddOnClick(1, "MSG", "Test alert from new window!");
            testWin.AddList(2, 12, 90, 200, 100, "Test Item 1;Test Item 2;Test Item 3");
            testWin.AddOnChange(2, "MSG", "Selected: $VALUE");
            testWin.AddButton(99, "Close", 12, 200, 100, 28);
            testWin.AddOnClick(99, "CLOSE", "");
            
            testWin.X = this.X + 220;
            testWin.Y = this.Y + 100;
            WindowManager.MoveToEnd(testWin);
            testWin.Visible = true;
        }
        
        private void ReloadDemos() {
            // Close existing demos
            if (_demo1 != null) _demo1.Visible = false;
            if (_demo2 != null) _demo2.Visible = false;
            if (_demo3 != null) _demo3.Visible = false;
            if (_demo4 != null) _demo4.Visible = false;
            
            // Reset flags and rebuild
            _samplesBuilt = false;
            _framesSinceVisible = 0;
            _framesAfterBuild = 0;
        }
        
        private void HideAllDemos() {
            if (_demo1 != null) _demo1.Visible = false;
            if (_demo2 != null) _demo2.Visible = false;
            if (_demo3 != null) _demo3.Visible = false;
            if (_demo4 != null) _demo4.Visible = false;
        }
        
        private void ShowAllDemos() {
            if (_demo1 != null) {
                _demo1.Visible = true;
                WindowManager.MoveToEnd(_demo1);
            }
            if (_demo2 != null) {
                _demo2.Visible = true;
                WindowManager.MoveToEnd(_demo2);
            }
            if (_demo3 != null) {
                _demo3.Visible = true;
                WindowManager.MoveToEnd(_demo3);
            }
            if (_demo4 != null) {
                _demo4.Visible = true;
                WindowManager.MoveToEnd(_demo4);
            }
        }
    }
}