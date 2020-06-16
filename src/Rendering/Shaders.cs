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
using System.IO;

namespace Vectoray.Rendering.OpenGL
{
    #region Enum declarations

    /// <summary>
    /// An enum of the two types of OpenGL shader.
    /// </summary>
    public enum ShaderType
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

    #endregion

    /// <summary>
    /// A wrapper for an OpenGL shader object.
    /// </summary>
    public class Shader
    {
        #region Variable & property declaration

        /// <summary>
        /// The parent renderer of this shader, representing the OpenGL context it will use.
        /// </summary>
        private readonly Renderer parentRenderer;

        /// <summary>
        /// The OpenGL identifier for this shader object.
        /// </summary>
        private readonly uint id;

        /// <summary>
        /// The type of OpenGL shader this is.
        /// </summary>
        public readonly ShaderType type;

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
            parentRenderer.IsShader(id) &&
            parentRenderer.GetShaderParam(id, ShaderParams.DELETE_STATUS) is Some<int>(int x) &&
            x == 0;

        #endregion

        #region Lifecycle functionality

        private Shader(Renderer renderer, uint id, ShaderType type) =>
            (parentRenderer, this.id, this.type) = (renderer, id, type);

        /// <summary>
        /// Creates a new shader of the specified type using the given source code.
        /// </summary>
        /// <param name="type">The shader type to create.</param>
        /// <param name="sources">An array of strings containing the source code to be used.</param>
        /// <returns>
        /// A `Valid` containing a new `Shader` if successful, or an `Invalid` containing an error if one occurred.
        /// </returns>
        public static Result<Shader, ShaderException> CreateNew(Renderer renderer, ShaderType type, string[] sources)
        {
            if (renderer == null)
                return new ShaderException(ShaderExceptionType.RendererNull,
                    "Failed to create a shader as the renderer provided was null.").Invalid();

            if (renderer.CreateShader(type) is Some<uint>(uint shaderId))
            {
                Shader instance = new Shader(renderer, shaderId, type);
                renderer.SetShaderSource(instance.id, sources);
                renderer.CompileShader(instance.id);

                // // The 'is' check here is for deconstruction; the method only returns None if the object id
                // // doesn't represent a shader object, and we can be pretty certain that won't happen.
                if (renderer.GetShaderParam(instance.id, ShaderParams.COMPILE_STATUS) is Some<int>(int x) && x == GL.FALSE)
                {
                    // Same deal for the 'is' check here.
                    if (renderer.GetShaderInfoLog(instance.id) is Some<string>(string message))
                        return new ShaderException(ShaderExceptionType.CompilationFailed,
                            $"Failed to compile OpenGL shader '{instance.id}'. Info log: {message}").Invalid();
                    else return new ShaderException(ShaderExceptionType.CompilationFailed,
                        $"Failed to compile OpenGL shader '{instance.id}'. Additionally, the info log could not be retrieved.")
                        .Invalid();
                }
                else return instance.Valid();
            }
            return new ShaderException(ShaderExceptionType.CreationFailed,
                "Failed to create OpenGL shader object for Shader instance.").Invalid();
        }

        /// <summary>
        /// Creates a new shader of the specified type using the given source code.
        /// </summary>
        /// <param name="type">The shader type to create.</param>
        /// <param name="source">A string containing the source code to be used.</param>
        /// <returns>
        /// A `Valid` containing a new `Shader` if successful, or an `Invalid` containing an error if one occurred.
        /// </returns>
        public static Result<Shader, ShaderException> CreateNew(Renderer renderer, ShaderType type, string source)
            => CreateNew(renderer, type, new[] { source });

        ~Shader()
        {
            if (IsUsable) parentRenderer.DeleteShader(id);
        }

        #endregion
    }

    /// <summary>
    /// A wrapper for an OpenGL shader program object.
    /// </summary>
    public class ShaderProgram
    {
        /// <summary>
        /// The parent renderer of this program object, representing the OpenGL context it will use.
        /// </summary>
        private readonly Renderer parentRenderer;

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
            parentRenderer.IsProgram(id) &&
            parentRenderer.GetProgramParam(id, ProgramParams.DELETE_STATUS) is Some<int>(int x) &&
            x == 0;

        private ShaderProgram(Renderer renderer, uint id) => (parentRenderer, this.id) = (renderer, id);

        /// <summary>
        /// Creates a new shader program using the given shaders.
        /// </summary>
        /// <param name="shaders">An array of shaders to be attached to this program.</param>
        /// <returns>
        /// A `Valid` containing a new `ShaderProgram` if successful, or an `Invalid` containing an error if one occurred.
        /// </returns>
        public static Result<ShaderProgram, ShaderProgramException> CreateNew(Renderer renderer, Shader[] shaders)
        {
            if (renderer == null)
                return new ShaderProgramException(ShaderProgramExceptionType.RendererNull,
                    "Failed to create a shader program as the renderer provided was null.").Invalid();

            if (renderer.CreateProgram() is Some<uint>(uint programId))
            {
                ShaderProgram instance = new ShaderProgram(renderer, programId);
                foreach (Shader shader in shaders)
                {
                    if (shader.ID is Some<uint>(uint shaderId))
                        renderer.AttachShader(programId, shaderId);
                    else
                    {
                        renderer.DeleteProgram(programId);
                        return new ShaderProgramException(ShaderProgramExceptionType.ShaderNotUsable,
                            "Failed to create OpenGL shader program as one of the given shaders was not usable.").Invalid();
                    }
                }

                renderer.LinkProgram(programId);
                // The 'is' check here is for deconstruction; the method only returns None if the object id
                // doesn't represent a shader object, and we can be pretty certain that won't happen.
                if (renderer.GetProgramParam(instance.id, ProgramParams.LINK_STATUS) is Some<int>(int x) && x == 0)
                {
                    // Same deal for the 'is' check here.
                    if (renderer.GetShaderInfoLog(instance.id) is Some<string>(string message))
                        return new ShaderProgramException(ShaderProgramExceptionType.CreationFailed,
                            $"Failed to link OpenGL program '{instance.id}'. Info log: {message}").Invalid();
                }

                foreach (Shader shader in shaders)
                {
                    if (shader.ID is Some<uint>(uint shaderId))
                        renderer.DetachShader(programId, shaderId);
                }
                return instance.Valid();
            }
            return new ShaderProgramException(ShaderProgramExceptionType.CreationFailed,
                "Failed to create OpenGL shader object for GLShader instance.").Invalid();
        }

        ~ShaderProgram()
        {
            if (IsUsable) parentRenderer.DeleteProgram(id);
        }
    }

    #region Exception definitions

    /// <summary>
    /// A type used to represent the various errors that can occur for the Shader class.
    /// </summary>
    public class ShaderException : ExceptionEnum<ShaderExceptionType>
    {
        public ShaderException(ShaderExceptionType type) : base(type) { }
        public ShaderException(ShaderExceptionType type, string message) : base(type, message) { }
        public ShaderException(ShaderExceptionType type, string message, Exception inner) : base(type, message, inner) { }
    }

    /// <summary>
    /// An enum of the various types of errors that can occur for the Shader class.
    /// </summary>
    public enum ShaderExceptionType
    {
        Default,
        RendererNull,
        CreationFailed,
        CompilationFailed
    }

    /// <summary>
    /// A type used to represent the various errors that can occur for the ShaderProgram class.
    /// </summary>
    public class ShaderProgramException : ExceptionEnum<ShaderProgramExceptionType>
    {
        public ShaderProgramException(ShaderProgramExceptionType type) : base(type) { }
        public ShaderProgramException(ShaderProgramExceptionType type, string message) : base(type, message) { }
        public ShaderProgramException(ShaderProgramExceptionType type, string message, Exception inner) : base(type, message, inner) { }
    }

    /// <summary>
    /// An enum of the various types of errors that can occur for the ShaderProgram class.
    /// </summary>
    public enum ShaderProgramExceptionType
    {
        Default,
        RendererNull,
        ShaderNotUsable,
        CreationFailed
    }

    #endregion
}