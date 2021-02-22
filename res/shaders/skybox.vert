#version 330 core

uniform mat4 Projection;
uniform mat4 View;

layout (location = 0) in vec3 aPos;

out vec3 textureDir;

void main()
{
	textureDir = aPos;
	gl_Position = Projection * View * vec4(aPos, 1.0f);
}