using System.Runtime.InteropServices;

namespace TimeDoctorAlert.Platform.Mac;

public class MacWindowEnumerator : IWindowEnumerator
{
    private const int kCGWindowListOptionOnScreenOnly = 1 << 0;
    private const int kCGWindowListExcludeDesktopElements = 1 << 4;
    private const uint kCGNullWindowID = 0;

    private const int kCFStringEncodingUTF8 = 0x08000100;

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGWindowListCopyWindowInfo(int option, uint relativeToWindow);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern int CFArrayGetCount(IntPtr theArray);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, int idx);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, int encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFStringGetCString(IntPtr theString, IntPtr buffer, int bufferSize, int encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFNumberGetValue(IntPtr number, int theType, out int value);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern bool CFNumberGetValue(IntPtr number, int theType, out double value);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, out CGRect rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public double X, Y, Width, Height;
    }

    // CFNumber types
    private const int kCFNumberSInt32Type = 3;

    public List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();

        var windowList = CGWindowListCopyWindowInfo(
            kCGWindowListOptionOnScreenOnly | kCGWindowListExcludeDesktopElements,
            kCGNullWindowID);

        if (windowList == IntPtr.Zero)
            return windows;

        try
        {
            var count = CFArrayGetCount(windowList);

            var keyOwnerName = CreateCFString("kCGWindowOwnerName");
            var keyWindowName = CreateCFString("kCGWindowName");
            var keyWindowNumber = CreateCFString("kCGWindowNumber");
            var keyBounds = CreateCFString("kCGWindowBounds");

            try
            {
                for (int i = 0; i < count; i++)
                {
                    var dict = CFArrayGetValueAtIndex(windowList, i);

                    var ownerName = GetStringValue(dict, keyOwnerName);
                    var windowName = GetStringValue(dict, keyWindowName);
                    var windowNumber = GetIntValue(dict, keyWindowNumber);

                    var boundsDict = CFDictionaryGetValue(dict, keyBounds);
                    int width = 0, height = 0;
                    if (boundsDict != IntPtr.Zero &&
                        CGRectMakeWithDictionaryRepresentation(boundsDict, out var rect))
                    {
                        width = (int)rect.Width;
                        height = (int)rect.Height;
                    }

                    if (string.IsNullOrEmpty(ownerName))
                        continue;

                    windows.Add(new WindowInfo
                    {
                        Id = windowNumber.ToString(),
                        Title = windowName ?? "",
                        ProcessName = ownerName,
                        Width = width,
                        Height = height,
                        IsForeground = false // Determining foreground requires AppKit interop, not critical for filtering
                    });
                }
            }
            finally
            {
                CFRelease(keyOwnerName);
                CFRelease(keyWindowName);
                CFRelease(keyWindowNumber);
                CFRelease(keyBounds);
            }
        }
        finally
        {
            CFRelease(windowList);
        }

        return windows;
    }

    private IntPtr CreateCFString(string s) =>
        CFStringCreateWithCString(IntPtr.Zero, s, kCFStringEncodingUTF8);

    private string? GetStringValue(IntPtr dict, IntPtr key)
    {
        var value = CFDictionaryGetValue(dict, key);
        if (value == IntPtr.Zero)
            return null;

        var buffer = Marshal.AllocHGlobal(1024);
        try
        {
            if (CFStringGetCString(value, buffer, 1024, kCFStringEncodingUTF8))
                return Marshal.PtrToStringUTF8(buffer);
            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private int GetIntValue(IntPtr dict, IntPtr key)
    {
        var value = CFDictionaryGetValue(dict, key);
        if (value == IntPtr.Zero)
            return 0;

        CFNumberGetValue(value, kCFNumberSInt32Type, out int result);
        return result;
    }
}
