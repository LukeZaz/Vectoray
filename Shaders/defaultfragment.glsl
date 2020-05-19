#version 460

in VSOutput
{
    vec3 color;
} vsInput;

out vec4 color;

void main()
{
    color = vec4(vsInput.color, 1.0);
}