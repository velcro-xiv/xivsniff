using System.Diagnostics;

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

    /// <summary>
    /// Returns the path to the executable of the provided process object.
    /// </summary>
    /// <param name="proc">The process object.</param>
    /// <returns>The path to the executable of the provided process object.</returns>
    /// <exception cref="InvalidOperationException">The function failed to retrieve the game path from the provided process object.</exception>
    public static string GetGamePathFromProcess(Process proc)
    {
        var fileName = proc.MainModule?.FileName;
        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("Failed to retrieve game path from instance");
        }

        return fileName;
    }
}