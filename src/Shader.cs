using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace Gorillas3D.Components
{
    class Shader
    {
        public int ProgramHandle { get; private set; }
        public int VertexHandle { get; private set; }
        public int FragmentHandle { get; private set; }

        public Shader(in string pVertexShaderFile = "res/shaders/lighting.vert", in string pFragmentShaderFile = "res/shaders/lighting.frag")
        {
            VertexHandle = GL.CreateShader(ShaderType.VertexShader);
            ParseShader(pVertexShaderFile, VertexHandle);

            FragmentHandle = GL.CreateShader(ShaderType.FragmentShader);
            ParseShader(pFragmentShaderFile, FragmentHandle);

            ProgramHandle = GL.CreateProgram();
            GL.AttachShader(ProgramHandle, VertexHandle);
            GL.AttachShader(ProgramHandle, FragmentHandle);
            GL.LinkProgram(ProgramHandle);

            // Once the shaders have been attached to the program, there is no need to keep the shaders.
            GL.DetachShader(ProgramHandle, VertexHandle);
            GL.DetachShader(ProgramHandle, FragmentHandle);
            GL.DeleteShader(VertexHandle);
            GL.DeleteShader(FragmentHandle);
        }

        private void ParseShader(in string pShaderFile, int ShaderID)
        {
            using (StreamReader reader = new StreamReader(pShaderFile))
            {
                GL.ShaderSource(ShaderID, reader.ReadToEnd());
            }

            GL.CompileShader(ShaderID);
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out int result);

            if (result == 0)
                throw new Exception("Shader compilation failed! " + GL.GetShaderInfoLog(ShaderID));
        }

        public void Delete()
        {
            GL.DeleteShader(VertexHandle);
            GL.DeleteShader(FragmentHandle);
            GL.DeleteProgram(ProgramHandle);
        }
    }
}
