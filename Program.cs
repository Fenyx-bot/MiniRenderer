using MiniRenderer.Engine;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace MiniRenderer
{
    // Module 1: Getting Started with OpenTK
    // In this module, we'll learn how to:
    // 1. Create a window and handle events
    // 2. Set up a basic OpenGL context
    // 3. Create and use Vertex Buffer Objects (VBOs) and Vertex Array Objects (VAOs)
    // 4. Draw a simple triangle using static colors

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 1: Getting Started with OpenTK");

            // Create a window
            var window = Engine.Window.Create(800, 600, "Module 1 - Triangle");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}