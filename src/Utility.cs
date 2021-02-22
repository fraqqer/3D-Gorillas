using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace Gorillas3D.Utility
{
    public static class Utility
    {
        /// <summary>
        /// Allows game to be frame-independent across all displays.
        /// </summary>
        public static void DeltaTime(ref Stopwatch pStopwatch, ref long pCurrentTime, ref long pPreviousTime, ref float pDeltaTime)
        {
            pStopwatch.Start();
            pCurrentTime = pStopwatch.ElapsedMilliseconds;
            pDeltaTime = (pCurrentTime - pPreviousTime) / 100f;

            // game capped at 60fps (1/60 = 0.16666 recurring)
            if (pDeltaTime > 0.166666667f)
                pDeltaTime = 0.166666667f;

            pPreviousTime = pCurrentTime;
            pCurrentTime = 0;
        }

        /// <summary>
        /// Loads a texture into the OpenGL scene.
        /// </summary>
        /// <param name="pTextureLocation">The index where the texture handle is stored. **Must always be an index of the mTexture_IDs array.**</param>
        /// <param name="pFilePath">The file path for the texture image.</param>
        public static void LoadTexture(int pTextureLocation, in string pFilePath)
        {
            GL.BindTexture(TextureTarget.Texture2D, pTextureLocation);

            using (Bitmap image = new Bitmap(pFilePath))
            {
                BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, imageData.Scan0);
                image.UnlockBits(imageData);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
        }

        /// <summary>
        /// Loads a cubemap into the current OpenGL scene.
        /// </summary>
        /// <param name="pTextureLocation">Must always be an index of the mTexture_IDs array.</param>
        /// <param name="pFilePath">The file path for the texture image.</param>
        public static void LoadCubemap(int pTextureLocation, in string[] pFilePath)
        {
            GL.BindTexture(TextureTarget.TextureCubeMap, pTextureLocation);

            for (int index = 0; index < pFilePath.Length; index++)
            {
                using (Bitmap image = new Bitmap(pFilePath[index]))
                {
                    BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    // (..., PositiveX, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ, ...) (thanks LearnOpenGL.com)
                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + index, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, imageData.Scan0);
                    image.UnlockBits(imageData);
                }
            }

            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        }
    }

    enum LightType
    {
        DIRECTIONAL,
        SPOTLIGHT,
        POINT
    }

    public enum GameState
    {
        NaN,
        P1_TURN,
        P2_TURN,
        BANANA_IN_MOTION,
        GAME_OVER
    }
}
