#version 330 core

struct Material {
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
    float shininess;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform Light light;
uniform Material material;
uniform vec3 viewPosition;
uniform sampler2D texture0;

out vec4 color;

in vec3 normal;
in vec2 textureCoord;
in vec3 worldPosition;

void main()
{
    //ambient
    vec3 ambient = light.ambient * material.ambient;

    //diffuse 
    vec3 norm = normalize(normal);
    vec3 lightDir = normalize(light.position - worldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = light.diffuse * (diff * material.diffuse);

    //specular
    vec3 viewDir = normalize(viewPosition - worldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = light.specular * (spec * material.specular);

    vec3 result = ambient + diffuse + specular;
    color = vec4(result, 1.0) * texture(texture0, textureCoord);
}