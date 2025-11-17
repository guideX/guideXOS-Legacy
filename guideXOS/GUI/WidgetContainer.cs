using guideXOS.Kernel.Drivers;
using System.Collections.Generic;
using System.Windows.Forms;

namespace guideXOS.GUI {
    /// <summary>
    /// Container window that holds multiple docked widgets
    /// </summary>
    internal class WidgetContainer : Window {
        private List<DockableWidget> _widgets;
        private const int Padding = 8;
        private const int CloseBtnSize = 16;
        private const int WidgetGap = 10;
        
        private bool _dragging = false;
        private int _dragOffsetX, _dragOffsetY;
        private bool _closeHover = false;
        private int _hoverWidgetIndex = -1; // Track which widget is being hovered for undocking
        
        public WidgetContainer(int x, int y) : base(x, y, 140, 90) {
            Title = "Widgets";
            BarHeight = 0;
            ShowInTaskbar = false;
            ShowMaximize = false;
            ShowMinimize = false;
            ShowTombstone = false;
            IsResizable = false;
            
            _widgets = new List<DockableWidget>();
        }
        
        public void AddWidget(DockableWidget widget) {
            // Check if widget is already in the list
            bool alreadyContains = false;
            for (int i = 0; i < _widgets.Count; i++) {
                if (_widgets[i] == widget) {
                    alreadyContains = true;
                    break;
                }
            }
            
            if (!alreadyContains) {
                _widgets.Add(widget);
                widget.DockedContainer = this;
                UpdateLayout();
            }
        }
        
        public void RemoveWidget(DockableWidget widget) {
            if (_widgets.Remove(widget)) {
                widget.DockedContainer = null;
                UpdateLayout();
                
                // If only one widget left, undock it
                if (_widgets.Count == 1) {
                    var remaining = _widgets[0];
                    remaining.DockedContainer = null;
                    remaining.Visible = true;
                    remaining.X = X;
                    remaining.Y = Y;
                    _widgets.Clear();
                    Visible = false;
                }
                // If no widgets left, hide container
                else if (_widgets.Count == 0) {
                    Visible = false;
                }
            }
        }
        
        private void UpdateLayout() {
            if (_widgets.Count == 0) {
                return;
            }
            
            // Calculate total height needed
            int totalHeight = Padding;
            int maxWidth = 140;
            
            for (int i = 0; i < _widgets.Count; i++) {
                totalHeight += _widgets[i].PreferredHeight;
                if (i < _widgets.Count - 1) {
                    totalHeight += WidgetGap;
                }
                if (_widgets[i].Width > maxWidth) {
                    maxWidth = _widgets[i].Width;
                }
            }
            totalHeight += Padding;
            
            // Update container size
            Width = maxWidth;
            Height = totalHeight;
        }
        
        public override void OnInput() {
            if (!Visible) return;
            
            int mx = Control.MousePosition.X;
            int my = Control.MousePosition.Y;
            bool leftDown = Control.MouseButtons.HasFlag(MouseButtons.Left);
            
            // Close button hit test
            int closeX = X + Width - Padding - CloseBtnSize;
            int closeY = Y + Padding;
            _closeHover = (mx >= closeX && mx <= closeX + CloseBtnSize && 
                          my >= closeY && my <= closeY + CloseBtnSize);
            
            // Update hover widget index
            _hoverWidgetIndex = -1;
            if (!leftDown && _widgets.Count > 1) {
                int currentY = Y + Padding;
                for (int i = 0; i < _widgets.Count; i++) {
                    var widget = _widgets[i];
                    int widgetHeight = widget.PreferredHeight;
                    
                    if (mx >= X && mx <= X + Width && 
                        my >= currentY && my <= currentY + widgetHeight) {
                        _hoverWidgetIndex = i;
                        break;
                    }
                    
                    currentY += widgetHeight;
                    if (i < _widgets.Count - 1) {
                        currentY += WidgetGap;
                    }
                }
            }
            
            if (leftDown) {
                // Check if clicking close button
                if (_closeHover) {
                    // Close all widgets
                    for (int i = 0; i < _widgets.Count; i++) {
                        _widgets[i].Visible = false;
                        _widgets[i].DockedContainer = null;
                    }
                    _widgets.Clear();
                    Visible = false;
                    return;
                }
                
                // Check if clicking on a specific widget to undock it
                if (!_dragging && _widgets.Count > 1 && _hoverWidgetIndex >= 0) {
                    UndockWidget(_widgets[_hoverWidgetIndex], mx, my);
                    return;
                }
                
                // Start dragging container
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
        
        private void UndockWidget(DockableWidget widget, int mouseX, int mouseY) {
            // Remove from container
            RemoveWidget(widget);
            
            // Make widget visible and position at mouse
            widget.Visible = true;
            widget.X = mouseX - widget.Width / 2;
            widget.Y = mouseY - 10; // Slight offset from mouse
            
            // Clamp to screen
            if (widget.X < 0) widget.X = 0;
            if (widget.Y < 0) widget.Y = 0;
            if (widget.X + widget.Width > Framebuffer.Width) widget.X = Framebuffer.Width - widget.Width;
            if (widget.Y + widget.Height > Framebuffer.Height) widget.Y = Framebuffer.Height - widget.Height;
            
            WindowManager.MoveToEnd(widget);
        }
        
        public override void OnDraw() {
            if (!Visible || _widgets.Count == 0) return;
            
            // Draw container background with subtle glow
            UIPrimitives.AFillRoundedRect(X - 2, Y - 2, Width + 4, Height + 4, 0x331E90FF, 8);
            UIPrimitives.AFillRoundedRect(X, Y, Width, Height, 0xDD1A1A1A, 6);
            
            // Draw border
            UIPrimitives.DrawRoundedRect(X, Y, Width, Height, 0xFF3A3A3A, 1, 6);
            
            // Draw each widget's content
            int currentY = Y + Padding;
            for (int i = 0; i < _widgets.Count; i++) {
                var widget = _widgets[i];
                int contentX = X + Padding;
                int contentWidth = Width - Padding * 2;
                int widgetHeight = widget.PreferredHeight;
                
                // Highlight widget if hovering (only when multiple widgets and not dragging)
                if (_hoverWidgetIndex == i && _widgets.Count > 1 && !_dragging) {
                    // Draw subtle highlight background
                    UIPrimitives.AFillRoundedRect(
                        X + 2, 
                        currentY - 2, 
                        Width - 4, 
                        widgetHeight + 4, 
                        0x333F7FBF, 
                        4
                    );
                }
                
                widget.DrawContent(contentX, currentY, contentWidth);
                
                currentY += widgetHeight;
                
                // Draw separator line between widgets
                if (i < _widgets.Count - 1) {
                    int lineY = currentY + WidgetGap / 2;
                    Framebuffer.Graphics.DrawLine(X + Padding, lineY, X + Width - Padding, lineY, 0xFF3A3A3A);
                    currentY += WidgetGap;
                }
            }
            
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
        
        public override void Dispose() {
            if (_widgets != null) {
                _widgets.Dispose();
                _widgets = null;
            }
            base.Dispose();
        }
    }
}
