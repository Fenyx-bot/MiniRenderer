using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Shader program wrapper for OpenGL shaders
    /// </summary>
    public class Shader : IDisposable
    {
        // The OpenGL handle to the shader program
        public int Handle { get; private set; }

        // Cache for uniform locations
        private readonly Dictionary<string, int> _uniformLocations;

        // Flag for resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a new shader from vertex and fragment shader source
        /// </summary>
        /// <param name="vertexSource">Source code for the vertex shader</param>
        /// <param name="fragmentSource">Source code for the fragment shader</param>
        public Shader(string vertexSource, string fragmentSource)
        {
            // Create and compile the vertex shader
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            // Check for compilation errors
            CheckShaderCompilation(vertexShader, "VERTEX");

            // Create and compile the fragment shader
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            // Check for compilation errors
            CheckShaderCompilation(fragmentShader, "FRAGMENT");

            // Create the shader program
            Handle = GL.CreateProgram();

            // Attach the shaders
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            // Link the program
            GL.LinkProgram(Handle);

            // Check for linking errors
            CheckProgramLinking(Handle);

            // Detach and delete the shaders
            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            // Cache all uniform locations
            _uniformLocations = new Dictionary<string, int>();

            // Get the number of active uniforms
            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

            // Loop through all uniforms and cache their locations
            for (int i = 0; i < uniformCount; i++)
            {
                // Get the name of this uniform
                string name = GL.GetActiveUniform(Handle, i, out _, out _);

                // Get the location of the uniform
                int location = GL.GetUniformLocation(Handle, name);

                // Store it in the dictionary
                _uniformLocations[name] = location;
            }
        }

        /// <summary>
        /// Create a shader from vertex and fragment shader files
        /// </summary>
        /// <param name="vertexShaderPath">Path to the vertex shader file</param>
        /// <param name="fragmentShaderPath">Path to the fragment shader file</param>
        /// <returns>A new shader instance</returns>
        public static Shader FromFiles(string vertexShaderPath, string fragmentShaderPath)
        {
            // Read the shader source from files
            string vertexSource = File.ReadAllText(vertexShaderPath);
            string fragmentSource = File.ReadAllText(fragmentShaderPath);

            // Create a new shader
            return new Shader(vertexSource, fragmentSource);
        }

        /// <summary>
        /// Check if a shader compiled successfully
        /// </summary>
        /// <param name="shader">The shader to check</param>
        /// <param name="type">The type of shader (for error messages)</param>
        private void CheckShaderCompilation(int shader, string type)
        {
            // Get the compilation status
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);

            // If compilation failed
            if (success == 0)
            {
                // Get the info log
                string infoLog = GL.GetShaderInfoLog(shader);

                // Output error message
                Console.WriteLine($"ERROR::SHADER::{type}::COMPILATION_FAILED\n{infoLog}");

                // Throw exception to stop execution
                throw new Exception($"Shader compilation error: {infoLog}");
            }
        }

        /// <summary>
        /// Check if a shader program linked successfully
        /// </summary>
        /// <param name="program">The program to check</param>
        private void CheckProgramLinking(int program)
        {
            // Get the linking status
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);

            // If linking failed
            if (success == 0)
            {
                // Get the info log
                string infoLog = GL.GetProgramInfoLog(program);

                // Output error message
                Console.WriteLine($"ERROR::PROGRAM::LINKING_FAILED\n{infoLog}");

                // Throw exception to stop execution
                throw new Exception($"Shader program linking error: {infoLog}");
            }
        }

        /// <summary>
        /// Use this shader program
        /// </summary>
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        /// <summary>
        /// Find the location of a uniform
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <returns>The location of the uniform, or -1 if not found</returns>
        public int GetUniformLocation(string name)
        {
            // Check if we've already cached this uniform location
            if (_uniformLocations.TryGetValue(name, out int location))
            {
                return location;
            }

            // Otherwise, get the location from OpenGL
            location = GL.GetUniformLocation(Handle, name);

            // If the uniform was not found or not used
            if (location == -1)
            {
                Console.WriteLine($"Warning: Uniform '{name}' not found or not active");
            }
            else
            {
                // Cache it for next time
                _uniformLocations[name] = location;
            }

            return location;
        }

        #region Uniform Setters

        /// <summary>
        /// Set a boolean uniform
        /// </summary>
        public void SetBool(string name, bool value)
        {
            Use();
            GL.Uniform1(GetUniformLocation(name), value ? 1 : 0);
        }

        /// <summary>
        /// Set an integer uniform
        /// </summary>
        public void SetInt(string name, int value)
        {
            Use();
            GL.Uniform1(GetUniformLocation(name), value);
        }

        /// <summary>
        /// Set a float uniform
        /// </summary>
        public void SetFloat(string name, float value)
        {
            Use();
            GL.Uniform1(GetUniformLocation(name), value);
        }

        /// <summary>
        /// Set a Vector2 uniform
        /// </summary>
        public void SetVector2(string name, Vector2 value)
        {
            Use();
            GL.Uniform2(GetUniformLocation(name), value);
        }

        /// <summary>
        /// Set a Vector3 uniform
        /// </summary>
        public void SetVector3(string name, Vector3 value)
        {
            Use();
            GL.Uniform3(GetUniformLocation(name), value);
        }

        /// <summary>
        /// Set a Vector4 uniform
        /// </summary>
        public void SetVector4(string name, Vector4 value)
        {
            Use();
            GL.Uniform4(GetUniformLocation(name), value);
        }

        /// <summary>
        /// Set a Matrix4 uniform
        /// </summary>
        public void SetMatrix4(string name, Matrix4 value)
        {
            Use();
            GL.UniformMatrix4(GetUniformLocation(name), false, ref value);
        }

        #endregion

        /// <summary>
        /// Dispose of the shader program resource
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteProgram(Handle);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer to warn if shader wasn't properly disposed
        /// </summary>
        ~Shader()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: Shader was not disposed! Call Dispose() to prevent resource leaks.");
            }
        }
    }
}