using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// Helper class for creating and configuring the application window
    /// </summary>
    public static class Window
    {
        /// <summary>
        /// Create a new window with the specified dimensions and title
        /// </summary>
        /// <param name="width">Width of the window in pixels</param>
        /// <param name="height">Height of the window in pixels</param>
        /// <param name="title">Window title</param>
        /// <returns>A configured GameWindow instance</returns>
        public static GameWindow Create(int width, int height, string title)
        {
            // Window settings
            GameWindowSettings gameWindowSettings = GameWindowSettings.Default;

            // Native window settings
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings
            {
                Size = new Vector2i(width, height),
                Title = title,
                // This is needed to run on macOS
                Flags = ContextFlags.ForwardCompatible
            };

            // Create the window
            var window = new GameWindow(gameWindowSettings, nativeWindowSettings);

            // Center the window on the screen
            window.CenterWindow();

            return window;
        }
    }
}