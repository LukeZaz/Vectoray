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

namespace Vectoray.Rendering.OpenGL
{
    /// <summary>
    /// An exception thrown whenever setting the value of a vital OpenGL configuration attribute has failed.
    /// </summary>
    public class VitalAttributeNotSetException : Exception
    {
        public VitalAttributeNotSetException() : base() { }
        public VitalAttributeNotSetException(string message) : base(message) { }
        public VitalAttributeNotSetException(string message, Exception inner) : base(message, inner) { }
    }

    #region Type & enum declaration

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
    public enum GLErrorCode
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

        private const int GL_ERROR_TYPES = 7;

        /// <summary>
        /// Whether or not the OpenGL configuration attributes have been set before.
        /// 
        /// In this version of Vectoray, changing these attributes is not currently possible.
        /// While this may change, doing so will likely only affect newly-created
        /// OpenGL-compatible windows. See: https://wiki.libsdl.org/SDL_GLattr#OpenGL
        /// </summary>
        public static bool configAttributesSet { get; private set; } = false;

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
            if (configAttributesSet)
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

            configAttributesSet = true;

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
                int attribVal;
                if (SDL_GL_GetAttribute(attribute, out attribVal) != 0)
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

        #region GL function implementations

        #region Debugging

        /// <summary>
        /// Get the value of and reset an arbitrary OpenGL error flag that has recorded an error.
        /// Will not affect any flags if no errors have been recorded.
        /// </summary>
        /// <returns>An OpenGL enum constant representing a type of error that has been recorded at least once.</returns>
        public static GLErrorCode GetError() => _glGetError();

        /// <summary>
        /// Get the values of and reset all OpenGL error flags that have recorded an error.
        /// Will not affect any flags for which no errors have been recorded.
        /// </summary>
        /// <returns>
        /// An array of OpenGL enum constants representing which types of errors have been recorded at least once.
        /// </returns>
        public static GLErrorCode[] GetAllErrors() =>
            Enumerable.Range(0, GL_ERROR_TYPES).Select(_ => GetError()).Where(e => e != GLErrorCode.NO_ERROR).ToArray();

        /// <summary>
        /// Get a string representing an aspect of the current OpenGL connection.
        /// </summary>
        /// <param name="name">
        /// Specifies a symbolic constant, one of GL_VENDOR, GL_RENDERER, GL_VERSION, or GL_SHADING_LANGUAGE_VERSION.
        /// </param>
        /// <returns>A string describing an aspect of the current OpenGL connection.</returns>
        public static string GetString(GLConnectionInfo name)
        {
            if (name != GLConnectionInfo.EXTENSIONS) return Marshal.PtrToStringAnsi(_glGetString(name));
            else Debug.LogError("Cannot read a GL_EXTENSIONS string with 'GetString(GLConnectionInfo name)'. "
                              + "(Did you mean to use 'GetString(int index)'?)");
            return string.Empty;
        }

        /// <summary>
        /// Get an extension string supported by the OpenGL implementation at `index`.
        /// </summary>
        /// <param name="index">The index of the string to return.</param>
        /// <returns>The string at `index`.</returns>
        public static string GetExtensionString(int index) =>
            Marshal.PtrToStringAnsi(_glGetStringi(GLConnectionInfo.EXTENSIONS, index));

        #endregion

        /// <summary>
        /// Clear the given OpenGL buffers and reset them to preset values.
        /// </summary>
        /// <param name="mask">A bitmask of the buffers to clear.</param>
        public static void Clear(GLClearMask mask) => _glClear(mask);

        /// <summary>
        /// Set the clear values for the OpenGL color buffers.
        /// 
        /// When `GL_COLOR_BUFFER_BIT` is cleared using `GL.Clear`, it will be set to these values.
        /// </summary>
        /// <param name="red">The red value to use.</param>
        /// <param name="green">The green value to use.</param>
        /// <param name="blue">The blue value to use.</param>
        /// <param name="alpha">The alpha value to use.</param>
        public static void ClearColor(float red, float green, float blue, float alpha) =>
            _glClearColor(red, green, blue, alpha);

        #endregion

        #region GL function delegate definitions

        /// <summary>
        /// Shorthand call for GetDelegateForFunctionPointer using SDL_GL_GetProcAddress.
        /// </summary>
        /// <typeparam name="T">Function delegate type to get and return.</typeparam>
        /// <param name="funcName">Name of the function for GetProcAddress, if it's different from the type name.
        /// 
        /// If not given, this will be set to the type name of T with underscores removed.</param>
        /// <returns>Delegate for the function.</returns>
        private static T GetDelegate<T>(string funcName = null) where T : Delegate
        {
            if (string.IsNullOrWhiteSpace(funcName)) funcName = typeof(T).Name.Replace("_", "");
            return Marshal.GetDelegateForFunctionPointer<T>(SDL_GL_GetProcAddress(funcName));
        }

        #region Debugging

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate GLErrorCode glGetError();
        private static readonly glGetError _glGetError = GetDelegate<glGetError>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr glGetString(GLConnectionInfo name);
        private static readonly glGetString _glGetString = GetDelegate<glGetString>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr glGetStringi(GLConnectionInfo name, int index);
        // glGetString includes glGetStringi, but delegates cannot be overloaded,
        // and the delegates cannot be combined because glGetStringi only accepts one type of
        // GLConnectionInfo value.
        private static readonly glGetStringi _glGetStringi = GetDelegate<glGetStringi>("glGetString");

        #endregion

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClear(GLClearMask mask);
        private static readonly glClear _glClear = GetDelegate<glClear>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClearColor(float red, float green, float blue, float alpha);
        private static readonly glClearColor _glClearColor = GetDelegate<glClearColor>();

        #endregion
    }
}