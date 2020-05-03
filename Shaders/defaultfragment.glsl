#version 460

in VSOutput
{
    vec3 color;
} input;

out vec4 color;

void main()
{
    color = input.color;
}