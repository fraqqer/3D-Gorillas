using Gorillas3D.Collision;
using Gorillas3D.Components;
using Gorillas3D.Objects;
using Gorillas3D.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Game
{
    public class Window : GameWindow
    {
        #region Ignore
        public Window() : base(1280, 720, GraphicsMode.Default, "3D Gorillas", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.ForwardCompatible) { }
        #endregion

        #region Variable Declarations
        public static List<Cube> cubePositions = new List<Cube>();
        public static List<Cube> UtilityCubes = new List<Cube>();
        public static Cube bananaCube, playerOneCube, playerTwoCube;
        private Thread getInputThread;
        private Thread turnTimeCheckThread;
        bool[] keysPressed = new bool[255];                         // 1 byte * 255 = 255 bytes
        private Stopwatch timeSinceLastFrame = new Stopwatch();
        private Shader phongShader, skyboxShader;
        private Matrix4 projection;                                 // 64 bytes
        private Matrix4 cameraPos = Matrix4.CreateTranslation(0, -14, -3); // 64 bytes
        private Matrix4 groundPos = Matrix4.CreateTranslation(0, 0, -5); // 64 bytes
        private Matrix4 CubePos = Matrix4.CreateTranslation(10, 0, -30); // 64 bytes
        private Matrix4 BananaPos = Matrix4.CreateTranslation(0, 0, 0); // 64 bytes

        private readonly int[] mVBO_IDs = new int[10];              // 4 bytes * 10 = 40 bytes
        private readonly int[] buildingHeight = new int[8];         // 4 bytes * 8 = 32 bytes
        private readonly int[] mVAO_IDs = new int[7];               // 4 bytes * 7 = 28 bytes
        private readonly Vector3[] PlayerPositions = new Vector3[2];      // 12 bytes * 2 = 24 bytes
        private long previousTime, currentTime = 0;                 // 8 bytes * 2 = 16 bytes
        public static int[] mTex_IDs = new int[4];                  // 4 bytes * 4 = 16 bytes
        private Vector3 bananaPos;                                  // 12 bytes
        private readonly int[] mFBO_IDs = new int[2];               // 4 bytes * 2 =  8 bytes

        public static float deltaTime;                                  // 4 bytes
        private readonly float walkingSpeed = 1;                        // 4 bytes
        private readonly float turningSpeed = 0.3f;                     // 4 bytes
        private readonly int buildingWidth = 3;                         // 4 bytes
        private float velocity = -1.0f;                                 // 4 bytes
        private float angle = -1.0f;                                    // 4 bytes
        private float windFactor;                                       // 4 bytes
        private bool buildingHeightsRegistered = false;                 // 1 byte
        private bool bananaRegistered = false;                          // 1 byte
        public static GameState currentGameState = GameState.P1_TURN;   // 1 byte
        public static GameState oldGameState;
        private readonly bool debugEnabled = false;                     // 1 byte
        private bool hasAsked = false;                                  // 1 byte
        private bool utilityCubesRegistered = false;                    // 1 byte
        private bool hasTimeCheckBegun = false;
        private bool inputHasStarted = false;
        #endregion

        protected override void OnLoad(EventArgs e)
        {
            #region No need to change
            base.OnLoad(e);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);
            #endregion

            GL.ClearColor(Color4.SkyBlue);

            #region Generate Building Heights and Wind Factor
            Random building1Rand = new Random();
            Random building2Rand = new Random();
            Random building3Rand = new Random();
            Random building4Rand = new Random();
            Random building5Rand = new Random();
            Random building6Rand = new Random();
            Random building7Rand = new Random();
            Random building8Rand = new Random();
            Random windFactorRand = new Random();

            // think about this carefully, performance issues may start cropping up when putting all buildings at a maximum of 13.
            // this means 39 blocks per building max, so 39 * 8 = 312 blocks to deal with. That's a lot of CPU power to calculate physics for!
            buildingHeight[0] = building1Rand.Next(3, 5);
            buildingHeight[1] = building2Rand.Next(1, 13);
            buildingHeight[2] = building3Rand.Next(4, 6);
            buildingHeight[3] = building4Rand.Next(7, 7);
            buildingHeight[4] = building5Rand.Next(3, 10);
            buildingHeight[5] = building6Rand.Next(5, 6);
            buildingHeight[6] = building7Rand.Next(3, 12);
            buildingHeight[7] = building8Rand.Next(1, 4);
            // formula determines wind multiplier for velocity
            windFactor = windFactorRand.Next(-3, 3);
            #endregion

            #region Calculate Player Position
            // Height is doubled to account for cubes so that they do not clip with one another.
            // Constant '2' is added to account for placing the cubes directly on the roof of the building.
            PlayerPositions[0] = new Vector3(-21, (buildingHeight[1] * 2) + 2, -29);
            PlayerPositions[1] = new Vector3(23, (buildingHeight[6] * 2) + 2, -29);
            #endregion

            #region Shader Code
            phongShader = new Shader();
            skyboxShader = new Shader("res/shaders/skybox.vert", "res/shaders/skybox.frag");
            GL.UseProgram(phongShader.ProgramHandle);
            #endregion

            #region Light Calls
            LightCall(LightType.DIRECTIONAL, new Vector3(0.7f, 0.5f, 0), new Vector3(1, 0.64f, 0), new Vector3(1, 1, 1), new Vector3(-15, 10, -10), new Vector3(3, -30, -50));
            //LightCall(LightType.POINT, new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0, 2, 0));
            //LightCall(LightType.SPOTLIGHT, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1f, 1f, 1f), new Vector3(1, 1, 1), new Vector3(0, 1, -30), new Vector3(0, -90, -1), Math.Cos(MathHelper.RadiansToDegrees(Math.PI)));
            #endregion

            #region Generate VAO/VBO/FBO/Texture Arrays
            GL.GenVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.GenTextures(mTex_IDs.Length, mTex_IDs);
            GL.GenFramebuffers(mFBO_IDs.Length, mFBO_IDs);
            #endregion

            #region Set Textures
            Utility.LoadTexture(mTex_IDs[0], "res/textures/grass.jpg");
            Utility.LoadTexture(mTex_IDs[1], "res/textures/tiles.jpg");
            string[] skyboxFileNames =
{
                @"res/textures/skybox/right.jpg",
                @"res/textures/skybox/left.jpg",
                @"res/textures/skybox/up.jpg",
                @"res/textures/skybox/down.jpg",
                @"res/textures/skybox/back.jpg",
                @"res/textures/skybox/front.jpg"
            };
            Utility.LoadCubemap(mTex_IDs[2], in skyboxFileNames);
            #endregion

            #region Load Ground
            float[] groundVertices = new float[] {-50f, 0, -40, 0, 1, 0, 0, 2,
                                                  -50f, 0, -10, 0, 1, 0, 0, 0,
                                                   50f, 0, -10, 0, 1, 0, 7, 0,
                                                   50f, 0, -40, 0, 1, 0, 7, 2
            };

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(groundVertices.Length * sizeof(float)), groundVertices, BufferUsageHint.StaticDraw);

            // tells vertex shader that the first 3 indices are the world position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            // tells vertex shader that the last 3 indices are the normals
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));
            // tells vertex shader that the last 2 indices are the texture coordinates
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, true, 8 * sizeof(float), 6 * sizeof(float));
            #endregion

            #region Load Cube
            float[] cubeVertices = new float[]
            {
                // front face
                1, 0, 1,  0, 0, 1,  0, 0,
               -1, 0, 1,  0, 0, 1,  1, 0,
               -1, 2, 1,  0, 0, 1,  1, 1,
                1, 2, 1,  0, 0, 1,  0, 1,

                // right face
                1, 0, 1,  1, 0, 0,  0, 0,
                1, 0, -1, 1, 0, 0,  1, 0,
                1, 2, -1, 1, 0, 0,  1, 1,
                1, 2, 1,  1, 0, 0,  0, 1,

                // left face
               -1, 0, 1,  -1, 0, 0, 0, 0,
               -1, 2, -1, -1, 0, 0, 1, 1,
               -1, 0, -1, -1, 0, 0, 1, 0,
               -1, 2, 1,  -1, 0, 0, 0, 1,

                // back face
               -1, 0, -1, 0, 0, -1, 0, 0,
                1, 0, -1, 0, 0, -1, 1, 0,
                1, 2, -1, 0, 0, -1, 1, 1,
               -1, 2, -1, 0, 0, -1, 0, 1,

                // top face
               -1, 2, 1,  0, 1, 0, 0, 0,
                1, 2, 1,  0, 1, 0, 0, 1,
                1, 2, -1, 0, 1, 0, 1, 1,
               -1, 2, -1, 0, 1, 0, 1, 0,

               // bottom face
               -1, 0, 1,  0, -1, 0, 0, 0,
                1, 0, 1,  0, -1, 0, 0, 1,
                1, 0, -1, 0, -1, 0, 1, 1,
               -1, 0, -1, 0, -1, 0, 1, 0,
            };
            byte[] cubeIndices = new byte[]
            {
                // front face
                2, 1, 0,
                0, 3, 2,

                // right face
                4, 5, 6,
                6, 7, 4,

                // left face
                8, 11, 9,
                9, 10, 8,

                // back face
                14, 13, 12,
                12, 15, 14,

                // top face
                16, 17, 18,
                18, 19, 16,
                
                // bottom face
                22, 21, 20,
                20, 23, 22,
            };

            GL.BindVertexArray(mVAO_IDs[1]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[1]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(cubeVertices.Length * sizeof(float)), cubeVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[2]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(cubeIndices.Length * sizeof(byte)), cubeIndices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, true, 8 * sizeof(float), 6 * sizeof(float));
            #endregion

            #region Load Players
            float[] playerVertices =
            {
                // front face
                0,   0,  0,     0, 0, 1,
               .5f,  0,  0,     0, 0, 1,
               .5f, .5f, 0,     0, 0, 1,
                0,  .5f, 0,     0, 0, 1,

                // right face
                .5f,  0,    0,  1, 0, 0,
                .5f,  0,  -.5f, 1, 0, 0,
                .5f, .5f, -.5f, 1, 0, 0,
                .5f, .5f, 0,    1, 0, 0,

                // left face
                0,   0, -.5f,   -1, 0, 0,
                0, .5f, -.5f,   -1, 0, 0,
                0, 0, 0,        -1, 0, 0,
                0, .5f, 0,      -1, 0, 0,

                // back face
                0,   0,  -.5f,   0, 0, -1,
               .5f,  0,  -.5f,   0, 0, -1,
               .5f, .5f, -.5f,   0, 0, -1,
                0,  .5f, -.5f,    0, 0, -1,

                // top face
                0,  .5f, 0,      0, 1, 0,
               .5f, .5f, 0,      0, 1, 0,
               .5f, .5f, -.5f,   0, 1, 0,
                0,  .5f, -.5f,   0, 1, 0,

                // bottom face
                0,  0f, 0,      0, -1, 0,
               .5f, 0f, 0,      0, -1, 0,
               .5f, 0f, -.5f,   0, -1, 0,
                0,  0f, -.5f,   0, -1, 0,
            };

            byte[] playerIndices =
            {
                // front face
                0, 1, 2,
                2, 3, 0,

                // right face
                4, 5, 6,
                6, 7, 4,

                // left face
                8, 10, 11,
                11, 9, 8,

                // back face
                14, 13, 12,
                12, 15, 14,

                // top face
                16, 17, 18,
                18, 19, 16,
                
                // bottom face
                22, 21, 20,
                20, 23, 22
            };

            GL.BindVertexArray(mVAO_IDs[4]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[4]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(playerVertices.Length * sizeof(float)), playerVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[5]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(playerIndices.Length * sizeof(byte)), playerIndices, BufferUsageHint.StaticDraw);
            // Activate positions
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            // Activate normals
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));
            #endregion

            #region Load Banana
            float[] bananaVertices =
{
                // front face
                0,   0,  0,     0, 0, 1,
               .5f,  0,  0,     0, 0, 1,
               .5f, .5f, 0,     0, 0, 1,
                0,  .5f, 0,     0, 0, 1,

                // right face
                .5f,  0,    0,  1, 0, 0,
                .5f,  0,  -.5f, 1, 0, 0,
                .5f, .5f, -.5f, 1, 0, 0,
                .5f, .5f, 0,    1, 0, 0,

                // left face
                0,   0, -.5f,   -1, 0, 0,
                0, .5f, -.5f,   -1, 0, 0,
                0, 0, 0,        -1, 0, 0,
                0, .5f, 0,      -1, 0, 0,

                // back face
                0,   0,  -.5f,   0, 0, -1,
               .5f,  0,  -.5f,   0, 0, -1,
               .5f, .5f, -.5f,   0, 0, -1,
                0,  .5f, -.5f,    0, 0, -1,

                // top face
                0,  .5f, 0,      0, 1, 0,
               .5f, .5f, 0,      0, 1, 0,
               .5f, .5f, -.5f,   0, 1, 0,
                0,  .5f, -.5f,   0, 1, 0,

                // bottom face
                0,  0f, 0,      0, -1, 0,
               .5f, 0f, 0,      0, -1, 0,
               .5f, 0f, -.5f,   0, -1, 0,
                0,  0f, -.5f,   0, -1, 0,
            };
            byte[] bananaIndices =
            {
                // front face
                0, 1, 2,
                2, 3, 0,

                // right face
                4, 5, 6,
                6, 7, 4,

                // left face
                8, 10, 11,
                11, 9, 8,

                // back face
                14, 13, 12,
                12, 15, 14,

                // top face
                16, 17, 18,
                18, 19, 16,
                
                // bottom face
                22, 21, 20,
                20, 23, 22
            };

            GL.BindVertexArray(mVAO_IDs[5]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[6]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(bananaVertices.Length * sizeof(float)), bananaVertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[7]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(bananaIndices.Length * sizeof(byte)), bananaIndices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));
            #endregion

            #region Load Skybox
            // Vertices borrowed from LearnOpenGL.com (https://learnopengl.com/code_viewer.php?code=advanced/cubemaps_skybox_data)
            float[] skyboxVertices = {
                -1.0f,  1.0f, -1.0f,
                -1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f, -1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,

                -1.0f, -1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f,
                -1.0f, -1.0f,  1.0f,

                -1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f, -1.0f,
                 1.0f,  1.0f,  1.0f,
                 1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f,  1.0f,
                -1.0f,  1.0f, -1.0f,

                -1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f, -1.0f,
                 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f,  1.0f,
                 1.0f, -1.0f,  1.0f
            };

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[3]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(skyboxVertices.Length * sizeof(float)), skyboxVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, 3 * sizeof(float), 0);
            #endregion

            GL.BindVertexArray(0);

            #region Shader uniform allocations
            int uViewLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "View");
            GL.UniformMatrix4(uViewLoc, true, ref cameraPos);
            int uSkyboxLoc = GL.GetUniformLocation(skyboxShader.ProgramHandle, "Skybox");
            GL.Uniform1(uSkyboxLoc, 0);
            #endregion

            #region UI
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("////////////////////////////////////////////////////////////////////////////////////////");
            }

            Console.WriteLine("//////          _____  ____    _________  ____  _  _    _    ____  ____           //////");
            Console.WriteLine("//////          \\__  \\/  _ \\  /  __/  _ \\/  __\\/ \\/ \\  / \\  /  _ \\/ ___\\          //////");
            Console.WriteLine("//////            /  || | \\|  | |  | / \\||  \\/|| || |  | |  | / \\||    \\          //////");
            Console.WriteLine("//////           _\\  || |_/|  | |_// \\_/||    /| || |_/\\ |_/\\ |-||\\___ |          //////");
            Console.WriteLine("//////          /____/\\____/  \\____\\____/\\_/\\_\\\\_/\\____|____|_/ \\|\\____/          //////");
            Console.WriteLine("//////                                                                            //////");
            for (int i = 0; i < 3; i++)
            {
                Console.WriteLine("////////////////////////////////////////////////////////////////////////////////////////");
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;

            string direction;

            if (windFactor == 0)
                direction = string.Empty;
            else if (windFactor > 0)
                direction = "Eastwards";
            else
                direction = "Westwards";

            Console.WriteLine();
            Console.WriteLine($"Current Wind Speed: {Math.Abs(windFactor) * 5} mph {direction}");
            Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            #endregion

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);

            if (phongShader != null)
            {
                int uProjectionLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "Projection");
                projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.05f, 100);
                GL.UniformMatrix4(uProjectionLoc, true, ref projection);
            }

            if (skyboxShader != null)
            {
                int uProjectionLoc = GL.GetUniformLocation(skyboxShader.ProgramHandle, "Projection");
                projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 100);
                GL.UniformMatrix4(uProjectionLoc, true, ref projection);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            #region No need to change
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            #endregion

            // Create general atmosphere for the environment. (i.e. sunset-esque)
            MaterialCall(new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 1, 1), 32);
            int uModelLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "Model");

            #region Draw Skybox
            GL.DepthMask(false);
            GL.UseProgram(skyboxShader.ProgramHandle);

            int uSkyboxViewLoc = GL.GetUniformLocation(skyboxShader.ProgramHandle, "View");

            Matrix4 viewMatrix = cameraPos.ClearTranslation();
            viewMatrix.Invert();
            GL.UniformMatrix4(uSkyboxViewLoc, true, ref viewMatrix);

            int uProjectionLoc = GL.GetUniformLocation(skyboxShader.ProgramHandle, "Projection");
            GL.UniformMatrix4(uProjectionLoc, true, ref projection);

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.BindTexture(TextureTarget.TextureCubeMap, mTex_IDs[2]);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);
            GL.DepthMask(true);
            #endregion


            // Call the shader to draw geometry instead of skybox.
            GL.UseProgram(phongShader.ProgramHandle);

            #region Draw Ground
            GL.UniformMatrix4(uModelLoc, true, ref groundPos);
            GL.BindVertexArray(mVAO_IDs[0]);
            GL.BindTexture(TextureTarget.Texture2D, mTex_IDs[0]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            #endregion

            #region Draw Buildings
            GL.BindVertexArray(mVAO_IDs[1]);
            GL.BindTexture(TextureTarget.Texture2D, mTex_IDs[1]);
            MaterialCall(new Vector3(1f, 1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(1, 1, 1), 16);

            // Draw cubes over pre-defined areas
            for (uint index = 0; index < buildingHeight.Length; index++)
            {
                GenerateBuilding(index, buildingHeight[index]);
            }
            buildingHeightsRegistered = true;
            #endregion

            #region Draw Players
            DrawPlayers(uModelLoc);
            #endregion

            #region Draw Banana
            GL.BindVertexArray(mVAO_IDs[5]);
            MaterialCall(new Vector3(1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 0), 32);
            bananaPos = BananaPos.ExtractTranslation();

            if (!bananaRegistered)
            {
                bananaCube = new Cube(ref bananaPos, "Banana Cube")
                {
                    Force = new Vector3(0, 0, 0)
                };
                cubePositions.Add(bananaCube);
                bananaRegistered = true;
            }

            Matrix4 utilCubeMatrix = Matrix4.CreateTranslation(bananaCube.position);
            GL.UniformMatrix4(uModelLoc, true, ref utilCubeMatrix);
            bananaCube.UpdatePhysics();
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedByte, 0);
            #endregion

            KeyInput();
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            Collision.UpdateCollisions();

            if (currentGameState == GameState.BANANA_IN_MOTION)
            {
                if (bananaCube.Force == Vector3.Zero)
                    bananaCube.Force = new Vector3(0, bananaCube.Gravity - 2.0f, 0);
                else
                    bananaCube.Force = new Vector3(bananaCube.Force.X, bananaCube.Force.Y - 0.1f, bananaCube.Force.Z);

                if (!hasTimeCheckBegun)
                {
                    turnTimeCheckThread = new Thread(() => TurnTimeCheck());
                    turnTimeCheckThread.Start();
                    turnTimeCheckThread.Name = "TurnTimeCheck()";
                    hasTimeCheckBegun = true;
                }
            }

            if (currentGameState == GameState.P1_TURN || currentGameState == GameState.P2_TURN && !hasAsked)
            {
                GameState playerTurn = currentGameState;

                if (!inputHasStarted)
                {
                    getInputThread = new Thread(() => GetInput());
                    getInputThread.Name = "GetInput()";
                    Thread.Sleep(500);
                    getInputThread.Start();
                    inputHasStarted = true;
                }

                hasAsked = true;

                if (playerTurn == GameState.P1_TURN && currentGameState == GameState.BANANA_IN_MOTION)
                {
                    currentGameState = GameState.P2_TURN;
                }

                else if (playerTurn == GameState.P2_TURN && currentGameState == GameState.BANANA_IN_MOTION)
                {
                    currentGameState = GameState.P1_TURN;
                }

            }

            Utility.DeltaTime(ref timeSinceLastFrame, ref currentTime, ref previousTime, ref deltaTime);
        }

        /// <summary>
        /// Event that triggers when a key is pushed.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            keysPressed[(char)e.Key] = true;
        }

        /// <summary>
        /// Event that triggers when a key is released. Also contains game information.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            keysPressed[(char)e.Key] = false;
        }

        /// <summary>
        /// Event that triggers when the window is closed. Unloads current geometry and textures from memory.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            GL.DeleteTextures(mTex_IDs.Length, mTex_IDs);
            phongShader.Delete();
            skyboxShader.Delete();
            timeSinceLastFrame.Stop();
            Environment.Exit(0);
        }

        /// <summary>
        /// Handles input and applies to camera.
        /// </summary>
        private void KeyInput()
        {
            if (debugEnabled)
                CameraControls();
        }

        /// <summary>
        /// Generates a set of buildings in the 3D environment using cubes.
        /// </summary>
        /// <param name="index">The current building that the user wishes to be built.</param>
        /// <param name="buildingHeight">The height that has been randomly generated for the current building.</param>
        private void GenerateBuilding(uint index, int buildingHeight)
        {
            for (uint currentBuildingHeight = 0; currentBuildingHeight <= buildingHeight; currentBuildingHeight++)
            {
                for (uint currentBuildingWidth = 0; currentBuildingWidth <= buildingWidth; currentBuildingWidth++)
                {
                    // Multiplied to ensure the cubes do not clip into each other, change reverted at the end to ensure iteration can continue properly.
                    currentBuildingWidth *= 2;
                    int firstBuildingPosX = -33;

                    // Multiplies the first building's starting position's (-33, 0, -30) X component by (9 * index), so we can get the starting position of each current building.
                    // 'currentBuildingWidth' is set to 3, meaning that each building will get 3 cubes.
                    // 'currentBuildingHeight' is multiplied by 2 to prevent any clipping of the cubes.
                    Vector3 cubePosition = new Vector3((firstBuildingPosX + (9 * index)) + currentBuildingWidth, currentBuildingHeight * 2, -30);

                    // If the cubes have already been drawn for the first time, do not add them to the initialisation list.
                    if (!buildingHeightsRegistered)
                    {
                        Cube cube = new Cube(ref cubePosition);
                        cubePositions.Add(cube);
                    }

                    for (int cubeIndex = 0; cubeIndex < cubePositions.Count - 3; cubeIndex++)
                    {
                        CubePos = Matrix4.CreateTranslation(cubePositions[cubeIndex].position);
                        GL.UniformMatrix4(0, true, ref CubePos);
                        cubePositions[cubeIndex].UpdatePhysics();
                        GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedByte, 0);
                    }

                    currentBuildingWidth /= 2;
                }
            }
        }

        /// <summary>
        /// Draws the players at the specified position on the buildings.
        /// </summary>
        /// <param name="pUModelLoc">Uniform variable needed from the MVP matrix in order to communicate with the shader and draw the objects.</param>
        private void DrawPlayers(int pUModelLoc)
        {
            // Create cube instances for players.
            if (!utilityCubesRegistered)
            {
                Vector3 playerOnePosition = PlayerPositions[0];
                playerOneCube = new Cube(ref playerOnePosition, "Player One");
                cubePositions.Add(playerOneCube);

                Vector3 playerTwoPosition = PlayerPositions[1];
                playerTwoCube = new Cube(ref playerTwoPosition, "Player Two");
                cubePositions.Add(playerTwoCube);
            }

            Matrix4 P1Position = Matrix4.CreateScale(2.5f);
            P1Position *= Matrix4.CreateTranslation(playerOneCube.position);
            GL.UniformMatrix4(0, true, ref P1Position);
            playerOneCube.UpdatePhysics();
            GL.BindVertexArray(mVAO_IDs[4]);
            MaterialCall(new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), 32);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedByte, 0);

            Matrix4 P2Position = Matrix4.CreateScale(2.5f);
            P2Position *= Matrix4.CreateTranslation(playerTwoCube.position);
            GL.UniformMatrix4(pUModelLoc, true, ref P2Position);
            playerTwoCube.UpdatePhysics();
            GL.BindVertexArray(mVAO_IDs[4]);
            MaterialCall(new Vector3(0, 1, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 1), 32);
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedByte, 0);

            utilityCubesRegistered = true;
        }

        private void CameraControls()
        {
            float ySpeed = 1f;

            if (keysPressed[(char)Key.W] || keysPressed[(char)Key.Up])
                cameraPos *= Matrix4.CreateTranslation(0.0f, 0.0f, walkingSpeed * deltaTime);

            if (keysPressed[(char)Key.A] || keysPressed[(char)Key.Left])
                cameraPos *= Matrix4.CreateRotationY(-turningSpeed * deltaTime);

            if (keysPressed[(char)Key.S] || keysPressed[(char)Key.Down])
                cameraPos *= Matrix4.CreateTranslation(0.0f, 0.0f, -walkingSpeed * deltaTime);

            if (keysPressed[(char)Key.D] || keysPressed[(char)Key.Right])
                cameraPos *= Matrix4.CreateRotationY(turningSpeed * deltaTime);

            if (keysPressed[(char)Key.ControlLeft])
                cameraPos *= Matrix4.CreateTranslation(0, ySpeed * deltaTime, 0);

            if (keysPressed[(char)Key.Space])
                cameraPos *= (Matrix4.CreateTranslation(0, -ySpeed * deltaTime, 0));

            int uViewLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "View");
            GL.UniformMatrix4(uViewLoc, true, ref cameraPos);
        }

        /// <summary>
        /// To be called before an object in OnRenderFrame()
        /// </summary>
        /// <param name="ambience"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="shininess"></param>
        private void MaterialCall(Vector3 ambience, Vector3 diffuse, Vector3 specular, float shininess)
        {
            int uMatAmbienceLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "material.ambient");
            GL.Uniform3(uMatAmbienceLoc, ref ambience);

            int uMatDiffLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "material.diffuse");
            GL.Uniform3(uMatDiffLoc, ref diffuse);

            int uMatSpecLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "material.specular");
            GL.Uniform3(uMatSpecLoc, ref specular);

            int uMatShinyLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "material.shininess");
            GL.Uniform1(uMatShinyLoc, shininess);
        }

        /// <summary>
        /// A light handler which gives various options for lighting types such as positional lighting, directional lighting, and spotlighting.
        /// </summary>
        /// <param name="lightType">Enum which specifies which lighting type is requested.</param>
        /// <param name="ambience">Normalised RGB values required in the Vector3.</param>
        /// <param name="diffuse">Normalised RGB values required in the Vector3.</param>
        /// <param name="specular">Normalised RGB values required in the Vector3.</param>
        /// <param name="position">Optional parameter for positional lighting. Specifies the position at where the positional lighting is.</param>
        /// <param name="direction">Optional parameter for spotlighting/directional lighting. Specifies the direction IN DEGREES where the light should go.</param>
        /// <param name="spotlightCutOff">Optional parameter for spotlighting. Specifies the cutoff angle for the spotlight.</param>
        /// <param name="spotlightOuterCutOff">Optional parameter for spotlighting. Specifies the outer cutoff angle for the spotlight.</param>
        private void LightCall(LightType lightType, Vector3 ambience, Vector3 diffuse, Vector3 specular, Vector3 position = new Vector3(), Vector3 direction = new Vector3())
        {
            if (lightType == LightType.POINT)
            {
                int uPointLightEnable = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLightRequested");
                GL.Uniform1(uPointLightEnable, 1);

                int uLightPositionLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.position");
                GL.Uniform3(uLightPositionLoc, ref position);

                int uLightAmbienceLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.ambient");
                GL.Uniform3(uLightAmbienceLoc, ref ambience);

                int uLightDiffLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.diffuse");
                GL.Uniform3(uLightDiffLoc, ref diffuse);

                int uLightSpecLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.specular");
                GL.Uniform3(uLightSpecLoc, ref specular);

                int uLightConstantLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.constant");
                GL.Uniform1(uLightConstantLoc, 1.0f);

                int uLightLinearLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.linear");
                GL.Uniform1(uLightLinearLoc, 0.045f);

                int uLightQuadraticLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "pointLight.quadratic");
                GL.Uniform1(uLightQuadraticLoc, 0.0075f);
            }
            else if (lightType == LightType.DIRECTIONAL)
            {
                int uDirLightLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "dirLightRequested");
                GL.Uniform1(uDirLightLoc, 1);

                int uLightDirectionLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "directionalLight.direction");
                GL.Uniform3(uLightDirectionLoc, ref direction);

                int uLightAmbienceLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "directionalLight.ambient");
                GL.Uniform3(uLightAmbienceLoc, ref ambience);

                int uLightDiffLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "directionalLight.diffuse");
                GL.Uniform3(uLightDiffLoc, ref diffuse);

                int uLightSpecLoc = GL.GetUniformLocation(phongShader.ProgramHandle, "directionalLight.specular");
                GL.Uniform3(uLightSpecLoc, ref specular);
            }
        }

        private void GetInput()
        {
            AskPlayerVelocity();

            // Validates velocity input, cannot be less than 0 or greater than 90
            while (velocity < 0 || velocity > 90)
            {
                try
                {
                    velocity = int.Parse(Console.ReadLine());

                    if (velocity < 0 || velocity > 90)
                        throw new ArgumentException();

                    velocity /= 5;
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;                    
                    Console.WriteLine("Please enter a valid number.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    AskPlayerVelocity();
                }
            }

            AskPlayerAngle();
            // Validates angle input, cannot be less than 0 or greater than 90
            while (angle < 0 || angle > 90)
            {
                try
                {
                    angle = float.Parse(Console.ReadLine());

                    if (angle < 0 || angle > 90)
                        throw new ArgumentException();
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please enter a valid number.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    AskPlayerAngle();
                }
            }

            BananaPos.ClearTranslation();

            CheckGameState();
            hasAsked = false;
        }

        /// <summary>
        /// Checks whether it is Player 1's or Player 2's turn and asks the respective player to enter a velocity.
        /// </summary>
        private void AskPlayerVelocity()
        {
            switch (currentGameState)
            {
                case GameState.P1_TURN:
                    Console.Write("Player 1 | Please enter a velocity (0 - 90): ");
                    oldGameState = GameState.P2_TURN;
                    break;
                case GameState.P2_TURN:
                    Console.Write("Player 2 | Please enter a velocity (0 - 90): ");
                    break;
            }
        }

        /// <summary>
        /// Checks whether it is Player 1's or Player 2's turn and asks the respective player to enter an angle.
        /// </summary>
        private void AskPlayerAngle()
        {
            switch (currentGameState)
            {
                case GameState.P1_TURN:
                    Console.Write("Player 1 | Please enter an angle (0 - 90): ");
                    break;
                case GameState.P2_TURN:
                    Console.Write("Player 2 | Please enter an angle (0 - 90): ");
                    break;
            }
        }

        private void CheckGameState()
        {
            float normalisedAngle = angle / 90.0f;
            // Sets the banana in front of the player and moves it in motion.
            switch (currentGameState)
            {
                case GameState.P1_TURN:
                    // Set the banana's position to in front of player 1.
                    BananaPos *= Matrix4.CreateTranslation(PlayerPositions[0].X + 1.5f, PlayerPositions[0].Y + 1, PlayerPositions[0].Z);
                    bananaCube.position = BananaPos.ExtractTranslation();
                    BananaPos = BananaPos.ClearTranslation();
                    // Add force to the banana cube and tell the game that the banana is now in motion.
                    bananaCube.Force = new Vector3(velocity * (1 - normalisedAngle) + (windFactor / 2.0f), 0.5f + ((velocity + (windFactor / 2.0f)) * normalisedAngle), 0);
                    currentGameState = GameState.BANANA_IN_MOTION;
                    oldGameState = GameState.P1_TURN;
                    break;
                case GameState.P2_TURN:
                    // Set the banana's position to in front of player 2.
                    BananaPos *= Matrix4.CreateTranslation(PlayerPositions[1].X - 2, PlayerPositions[1].Y + 1, PlayerPositions[1].Z);
                    bananaCube.position = BananaPos.ExtractTranslation();
                    BananaPos = BananaPos.ClearTranslation();
                    // Add force to the banana cube and tell the game that the banana is now in motion.
                    bananaCube.Force = new Vector3((-velocity * (1 - normalisedAngle) + (windFactor / 2.0f)), 0.5f + ((velocity + (windFactor / 2.0f)) * normalisedAngle), 0);
                    currentGameState = GameState.BANANA_IN_MOTION;
                    oldGameState = GameState.P2_TURN;
                    break;
                case GameState.BANANA_IN_MOTION:
                    break;
            }
        }

        private void TurnTimeCheck()
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            while (s.ElapsedMilliseconds < (10 * 1000))
            {
            }
            s.Stop();
            if (oldGameState == GameState.P2_TURN)
                currentGameState = GameState.P1_TURN;
            else if (oldGameState == GameState.P1_TURN)
                currentGameState = GameState.P2_TURN;
            hasTimeCheckBegun = false;
            hasAsked = false;
            inputHasStarted = false;
            velocity = -1;
            angle = -1;
            bananaCube.position = Vector3.Zero;
        }
    }
}