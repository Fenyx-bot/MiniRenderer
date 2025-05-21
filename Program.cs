using MiniRenderer.Engine;

namespace MiniRenderer
{
    // Module 3: 2D Rendering Basics
    // In this module, we'll learn how to:
    // 1. Understand the 2D coordinate system
    // 2. Draw 2D shapes with vertex indices
    // 3. Work with textures and UV coordinates
    // 4. Apply transformations (translation, rotation, scaling)
    // 5. Implement a simple 2D camera

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 3: 2D Rendering Basics");

            // Create a window
            var window = Window.Create(800, 600, "Module 3 - 2D Rendering");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}