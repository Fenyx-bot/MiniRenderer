using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.IO;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Wrapper class for OpenGL textures
    /// </summary>
    public class Texture : IDisposable
    {
        // The OpenGL handle to the texture
        public int Handle { get; private set; }

        // Texture dimensions
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Flag for resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Load a texture from a file
        /// </summary>
        /// <param name="path">Path to the image file</param>
        public Texture(string path)
        {
            // Generate a texture handle
            Handle = GL.GenTexture();

            // Bind the texture
            Bind();

            // Set texture wrapping parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Set texture filtering parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Tell StbImageSharp to flip the image when loading (OpenGL expects the origin to be bottom-left)
            StbImage.stbi_set_flip_vertically_on_load(1);

            try
            {
                // Load the image
                using (Stream stream = File.OpenRead(path))
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    Width = image.Width;
                    Height = image.Height;

                    // Upload the image data to the texture
                    GL.TexImage2D(
                        TextureTarget.Texture2D,    // Texture target
                        0,                          // Mipmap level
                        PixelInternalFormat.Rgba,   // Internal format
                        image.Width,                // Width
                        image.Height,               // Height
                        0,                          // Border (must be 0)
                        PixelFormat.Rgba,           // Format
                        PixelType.UnsignedByte,     // Type
                        image.Data                  // Data
                    );

                    // Generate mipmaps
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                }

                Console.WriteLine($"Loaded texture: {path} ({Width}x{Height})");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading texture '{path}': {e.Message}");

                // Create a placeholder texture
                CreatePlaceholderTexture();
            }
        }

        /// <summary>
        /// Create a placeholder texture (checkerboard pattern)
        /// </summary>
        private void CreatePlaceholderTexture()
        {
            // Create a simple 16x16 checkerboard pattern
            Width = 16;
            Height = 16;
            byte[] data = new byte[Width * Height * 4];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // Each pixel has 4 bytes (RGBA)
                    int index = (y * Width + x) * 4;

                    // Create a checkerboard pattern
                    bool isBlack = (x / 4 + y / 4) % 2 == 0;

                    // Set color components
                    if (isBlack)
                    {
                        data[index + 0] = 0;    // R
                        data[index + 1] = 0;    // G
                        data[index + 2] = 0;    // B
                        data[index + 3] = 255;  // A
                    }
                    else
                    {
                        data[index + 0] = 255;  // R
                        data[index + 1] = 0;    // G
                        data[index + 2] = 255;  // B (magenta for visibility)
                        data[index + 3] = 255;  // A
                    }
                }
            }

            // Upload the placeholder data
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                Width,
                Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                data
            );

            // Generate mipmaps
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            Console.WriteLine("Created placeholder texture (checkerboard pattern)");
        }

        /// <summary>
        /// Create a colored texture
        /// </summary>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <param name="color">Color as RGBA bytes</param>
        public Texture(int width, int height, byte[] color)
        {
            if (color.Length != 4)
            {
                throw new ArgumentException("Color must be 4 bytes (RGBA)", nameof(color));
            }

            // Generate a texture handle
            Handle = GL.GenTexture();

            // Bind the texture
            Bind();

            // Set texture wrapping parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            // Set texture filtering parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Set dimensions
            Width = width;
            Height = height;

            // Create a solid color texture
            byte[] data = new byte[width * height * 4];

            for (int i = 0; i < width * height; i++)
            {
                data[i * 4 + 0] = color[0]; // R
                data[i * 4 + 1] = color[1]; // G
                data[i * 4 + 2] = color[2]; // B
                data[i * 4 + 3] = color[3]; // A
            }

            // Upload the texture data
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                width,
                height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                data
            );

            // Generate mipmaps
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>
        /// Bind the texture to a texture unit
        /// </summary>
        /// <param name="unit">Texture unit to bind to</param>
        public void Use(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            Bind();
        }

        /// <summary>
        /// Bind this texture to make it the current texture for subsequent operations
        /// </summary>
        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        /// <summary>
        /// Unbind this texture (bind texture 0)
        /// </summary>
        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Dispose of the texture resource
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteTexture(Handle);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer to warn if texture wasn't properly disposed
        /// </summary>
        ~Texture()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: Texture was not disposed! Call Dispose() to prevent resource leaks.");
            }
        }

        /// <summary>
        /// Create a white 1x1 texture for use when no texture is needed
        /// </summary>
        /// <returns>A white texture</returns>
        public static Texture CreateWhiteTexture()
        {
            return new Texture(1, 1, new byte[] { 255, 255, 255, 255 });
        }
    }
}