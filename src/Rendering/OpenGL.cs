/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2020 LukeZaz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Runtime.InteropServices;

using static SDL2.SDL;
using static Vectoray.Extensions;

// TODO: Split this up into several partial classes/files
namespace Vectoray.Rendering.OpenGL
{
    #region Enum declaration

    // TODO: Mark versions as supported/unsupported by the engine as a whole. Also decide on what to/to not support and why.
    // TODO: Determine what 'supporting' an OpenGL version would entail.
    // TODO: Features made available via the engine for supported versions should check the active version before running.
    /// <summary>
    /// Represents various OpenGL versions from 1.0 through 4.6.
    /// 
    /// See the [Khronos OpenGL version history page] for more detail on what each version adds.
    /// 
    /// [Khronos OpenGL version history page]: https://www.khronos.org/opengl/wiki/History_of_OpenGL
    /// </summary>
    public enum GLVersion
    {
        GL_1_0,
        GL_1_1,
        GL_1_2,
        GL_1_3,
        GL_1_4,
        GL_1_5,
        GL_2_0,
        GL_2_1,
        GL_3_0,
        GL_3_1,
        GL_3_2,
        GL_3_3,
        GL_4_0,
        GL_4_1,
        GL_4_2,
        GL_4_3,
        GL_4_4,
        GL_4_5,
        GL_4_6
    }

    /// <summary>
    /// An enum of the seven OpenGL error flags.
    /// </summary>
    public enum ErrorCode
    {
        NO_ERROR = 0,
        INVALID_ENUM = 0x0500,
        INVALID_VALUE = 0x0501,
        INVALID_OPERATION = 0x0502,
        STACK_OVERFLOW = 0x0503,
        STACK_UNDERFLOW = 0x0504,
        OUT_OF_MEMORY = 0x0505,
        INVALID_FRAMEBUFFER_OPERATION = 0x0506
    }

    /// <summary>
    /// An enum of the five OpenGL connection info values.
    /// </summary>
    public enum ConnectionInfo
    {
        VENDOR = 0x1F00,
        RENDERER = 0x1F01,
        VERSION = 0x1F02,
        EXTENSIONS = 0x1F03,
        SHADING_LANGUAGE_VERSION = 0x8B8C,
    }

    /// <summary>
    /// An enum of the various OpenGL buffer target values.
    /// </summary>
    public enum BufferTarget
    {
        ARRAY_BUFFER = 0x8892,
        ELEMENT_ARRAY_BUFFER = 0x8893,
        PIXEL_PACK_BUFFER = 0x88EB,
        PIXEL_UNPACK_BUFFER = 0x88EC,
        UNIFORM_BUFFER = 0x8A11,
        TEXTURE_BUFFER = 0x8C2A,
        TRANSFORM_FEEDBACK_BUFFER = 0x8C8E,
        COPY_READ_BUFFER = 0x8F36,
        COPY_WRITE_BUFFER = 0x8F37,
        DRAW_INDIRECT_BUFFER = 0x8F3F,
        SHADER_STORAGE_BUFFER = 0x90D2,
        DISPATCH_INDIRECT_BUFFER = 0x90EE,
        QUERY_BUFFER = 0x9192,
        ATOMIC_COUNTER_BUFFER = 0x92C0,
    }

    /// <summary>
    /// An enum of the various usage hints that can be provided to OpenGL regarding buffer object data stores.
    /// </summary>
    public enum BufferUsageHint
    {
        STREAM_DRAW = 0x88E0,
        STREAM_READ = 0x88E1,
        STREAM_COPY = 0x88E2,
        STATIC_DRAW = 0x88E4,
        STATIC_READ = 0x88E5,
        STATIC_COPY = 0x88E6,
        DYNAMIC_DRAW = 0x88E8,
        DYNAMIC_READ = 0x88E9,
        DYNAMIC_COPY = 0x88EA,
    }

    /// <summary>
    /// An enum of the various data types accepted for vertex attributes in OpenGL. Note that
    /// this enum does *not* include types for which there is no C# equivalent, such as GLfixed or GLhalf.
    /// </summary>
    public enum VertexDataType
    {
        BYTE = 0x1400,
        UNSIGNED_BYTE = 0x1401,
        SHORT = 0x1402,
        UNSIGNED_SHORT = 0x1403,
        INT = 0x1404,
        UNSIGNED_INT = 0x1405,
        FLOAT = 0x1406,
        DOUBLE = 0x140A,
    }

    /// <summary>
    /// An enum of the three OpenGL buffer bit flags.
    /// </summary>
    public enum ClearMask
    {
        DEPTH_BUFFER_BIT = 0x00000100,
        STENCIL_BUFFER_BIT = 0x00000400,
        COLOR_BUFFER_BIT = 0x00004000
    }

    /// <summary>
    /// An enum of the various OpenGL rendering modes usable with `glDrawArrays`.
    /// </summary>
    public enum DrawMode
    {
        POINTS = 0x0000,
        LINES = 0x0001,
        LINE_LOOP = 0x0002,
        LINE_STRIP = 0x0003,
        TRIANGLES = 0x0004,
        TRIANGLE_STRIP = 0x0005,
        TRIANGLE_FAN = 0x0006,
        LINES_ADJACENCY = 0x000A,
        LINE_STRIP_ADJACENCY = 0x000B,
        TRIANGLES_ADJACENCY = 0x000C,
        TRIANGLE_STRIP_ADJACENCY = 0x000D,
    }

    #endregion

    /// <summary>
    /// General global-use OpenGL methods and functionality.
    /// If you're looking for functionality to do with instances of OpenGL contexts,
    /// create an instance of the Renderer class and use that instead.
    /// </summary>
    public static class GL
    {
        #region Variable & property declaration

        /// <summary>
        /// Whether or not the OpenGL configuration attributes have been set before.
        /// 
        /// In this version of Vectoray, changing these attributes is not currently possible.
        /// While this may change, doing so will likely only affect newly-created
        /// OpenGL-compatible windows. See: https://wiki.libsdl.org/SDL_GLattr#OpenGL
        /// </summary>
        public static bool ConfigAttributesSet { get; private set; } = false;

        #endregion

        // TODO: Unit testing

        // TODO: Running this > 1x is probably already functional, but it should be tested before being allowed.
        /// <summary>
        /// Sets various global OpenGL attributes that need to be set before
        /// initial window creation in order to be properly applied.
        /// 
        /// **This can only be done once,** as the current version of Vectoray does not yet allow them to change.
        /// This is likely to be fixed later, however some attributes will still require jumping through
        /// some extra hoops to change (namely the OpenGL profile mask).
        /// </summary>
        /// <param name="version">The OpenGL version to use. Must be set before any OpenGL windows can be made.</param>
        /// <param name="profileMask">The OpenGL profile to use. Must be set before any OpenGL windows can be made.</param>
        /// <param name="attributes">An array of various other OpenGL attributes to be set.</param>
        public static void SetConfigAttributes(
            GLVersion version,
            SDL_GLprofile profileMask,
            params (SDL_GLattr attrib, int value)[] attributes)
        {
            if (ConfigAttributesSet)
            {
                Debug.LogError("Cannot set global OpenGL configuration attributes when they have already been set.");
                return;
            }

            // Vital attributes are set first in case they fail.
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, version.GetMajor());
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, version.GetMinor());
            SetConfigAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)profileMask);

            foreach ((SDL_GLattr attrib, int value) in attributes)
                SetConfigAttribute(attrib, value);

            ConfigAttributesSet = true;

            static void SetConfigAttribute(SDL_GLattr attribute, int value)
            {
                if (SDL_GL_SetAttribute(attribute, value) != 0)
                {
                    // Since OpenGL's version and profile mask are required in order to properly create
                    // GL contexts via SDL, failing to set these should be considered huge errors that break the program.
                    switch (attribute)
                    {
                        case SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION:
                        case SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION:
                        case SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK:
                            throw new VitalAttributeNotSetException(
                                $"Failed to set vital OpenGL configuration attribute '{GetAttrName(attribute)}'."
                              + $" SDL error: {SDL_GetError()}");
                        default:
                            Debug.LogError(
                                $"Failed to set non-vital OpenGL configuration attribute '{GetAttrName(attribute)}'."
                              + $" SDL error: {SDL_GetError()}"
                            );
                            return;
                    }
                }

                // Check if the attribute has the expected value, then warn the user if it does not.
                // As detailed by the warning message, this is in most cases not a problem. When setting
                // OpenGL attributes, some are treated as 'minimum requests'; e.g., you may ask for a 16-bit depth buffer,
                // but get a 24-bit one. Anything less than what you asked for *usually* causes a failure
                // during context creation.
                // See: https://wiki.libsdl.org/SDL_GLattr#OpenGL
                if (SDL_GL_GetAttribute(attribute, out int attribVal) != 0)
                {
                    Debug.LogError(
                        $"Failed to retrieve the value of OpenGL configuration attribute '{GetAttrName(attribute)}'"
                      + $" after setting it. SDL error: {SDL_GetError()}"
                    );
                    return;
                }

                if (attribVal != value)
                {
                    Debug.LogWarning(
                        $"OpenGL configuration attribute '{GetAttrName(attribute)}' was set to {value},"
                      + $" but then found to instead have a value of {attribVal}."
                      + "\nThis is not usually a problem, but you should keep this in mind when using this attribute."
                    );
                }

                static string GetAttrName(SDL_GLattr attrib) =>
                    Enum.GetName(typeof(SDL_GLattr), attrib);
            }
        }

        /// <summary>
        /// Gets the major version number of this OpenGL version enum.
        /// </summary>
        /// <param name="version">The version to retrieve the major version number of.</param>
        /// <returns>The major version number of this OpenGL version enum.</returns>
        public static int GetMajor(this GLVersion version)
        {
            return (int)version switch
            {
                int v when v.IsWithin(0..6) => 1,
                int v when v.IsWithin(6..8) => 2,
                int v when v.IsWithin(8..12) => 3,
                int v when v.IsWithin(12..19) => 4,
                _ => throw new InvalidOperationException("Cannot retrieve version info;"
                    + " OpenGL version is unrecognized or invalid.")
            };
        }

        /// <summary>
        /// Gets the minor version number of this OpenGL version enum.
        /// </summary>
        /// <param name="version">The version to retrieve the minor version number of.</param>
        /// <returns>The minor version number of this OpenGL version enum.</returns>
        public static int GetMinor(this GLVersion version)
        {
            return (int)version switch
            {
                int v when new[] { 0, 6, 8, 12 }.Contains(v) => 0,
                int v when new[] { 1, 7, 9, 13 }.Contains(v) => 1,
                int v when new[] { 2, 10, 14 }.Contains(v) => 2,
                int v when new[] { 3, 11, 15 }.Contains(v) => 3,
                int v when new[] { 4, 16 }.Contains(v) => 4,
                int v when new[] { 5, 17 }.Contains(v) => 5,
                18 => 6,
                _ => throw new InvalidOperationException("Cannot retrieve version info;"
                    + " OpenGL version is unrecognized or invalid.")
            };
        }
    }

    #region Exception declaration

    /// <summary>
    /// An exception thrown whenever setting the value of a vital OpenGL configuration attribute has failed.
    /// </summary>
    public class VitalAttributeNotSetException : Exception
    {
        public VitalAttributeNotSetException() : base() { }
        public VitalAttributeNotSetException(string message) : base(message) { }
        public VitalAttributeNotSetException(string message, Exception inner) : base(message, inner) { }
    }

    #endregion
}