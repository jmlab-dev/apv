using System.Runtime.InteropServices;

namespace AdrenalinProfileViewer.UI;

internal static class NativeThemeHelper
{
    private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int PreferredAppModeAllowDark = 1;
    private static readonly object Sync = new();
    private static bool _initialized;
    private static AllowDarkModeForWindowDelegate? _allowDarkModeForWindow;

    public static void InitializeApplication()
    {
        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            try
            {
                var module = LoadLibrary("uxtheme.dll");
                if (module == IntPtr.Zero)
                {
                    return;
                }

                // These exports are used by modern Windows 10/11 to let native
                // controls (especially scrollbars) participate in dark mode.
                var preferredModeAddress = GetProcAddress(module, (IntPtr)135);
                if (preferredModeAddress != IntPtr.Zero)
                {
                    var setPreferredMode = Marshal.GetDelegateForFunctionPointer<SetPreferredAppModeDelegate>(preferredModeAddress);
                    _ = setPreferredMode(PreferredAppModeAllowDark);
                }

                var allowWindowAddress = GetProcAddress(module, (IntPtr)133);
                if (allowWindowAddress != IntPtr.Zero)
                {
                    _allowDarkModeForWindow = Marshal.GetDelegateForFunctionPointer<AllowDarkModeForWindowDelegate>(allowWindowAddress);
                }
            }
            catch
            {
                // The managed palette remains usable on Windows builds that do
                // not expose the optional dark-mode helpers.
            }
        }
    }

    public static void Apply(Control root, bool dark)
    {
        InitializeApplication();

        if (root.IsHandleCreated)
        {
            ApplyOne(root, dark);
        }

        foreach (Control child in root.Controls)
        {
            Apply(child, dark);
        }
    }

    private static void ApplyOne(Control control, bool dark)
    {
        try
        {
            _allowDarkModeForWindow?.Invoke(control.Handle, dark);
            var themeName = dark ? "DarkMode_Explorer" : "Explorer";
            _ = SetWindowTheme(control.Handle, themeName, null);

            if (control is Form)
            {
                var enabled = dark ? 1 : 0;
                if (DwmSetWindowAttribute(
                        control.Handle,
                        DwmUseImmersiveDarkMode,
                        ref enabled,
                        Marshal.SizeOf<int>()) != 0)
                {
                    _ = DwmSetWindowAttribute(
                        control.Handle,
                        DwmUseImmersiveDarkModeBefore20H1,
                        ref enabled,
                        Marshal.SizeOf<int>());
                }
            }
        }
        catch
        {
            // Theme APIs vary slightly across Windows builds. The managed palette
            // remains fully functional even when a native theme call is unavailable.
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int SetPreferredAppModeDelegate(int appMode);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool AllowDarkModeForWindowDelegate(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool allow);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procName);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
