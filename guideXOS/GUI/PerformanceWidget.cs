using guideXOS.Kernel.Drivers;
using guideXOS.Misc;
using System.Windows.Forms;
using System.Drawing;

namespace guideXOS.GUI {
    /// <summary>
    /// Minimal floating performance widget showing RAM and CPU usage
    /// </summary>
    internal class PerformanceWidget : Window {
        private const int WidgetWidth = 140;
        private const int WidgetHeight = 90;
        private const int Padding = 8;
        private const int CloseBtnSize = 16;
        
        private int _cpuPct = 0;
        private int _memPct = 0;
        private ulong _lastUpdateTick = 0;
        private const ulong UpdateIntervalMs = 500; // Update every 500ms
        
        // Cached strings to prevent memory leak
        private string _cpuText = "0%";
        private string _memText = "0%";
        
        private bool _closeHover = false;
        private bool _dragging = false;
        private int _dragOffsetX, _dragOffsetY;
        
        public PerformanceWidget() : base(
            Framebuffer.Width - WidgetWidth - 20,  // Position on right side
            80,  // Y position from top
            WidgetWidth, 
            WidgetHeight
        ) {
            Title = "Performance";
            BarHeight = 0; // No title bar
            ShowInTaskbar = false;
            ShowMaximize = false;
            ShowMinimize = false;
            ShowTombstone = false;
            IsResizable = false;
        }
        
        public override void OnInput() {
            // Don't call base.OnInput() to avoid default title bar dragging
            if (!Visible) return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);
            
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
        
        public override void OnDraw() {
            // Don't call base.OnDraw() - we'll draw our own minimal widget
            if (!Visible) return;
            
            // Update performance metrics periodically
            if (Timer.Ticks - _lastUpdateTick >= UpdateIntervalMs) {
                UpdateMetrics();
                _lastUpdateTick = Timer.Ticks;
            }
            
            // Draw widget background with subtle glow
            UIPrimitives.AFillRoundedRect(X - 2, Y - 2, Width + 4, Height + 4, 0x331E90FF, 8);
            UIPrimitives.AFillRoundedRect(X, Y, Width, Height, 0xDD1A1A1A, 6);
            
            // Draw border
            UIPrimitives.DrawRoundedRect(X, Y, Width, Height, 0xFF3A3A3A, 1, 6);
            
            int cy = Y + Padding;
            int cx = X + Padding;
            int contentWidth = Width - Padding * 2;
            
            // CPU Section
            DrawMetric(cx, cy, contentWidth, "CPU", _cpuPct, 0xFF5DADE2, _cpuText);
            cy += 30;
            
            // Memory Section
            DrawMetric(cx, cy, contentWidth, "RAM", _memPct, 0xFF58D68D, _memText);
            
            // Draw close button
            DrawCloseButton();
        }
        
        private void DrawMetric(int x, int y, int width, string label, int percent, uint barColor, string cachedText) {
            // Label and percentage
            WindowManager.font.DrawString(x, y, label);
            
            // Use cached text instead of creating new string
            int pctWidth = WindowManager.font.MeasureString(cachedText);
            WindowManager.font.DrawString(x + width - pctWidth, y, cachedText);
            
            // Progress bar
            int barY = y + WindowManager.font.FontSize + 3;
            int barHeight = 6;
            int barWidth = width - CloseBtnSize - 4; // Leave space for close button on first row
            
            // Background
            Framebuffer.Graphics.FillRectangle(x, barY, barWidth, barHeight, 0xFF2A2A2A);
            
            // Filled portion
            int fillWidth = barWidth * percent / 100;
            if (fillWidth > 0) {
                Framebuffer.Graphics.FillRectangle(x, barY, fillWidth, barHeight, barColor);
                
                // Subtle highlight on top
                Framebuffer.Graphics.FillRectangle(x, barY, fillWidth, 1, 0x55FFFFFF);
            }
            
            // Border
            Framebuffer.Graphics.DrawRectangle(x, barY, barWidth, barHeight, 0xFF444444, 1);
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
        
        private void UpdateMetrics() {
            // Get CPU usage
            int oldCpu = _cpuPct;
            _cpuPct = (int)ThreadPool.CPUUsage;
            if (_cpuPct < 0) _cpuPct = 0;
            if (_cpuPct > 100) _cpuPct = 100;
            
            // Only update cached string if value changed
            if (_cpuPct != oldCpu) {
                if (_cpuText != null) _cpuText.Dispose();
                _cpuText = _cpuPct.ToString() + "%";
            }
            
            // Get memory usage
            int oldMem = _memPct;
            ulong totalMem = Allocator.MemorySize;
            if (totalMem == 0) totalMem = 1; // Avoid division by zero
            ulong usedMem = Allocator.MemoryInUse;
            _memPct = (int)(usedMem * 100UL / totalMem);
            if (_memPct < 0) _memPct = 0;
            if (_memPct > 100) _memPct = 100;
            
            // Only update cached string if value changed
            if (_memPct != oldMem) {
                if (_memText != null) _memText.Dispose();
                _memText = _memPct.ToString() + "%";
            }
        }
    }
}
