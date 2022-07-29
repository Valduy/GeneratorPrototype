#version 330 core

struct Material 
{
    vec3 color;
    float ambient;
    float shininess;
    float specular;    
};

struct Light 
{
    vec3 position;
    vec3 color;
};

uniform Light light;
uniform Material material;
uniform vec3 viewPosition;
uniform sampler2D texture0;

out vec4 fragmentColor;

in vec3 normal;
in vec2 textureCoord;
in vec3 worldPosition;

void main()
{
    vec4 color = texture(texture0, textureCoord) * vec4(material.color, 1.0);
    vec3 lightDirection = normalize(worldPosition - light.position);

    //ambient
    vec3 ambient = material.ambient * light.color;

    //diffuse 
    vec3 norm = normalize(normal);
    vec3 toLightDirection = -lightDirection;
    float diff = max(dot(norm, toLightDirection), 0.0);
    vec3 diffuse = diff * light.color;

    //specular
    vec3 viewDirection = normalize(viewPosition - worldPosition);
    vec3 reflectDirection = reflect(toLightDirection, norm);
    float spec = pow(max(dot(viewDirection, reflectDirection), 0.0), material.shininess);
    vec3 specular = material.specular * spec * light.color;

    fragmentColor = vec4(ambient + diffuse + specular, 1.0) * color;
}