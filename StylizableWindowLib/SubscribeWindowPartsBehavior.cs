using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using Microsoft.Windows.Shell;
using Native;

namespace StylizableWindowLib
{
    public class SubscribeWindowPartsBehavior:Behavior<Window>
    {
        private const string PART_Close = "PART_Close";
        private const string PART_Max = "PART_Max";
        private const string PART_Min = "PART_Min";
        private const string PART_Icon = "PART_Icon";
        private const string PART_WindowTitleBackground = "PART_WindowTitleBackground";
        private const string PART_WindowResizeGrip = "PART_WindowResizeGrip";

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObjectOnLoaded;
            AssociatedObject.Unloaded += AssociatedObjectOnUnloaded;
        }
        
        protected override void OnDetaching()
        {
            base.OnDetaching();
            ClearEvents();
        }

        private void AssociatedObjectOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            OnApplyTemplate();
        }

        private void AssociatedObjectOnUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            ClearEvents();
            AssociatedObject.Loaded -= AssociatedObjectOnLoaded;
            AssociatedObject.Unloaded -= AssociatedObjectOnUnloaded;
        }

        private ButtonBase _closeButton, _minButton, _maxButton;
        private UIElement _icon;
        private UIElement _titleBarBackground;
        private UIElement _resizeGrip;
        public void OnApplyTemplate()
        {
            var template = AssociatedObject.Template;
            if (template == null) return;

            _closeButton = template.FindName(PART_Close, AssociatedObject) as ButtonBase;
            _minButton = template.FindName(PART_Min, AssociatedObject) as ButtonBase;
            _maxButton = template.FindName(PART_Max, AssociatedObject) as ButtonBase;
            _icon = template.FindName(PART_Icon, AssociatedObject) as UIElement;
            _titleBarBackground = template.FindName(PART_WindowTitleBackground, AssociatedObject) as UIElement;
            _resizeGrip = template.FindName(PART_WindowResizeGrip, AssociatedObject) as UIElement;
            SetEvents();
        }

        private void SetEvents()
        {
            ClearEvents();

            if (_closeButton != null) _closeButton.Click += CloseClick;
            if (_minButton != null) _minButton.Click += MinimizeClick;
            if (_maxButton != null) _maxButton.Click += MaximizeClick;
            if (_icon != null) _icon.MouseDown += IconMouseDown;
            if (_titleBarBackground != null)
            {
                _titleBarBackground.MouseDown += TitleBarMouseDown;
                _titleBarBackground.MouseUp += TitleBarMouseUp;
            }
            if (_resizeGrip != null) _resizeGrip.MouseDown += ResizeWithGrip;
        }

        private void ClearEvents()
        {
            if (_closeButton != null) _closeButton.Click -= CloseClick;
            if (_minButton != null) _minButton.Click -= MinimizeClick;
            if (_maxButton != null) _maxButton.Click -= MaximizeClick;
            if (_icon != null) _icon.MouseDown -= IconMouseDown;
            if (_titleBarBackground != null)
            {
                _titleBarBackground.MouseDown -= TitleBarMouseDown;
                _titleBarBackground.MouseUp -= TitleBarMouseUp;
            }
            if (_resizeGrip != null) _resizeGrip.MouseDown -= ResizeWithGrip;
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.ResizeMode == ResizeMode.NoResize)
                return;
            SystemCommands.MinimizeWindow(AssociatedObject);
        }

        private void MaximizeClick(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject.ResizeMode == ResizeMode.CanMinimize ||
                AssociatedObject.ResizeMode == ResizeMode.NoResize)
                return;

            if (AssociatedObject.WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(AssociatedObject);
            else
                SystemCommands.MaximizeWindow(AssociatedObject);

        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Close();
        }

        private void IconMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    AssociatedObject.Close();
                }
                else
                {
                    var mPoint = Mouse.GetPosition(AssociatedObject);
                    ShowSystemMenuPhysicalCoordinates(AssociatedObject, AssociatedObject.PointToScreen(new Point(0, mPoint.Y + 10)));
                }
            }
        }

        private static void ShowSystemMenuPhysicalCoordinates(Window window, Point physicalScreenLocation)
        {
            if (window == null) return;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || !UnsafeNativeMethods.IsWindow(hwnd))
                return;

            var hmenu = UnsafeNativeMethods.GetSystemMenu(hwnd, false);

            var cmd = UnsafeNativeMethods.TrackPopupMenuEx(hmenu, Constants.TPM_LEFTBUTTON | Constants.TPM_RETURNCMD,
                (int)physicalScreenLocation.X, (int)physicalScreenLocation.Y, hwnd, IntPtr.Zero);
            if (0 != cmd)
                UnsafeNativeMethods.PostMessage(hwnd, Constants.SYSCOMMAND, new IntPtr(cmd), IntPtr.Zero);
        }

        protected void TitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                UnsafeNativeMethods.ReleaseCapture();

                var mPoint = Mouse.GetPosition(AssociatedObject);

                var wpfPoint = AssociatedObject.PointToScreen(mPoint);
                var x = Convert.ToInt16(wpfPoint.X);
                var y = Convert.ToInt16(wpfPoint.Y);
                var lParam = (int)(uint)x | (y << 16);
                var handle = new WindowInteropHelper(AssociatedObject).Handle;
                UnsafeNativeMethods.SendMessage(handle, Constants.WM_NCLBUTTONDOWN, Constants.HT_CAPTION, lParam);

                if (e.ClickCount == 2 && (AssociatedObject.ResizeMode == ResizeMode.CanResizeWithGrip || AssociatedObject.ResizeMode == ResizeMode.CanResize))
                {
                    if (AssociatedObject.WindowState == WindowState.Maximized)
                        SystemCommands.RestoreWindow(AssociatedObject);
                    else
                        SystemCommands.MaximizeWindow(AssociatedObject);
                }
            }
        }

        protected void TitleBarMouseUp(object sender, MouseButtonEventArgs e)
        {
            var mousePosition = e.GetPosition(AssociatedObject);
            if (e.ChangedButton == MouseButton.Right)
                ShowSystemMenuPhysicalCoordinates(AssociatedObject, AssociatedObject.PointToScreen(mousePosition));


        }

        private void ResizeWithGrip(object sender, MouseButtonEventArgs e)
        {
            var handle = new WindowInteropHelper(AssociatedObject).Handle;
            UnsafeNativeMethods.SendMessage(handle, Constants.WM_SYSCOMMAND, Constants.SC_SIZE + (int)Constants.SizingAction.SouthEast, 0);
        }
    }
}
