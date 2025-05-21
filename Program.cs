using MiniRenderer.Engine;

namespace MiniRenderer
{
    /// <summary>
    /// Module 4: Expanding to 3D Rendering
    /// 
    /// In this module, we'll learn about:
    /// 1. Introduction to 3D space (Adding a Z-Axis)
    /// 2. Rendering a 3D Cube (Wireframe & Solid)
    /// 3. Switching Between Orthographic and Perspective Projection
    /// 4. Applying Transformations in 3D (Translation, Rotation, Scaling)
    /// 5. Understanding the Model-View-Projection (MVP) Matrix
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Module 4: Expanding to 3D Rendering");

            // Create a window
            var window = Window.Create(800, 600, "Module 4 - 3D Rendering");

            // Create and run the engine
            using (var engine = new Engine.Engine(window))
            {
                engine.Run();
            }
        }
    }
}