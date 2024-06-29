namespace XIVSniff;

public class FFXIV
{
    /// <summary>
    /// Returns the main window handle of a current FFXIV process.
    /// </summary>
    /// <returns>A pointer to the main window of a current FFXIV process.</returns>
    public static IntPtr GetMainWindowHandle()
    {
        return User32.FindWindow("FFXIVGAME", null);
    }

    /// <summary>
    /// Returns the process ID of the main window of a current FFXIV process.
    /// </summary>
    /// <returns>The process ID of the main window of a current FFXIV process.</returns>
    public static uint GetMainWindowProcessId()
    {
        var window = GetMainWindowHandle();

        // Returns the ID of the thread that created the window, which we don't care about
        _ = User32.GetWindowThreadProcessId(window, out var pid);
        return pid;
    }
}