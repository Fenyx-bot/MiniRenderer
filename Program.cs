using MiniRenderer.Engine;

namespace MiniRenderer
{
    /// <summary>
    /// Module 6: Loading 3D Models
    /// 
    /// In this module, we'll learn about:
    /// 1. Understanding 3D Model Formats (Focus on OBJ Files)
    /// 2. Writing a Basic OBJ File Loader
    /// 3. Parsing and Rendering a Loaded 3D Model
    /// 4. Applying Textures and Materials to 3D Models
    /// 5. Optimizing Model Loading for Performance
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 6: Loading 3D Models");

            // Create a window
            var window = Window.Create(800, 600, "Module 6 - Loading 3D Models");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}