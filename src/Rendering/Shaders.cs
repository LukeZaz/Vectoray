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

namespace Vectoray.Rendering.OpenGL
{
    /// <summary>
    /// An enum of the two types of OpenGL shader.
    /// </summary>
    public enum GLShaderType
    {
        FRAGMENT_SHADER = 0x8B30,
        VERTEX_SHADER = 0x8B31
    }

    /// <summary>
    /// An enum of the various OpenGL Shader Object parameters, used for GetShaderiv.
    /// </summary>
    public enum GLShaderObjectParams
    {
        SHADER_TYPE = 0x8B4F,
        DELETE_STATUS = 0x8B80,
        COMPILE_STATUS = 0x8B81,
        INFO_LOG_LENGTH = 0x8B84,
        SHADER_SOURCE_LENGTH = 0x8B88
    }

    /// <summary>
    /// A wrapper for an OpenGL shader object.
    /// </summary>
    public class Shader
    {
        #region Variable & property declaration

        private readonly uint id;

        /// <summary>
        /// The type of OpenGL shader this is.
        /// </summary>
        public readonly GLShaderType type;

        /// <summary>
        /// Gets the ID of the OpenGL shader object this represents, if it's usable;
        /// i.e., the ID represents an OpenGL shader object *and* said shader object is not marked for deletion.
        /// </summary>
        /// <returns>A `Some` containing the ID if the OpenGL shader object was usable, or `None` otherwise.</returns>
        public Opt<uint> ID => IsUsable ? id.Some() : (Opt<uint>)new None<uint>();

        /// <summary>
        /// Whether or not this shader is usable;
        /// i.e., the ID represents an OpenGL shader object *and* said shader object is not marked for deletion.
        /// </summary>
        /// <returns>
        /// Whether or not this shader both has an ID that represents a valid OpenGL shader object,
        /// *and* said shader object is not marked for deletion.
        /// </returns>
        public bool IsUsable =>
            GL.IsShader(id) &&
            GL.GetShaderParam(id, GLShaderObjectParams.DELETE_STATUS) is Some<int>(int x) &&
            x == 0;

        #endregion

        #region Lifecycle functionality

        private Shader(uint id, GLShaderType type) => (this.id, this.type) = (id, type);

        /// <summary>
        /// Creates a new shader of the specified type using the given source code.
        /// </summary>
        /// <param name="type">The shader type to create.</param>
        /// <param name="sources">An array of strings containing the source code to be used.</param>
        /// <returns>
        /// A `Valid` containing a new `Shader` if successful, or an `Invalid` containing an error if one occurred.
        /// </returns>
        public static Result<Shader, ShaderException> CreateNew(GLShaderType type, string[] sources)
        {
            if (GL.CreateShader(type) is Some<uint>(uint shaderId))
            {
                Shader instance = new Shader(shaderId, type);
                GL.SetShaderSource(instance.id, sources);
                GL.CompileShader(instance.id);

                // The 'is' check here is for deconstruction; the method only returns None if the object id
                // doesn't represent a shader object, and we can be pretty certain that won't happen.
                if (GL.GetShaderParam(instance.id, GLShaderObjectParams.COMPILE_STATUS) is Some<int>(int x) && x == 0)
                {
                    // Same deal for the 'is' check here.
                    if (GL.GetShaderInfoLog(instance.id) is Some<string>(string message))
                        return new ShaderCompilationFailedException(
                            $"Failed to compile OpenGL shader '{instance.id}'. Info log: {message}")
                            .Invalid<ShaderException>();
                }
                else return instance.Valid();
            }
            return new ShaderCreationFailedException(
                "Failed to create OpenGL shader object for GLShader instance.")
                .Invalid<ShaderException>();
        }

        ~Shader()
        {
            if (IsUsable) GL.DeleteShader(id);
        }

        #endregion
    }

    /// <summary>
    /// A wrapper for an OpenGL shader program object.
    /// </summary>
    public class ShaderProgram
    {
        // TODO: This class.
    }

    /// <summary>
    /// Base exception type used by the `Shader` class for `Result` error types.
    /// </summary>
    public class ShaderException : Exception
    {
        protected ShaderException() : base() { }
        protected ShaderException(string message) : base(message) { }
        protected ShaderException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// An exception used to indicate that OpenGL failed to create the requested shader object.
    /// </summary>
    public class ShaderCreationFailedException : ShaderException
    {
        public ShaderCreationFailedException(string message) : base(message) { }
        public ShaderCreationFailedException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// An exception used to indicate that OpenGL failed to compile the given shader object.
    /// </summary>
    public class ShaderCompilationFailedException : ShaderException
    {
        public ShaderCompilationFailedException(string message) : base(message) { }
        public ShaderCompilationFailedException(string message, Exception inner) : base(message, inner) { }
    }
}