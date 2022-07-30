#version 330 core

struct Transform 
{
    mat4 model;
    mat4 view;
    mat4 projection;
};

layout (location = 0) in vec3 vertexPosition;
layout (location = 1) in vec3 vertexNormal;
layout (location = 2) in vec2 vertexTextureCoord;

uniform Transform transform;

out vec3 normal;
out vec2 textureCoord;
out vec3 worldPosition;

void main()
{
    gl_Position = vec4(vertexPosition, 1.0) * transform.model * transform.view * transform.projection;
    worldPosition = vec3(vec4(vertexPosition, 1.0) * transform.model);
    normal = vertexNormal * mat3(transpose(inverse(transform.model)));
    textureCoord = vertexTextureCoord;
}