#version 460

in VS_Output
{
    vec3 color;
} input;

out vec4 color;

void main()
{
    color = input.color;
}