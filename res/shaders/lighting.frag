#version 330 core

struct Material
{
	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
	float shininess;
};

struct DirLight
{
	vec3 direction;

	vec3 ambient;
	vec3 diffuse;
	vec3 specular;
};


uniform bool dirLightRequested;

uniform DirLight directionalLight;
uniform Material material;

uniform sampler2D tex;

in vec2 TexCoords;
in vec3 Normal;
in vec3 FragPos;

uniform vec3 ViewPos;

out vec4 FragColour;

void DirectionalLighting();
void SpotlightLighting();

void main()
{
	if (dirLightRequested)
		DirectionalLighting();

	FragColour *= vec4(vec3(texture2D(tex, TexCoords)), 1.0f);
}


void DirectionalLighting()
{
	// diffuse
	vec3 norm = normalize(Normal);
	vec3 lightDir = normalize(-directionalLight.direction);
	float diff = max(dot(norm, lightDir), 0.0);
	
	// specular
	vec3 viewDir = normalize(ViewPos - FragPos);
	vec3 reflectDir = reflect(-lightDir, norm);
	float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);

	vec3 result = (material.ambient * directionalLight.ambient) + ((diff * material.diffuse) * directionalLight.diffuse) + (spec * material.specular * directionalLight.specular);
	FragColour += vec4(result, 1.0f);
}