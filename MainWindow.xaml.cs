using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WindowCaptureExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Hardcoded folder for saving screenshots
        private static readonly string _captureFolder = @"C:\WindowCaptures";

        // Low-level mouse hook
        private static IntPtr _hookId = IntPtr.Zero;
        private static LowLevelMouseProc _proc = HookCallback;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure folder exists
            if (!Directory.Exists(_captureFolder))
                Directory.CreateDirectory(_captureFolder);

            MessageBox.Show("Click on a window within this application to capture it.");
            _hookId = SetHook(_proc);
        }

        #region Mouse Hook

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_MOUSE_LL, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                POINT pt = hookStruct.pt;

                IntPtr hWnd = WindowFromPoint(pt);
                if (hWnd != IntPtr.Zero)
                {
                    // Only capture windows in this process
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == (uint)Process.GetCurrentProcess().Id)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (Window win in Application.Current.Windows)
                            {
                                var helper = new WindowInteropHelper(win);
                                if (helper.Handle == hWnd)
                                {
                                    CaptureWindow(win); // static call
                                    break;
                                }
                            }
                        });
                    }
                }

                // Remove hook after first click
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #endregion

        #region Capture Logic

        private static void CaptureWindow(Window targetWindow)
        {
            if (targetWindow == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var rtb = new RenderTargetBitmap(
                    (int)targetWindow.ActualWidth,
                    (int)targetWindow.ActualHeight,
                    96, 96,
                    System.Windows.Media.PixelFormats.Pbgra32);

                rtb.Render(targetWindow);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                string fileName = $"{targetWindow.Title}_Capture.png";
                string fullPath = System.IO.Path.Combine(_captureFolder, fileName);

                using (var fs = new FileStream(fullPath, FileMode.Create))
                    encoder.Save(fs);

                MessageBox.Show($"Captured {targetWindow.Title} → saved as {fullPath}");
            });
        }

        #endregion

        #region Win32 Interop

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}

