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
    /// An enum of the various OpenGL Shader Object parameters, used for GetShaderParam.
    /// </summary>
    public enum ShaderParams
    {
        SHADER_TYPE = 0x8B4F,
        DELETE_STATUS = 0x8B80,
        COMPILE_STATUS = 0x8B81,
        INFO_LOG_LENGTH = 0x8B84,
        SHADER_SOURCE_LENGTH = 0x8B88
    }

    /// <summary>
    /// An enum of the various OpenGL Program Object parameters, used for GetProgramParam.
    /// </summary>
    public enum ProgramParams
    {
        COMPUTE_GROUP_WORK_SIZE = 0x8267,
        PROGRAM_BINARY_LENGTH = 0x8741,
        GEOMETRY_VERTICES_OUT = 0x8916,
        GEOMETRY_INPUT_TYPE = 0x8917,
        GEOMETRY_OUTPUT_TYPE = 0x8918,
        ACTIVE_UNIFORM_BLOCK_MAX_NAME_LENGTH = 0x8A35,
        ACTIVE_UNIFORM_BLOCKS = 0x8A36,
        DELETE_STATUS = 0x8B80,
        COMPILE_STATUS = 0x8B81,
        LINK_STATUS = 0x8B82,
        VALIDATE_STATUS = 0x8B83,
        INFO_LOG_LENGTH = 0x8B84,
        ATTACHED_SHADERS = 0x8B85,
        ACTIVE_UNIFORMS = 0x8B86,
        ACTIVE_UNIFORM_MAX_LENGTH = 0x8B87,
        ACTIVE_ATTRIBUTES = 0x8B89,
        ACTIVE_ATTRIBUTE_MAX_LENGTH = 0x8B8A,
        TRANSFORM_FEEDBACK_VARYING_MAX_LENGTH = 0x8C76,
        TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F,
        TRANSFORM_FEEDBACK_VARYINGS = 0x8C83,
        ACTIVE_ATOMIC_COUNTER_BUFFERS = 0x92D9
    }

    /// <summary>
    /// A wrapper for an OpenGL shader object.
    /// </summary>
    public class Shader
    {
        #region Variable & property declaration

        /// <summary>
        /// The OpenGL identifier for this shader object.
        /// </summary>
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
            GL.GetShaderParam(id, ShaderParams.DELETE_STATUS) is Some<int>(int x) &&
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
                if (GL.GetShaderParam(instance.id, ShaderParams.COMPILE_STATUS) is Some<int>(int x) && x == 0)
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
                "Failed to create OpenGL shader object for Shader instance.")
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
        /// <summary>
        /// The OpenGL identifier for this program object.
        /// </summary>
        private readonly uint id;

        /// <summary>
        /// Gets the ID of the OpenGL program object this represents, if it's usable;
        /// i.e., the ID represents an OpenGL program object *and* said program object is not marked for deletion.
        /// </summary>
        /// <returns>A `Some` containing the ID if the OpenGL program object was usable, or `None` otherwise.</returns>
        public Opt<uint> ID => IsUsable ? id.Some() : (Opt<uint>)new None<uint>();

        /// <summary>
        /// Whether or not this OpenGL program is usable;
        /// i.e., the ID represents an OpenGL program object *and* said program object is not marked for deletion.
        /// </summary>
        /// <returns>
        /// Whether or not this program both has an ID that represents a valid OpenGL program object,
        /// *and* said program object is not marked for deletion.
        /// </returns>
        public bool IsUsable =>
            GL.IsProgram(id) &&
            GL.GetProgramParam(id, ProgramParams.DELETE_STATUS) is Some<int>(int x) &&
            x == 0;

        private ShaderProgram(uint id) => this.id = id;

        /// <summary>
        /// Creates a new shader program using the given shaders.
        /// </summary>
        /// <param name="shaders">An array of shaders to be attached to this program.</param>
        /// <returns>
        /// A `Valid` containing a new `ShaderProgram` if successful, or an `Invalid` containing an error if one occurred.
        /// </returns>
        public static Result<ShaderProgram, ShaderProgramException> CreateNew(Shader[] shaders)
        {
            if (GL.CreateProgram() is Some<uint>(uint programId))
            {
                ShaderProgram instance = new ShaderProgram(programId);
                foreach (Shader shader in shaders)
                {
                    if (shader.ID is Some<uint>(uint shaderId))
                        GL.AttachShader(programId, shaderId);
                    else
                    {
                        GL.DeleteProgram(programId);
                        return new ShaderNotUsableException(
                            "Failed to create OpenGL shader program as one of the given shaders was not usable.")
                            .Invalid<ShaderProgramException>();
                    }
                }

                GL.LinkProgram(programId);
                // The 'is' check here is for deconstruction; the method only returns None if the object id
                // doesn't represent a shader object, and we can be pretty certain that won't happen.
                if (GL.GetProgramParam(instance.id, ProgramParams.LINK_STATUS) is Some<int>(int x) && x == 0)
                {
                    // Same deal for the 'is' check here.
                    if (GL.GetShaderInfoLog(instance.id) is Some<string>(string message))
                        return new ShaderProgramCreationFailedException(
                            $"Failed to link OpenGL program '{instance.id}'. Info log: {message}")
                            .Invalid<ShaderProgramException>();
                }

                foreach (Shader shader in shaders)
                {
                    if (shader.ID is Some<uint>(uint shaderId))
                        GL.DetachShader(programId, shaderId);
                }
                return instance.Valid();
            }
            return new ShaderProgramCreationFailedException(
                "Failed to create OpenGL shader object for GLShader instance.")
                .Invalid<ShaderProgramException>();
        }

        ~ShaderProgram()
        {
            if (IsUsable) GL.DeleteProgram(id);
        }
    }

    #region Exception definitions

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

    /// <summary>
    /// Base exception type used by the `ShaderProgram` class for `Result` error types.
    /// </summary>
    public class ShaderProgramException : Exception
    {
        protected ShaderProgramException() : base() { }
        protected ShaderProgramException(string message) : base(message) { }
        protected ShaderProgramException(string message, Exception inner) : base(message, inner) { }
    }

    public class ShaderNotUsableException : ShaderProgramException
    {
        public ShaderNotUsableException(string message) : base(message) { }
        public ShaderNotUsableException(string message, Exception inner) : base(message, inner) { }
    }

    public class ShaderProgramCreationFailedException : ShaderProgramException
    {
        public ShaderProgramCreationFailedException(string message) : base(message) { }
        public ShaderProgramCreationFailedException(string message, Exception inner) : base(message, inner) { }
    }

    #endregion
}