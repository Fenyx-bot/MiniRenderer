using MiniRenderer.Engine;

namespace MiniRenderer
{
    /// <summary>
    /// Module 5: Textures & Materials
    /// 
    /// In this module, we'll learn about:
    /// 1. Loading and Applying Textures to Objects
    /// 2. Introduction to UV Mapping and Texture Coordinates
    /// 3. Applying Textures to Both 2D and 3D Shapes (e.g., a Textured Cube)
    /// 4. Creating a Simple Material System
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 5: Textures & Materials");

            // Create a window
            var window = Window.Create(800, 600, "Module 5 - Textures & Materials");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}