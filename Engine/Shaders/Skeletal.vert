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
layout (location = 3) in vec4 weights;
layout (location = 4) in vec4 bonesIds;

const int MAX_BONES = 100;
const int MAX_BONES_INFLUENCE = 4;

uniform Transform transform;
uniform mat4 bonesMatrices[MAX_BONES];

out vec3 normal;
out vec2 textureCoord;
out vec3 worldPosition;

void main()
{
    vec4 vertex = vec4(vertexPosition, 1.0);
    vec4 blendVertex = vec4(0.0);
    vec3 blendNormal = vec3(0.0);
    int influences = 0;

    for (int i = 0; i < MAX_BONES_INFLUENCE; i++)
    {
        if (bonesIds[i] == -1)
        {
            continue;
        }
    
        int index = int(bonesIds[i]);
        blendVertex += (vertex * bonesMatrices[index]) * weights[i];
        blendNormal += (vec4(vertexNormal, 0.0) * bonesMatrices[index]).xyz * weights[i];
        influences += 1;
    }

    if (influences == 0) 
    {
        blendVertex = vertex;
        blendNormal = vertexNormal;
    }

    gl_Position = vec4(blendVertex.xyz, 1.0) * transform.model * transform.view * transform.projection;
    worldPosition = vec3(blendVertex * transform.model);
    normal = blendNormal * mat3(transpose(inverse(transform.model)));
    textureCoord = vertexTextureCoord;
}