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
    public enum GLConnectionInfo
    {
        VENDOR = 0x1F00,
        RENDERER = 0x1F01,
        VERSION = 0x1F02,
        EXTENSIONS = 0x1F03,
        SHADING_LANGUAGE_VERSION = 0x8B8C
    }

    /// <summary>
    /// An enum of the three OpenGL buffer bit flags.
    /// </summary>
    public enum GLClearMask
    {
        DEPTH_BUFFER_BIT = 0x00000100,
        STENCIL_BUFFER_BIT = 0x00000400,
        COLOR_BUFFER_BIT = 0x00004000
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