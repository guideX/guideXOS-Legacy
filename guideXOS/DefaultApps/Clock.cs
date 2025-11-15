using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace guideXOS.DefaultApps {
    /// <summary>
    /// Clock - Floating widget with analog and digital display
    /// </summary>
    internal class Clock : Window {
        /// <summary>
        /// Sine lookup table
        /// </summary>
        static int[] sine;
        
        private const int WidgetWidth = 200;
        private const int WidgetHeight = 200;
        private const int Padding = 8;
        private const int CloseBtnSize = 16;
        
        private bool _closeHover = false;
        private bool _dragging = false;
        private int _dragOffsetX, _dragOffsetY;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        public Clock(int X, int Y) : base(X, Y, WidgetWidth, WidgetHeight) {
            Title = "Clock";
            BarHeight = 0; // No title bar
            IsResizable = false;
            ShowInTaskbar = false; // Don't show in taskbar
            ShowMaximize = false;
            ShowMinimize = false;
            ShowRestore = false;
            ShowTombstone = false;
            sine = new int[16] {
        		0,
        		27,
        		54,
        		79,
        		104,
        		128,
        		150,
        		171,
        		190,
        		201,
        		221,
        		233,
        		243,
        		250,
        		254,
        		255
      		};
        }
        
        public override void OnInput() {
            // Don't call base.OnInput() to avoid default title bar dragging
            if (!Visible) return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons.HasFlag(System.Windows.Forms.MouseButtons.Left);
            
            // Close button hit test
            int closeX = X + Width - Padding - CloseBtnSize;
            int closeY = Y + Padding;
            _closeHover = (mx >= closeX && mx <= closeX + CloseBtnSize && 
                          my >= closeY && my <= closeY + CloseBtnSize);
            
            if (leftDown) {
                // Check if clicking close button
                if (_closeHover) {
                    Visible = false;
                    return;
                }
                
                // Start dragging if clicked anywhere else in the widget
                if (!_dragging && mx >= X && mx <= X + Width && my >= Y && my <= Y + Height) {
                    _dragging = true;
                    _dragOffsetX = mx - X;
                    _dragOffsetY = my - Y;
                }
                
                // Handle dragging
                if (_dragging) {
                    X = mx - _dragOffsetX;
                    Y = my - _dragOffsetY;
                    
                    // Clamp to screen bounds
                    if (X < 0) X = 0;
                    if (Y < 0) Y = 0;
                    if (X + Width > Framebuffer.Width) X = Framebuffer.Width - Width;
                    if (Y + Height > Framebuffer.Height) Y = Framebuffer.Height - Height;
                }
            } else {
                _dragging = false;
            }
        }
        
        /// <summary>
        /// On Draw
        /// </summary>
        public override void OnDraw() {
            // Don't call base.OnDraw() - we'll draw our own minimal widget
            if (!Visible) return;
            
            // Draw widget background with subtle glow
            UIPrimitives.AFillRoundedRect(X - 2, Y - 2, Width + 4, Height + 4, 0x331E90FF, 8);
            UIPrimitives.AFillRoundedRect(X, Y, Width, Height, 0xDD1A1A1A, 6);
            
            // Draw border
            UIPrimitives.DrawRoundedRect(X, Y, Width, Height, 0xFF3A3A3A, 1, 6);
            
            // Draw analog clock hands
            int centerX = X + Width / 2;
            int centerY = Y + Height / 2;
            
            int second = RTC.Second * 6;
            DrawHand(centerX, centerY, second, Width / 3, 0xFFFF5555);
            int minute = RTC.Minute * 6;
            DrawHand(centerX, centerY, minute, Width / 4, 0xFFCCCCCC);
            int hour = (RTC.Hour >= 12 ? RTC.Hour - 12 : RTC.Hour) * 30;
            DrawHand(centerX, centerY, hour, Width / 6, 0xFFFFFFFF);
            
            // Draw digital time at top
            string devider = ":";
            string shour = RTC.Hour.ToString();
            string sminute = RTC.Minute.ToString();
            string ssecond = RTC.Second.ToString();
            string result = shour + devider + sminute + devider + ssecond;
            WindowManager.font.DrawString(X + Width / 2 - WindowManager.font.MeasureString(result) / 2, Y + Padding + 4, result);
            devider.Dispose();
            shour.Dispose();
            sminute.Dispose();
            ssecond.Dispose();
            result.Dispose();
            
            // Draw close button
            DrawCloseButton();
        }
        
        private void DrawCloseButton() {
            int closeX = X + Width - Padding - CloseBtnSize;
            int closeY = Y + Padding;
            
            // Button background
            uint btnColor = _closeHover ? 0xFFFF5555 : 0xFF883333;
            UIPrimitives.AFillRoundedRect(closeX, closeY, CloseBtnSize, CloseBtnSize, btnColor, 3);
            
            // X symbol
            uint xColor = _closeHover ? 0xFFFFFFFF : 0xFFCCCCCC;
            int pad = 4;
            
            // Draw X with 2 lines
            Framebuffer.Graphics.DrawLine(closeX + pad, closeY + pad, 
                                         closeX + CloseBtnSize - pad, closeY + CloseBtnSize - pad, xColor);
            Framebuffer.Graphics.DrawLine(closeX + CloseBtnSize - pad, closeY + pad, 
                                         closeX + pad, closeY + CloseBtnSize - pad, xColor);
            
            // Draw again shifted by 1px for thickness
            Framebuffer.Graphics.DrawLine(closeX + pad + 1, closeY + pad, 
                                         closeX + CloseBtnSize - pad + 1, closeY + CloseBtnSize - pad, xColor);
            Framebuffer.Graphics.DrawLine(closeX + CloseBtnSize - pad, closeY + pad + 1, 
                                         closeX + pad, closeY + CloseBtnSize - pad + 1, xColor);
        }
        
        /// <summary>
        /// Draw Hand
        /// </summary>
        /// <param name="xStart"></param>
        /// <param name="yStart"></param>
        /// <param name="angle"></param>
        /// <param name="radius"></param>
        /// <param name="color"></param>
        void DrawHand(int xStart, int yStart, int angle, int radius, uint color) {
            if (angle >= 0 && angle <= 360) {
                lock (this) {
                    angle /= 6;
                    int xEnd, yEnd, quadrant, x_flip, y_flip;
                    quadrant = angle / 15;
                    switch (quadrant) {
                        case 0:
                            x_flip = 1;
                            y_flip = -1;
                            break;
                        case 1:
                            angle = Math.Abs(angle - 30);
                            x_flip = y_flip = 1;
                            break;
                        case 2:
                            angle = angle - 30;
                            x_flip = -1;
                            y_flip = 1;
                            break;
                        case 3:
                            angle = Math.Abs(angle - 60);
                            x_flip = y_flip = -1;
                            break;
                        default:
                            x_flip = y_flip = 1;
                            break;
                    }
                    if (angle > sine.Length - 1) { } else {
                        xEnd = xStart;
                        yEnd = yStart;
                        xEnd += x_flip * (sine[angle] * radius >> 8);
                        yEnd += y_flip * (sine[15 - angle] * radius >> 8);
                        Framebuffer.Graphics.DrawLine(xStart, yStart, xEnd, yEnd, color);
                    }
                }
            }
        }
    }
}