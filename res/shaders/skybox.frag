#version 330 core

uniform samplerCube Skybox;

in vec3 textureDir;

out vec4 FragColour;

void main()
{
	FragColour = texture(Skybox, textureDir);
}