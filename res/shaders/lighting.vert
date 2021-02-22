#version 330 core

uniform mat4 Model;
uniform mat4 View;
uniform mat4 Projection;

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoords;

void main() 
{ 
	gl_Position = vec4(aPos, 1) * Model * View * Projection;
	TexCoords = aTexCoords;
	FragPos = vec3(Model * vec4(aPos, 1.0));
	Normal = mat3(transpose(inverse(Model))) * aNormal;
}