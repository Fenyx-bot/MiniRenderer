using MiniRenderer.Engine;

namespace MiniRenderer
{
    // Module 2: Understanding the Graphics Pipeline
    // In this module, we'll learn how to:
    // 1. Understand the rendering pipeline
    // 2. Create and use GLSL shaders (vertex and fragment)
    // 3. Work with shader uniforms
    // 4. Create more advanced visual effects with shaders

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 2: Understanding the Graphics Pipeline");

            // Create a window
            var window = Window.Create(800, 600, "Module 2 - Shaders");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}