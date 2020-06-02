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
using System.Text;

using Vectoray.Rendering.OpenGL;

using static SDL2.SDL;

// TODO: Support for debugging contexts/output.
// See: https://www.khronos.org/opengl/wiki/Debug_Output
namespace Vectoray.Rendering
{
    /// <summary>
    /// Represents the various VSync modes. Specifically:
    /// 
    /// **Disabled (0):** No VSync; frame updates are immediate. Can cause screen tearing if the framerate
    /// is higher than the monitor's refresh rate.
    /// 
    /// **Enabled (1):** Regular VSync; frame updates are synchronized with the vertical retrace. Avoids screen tearing,
    /// but it can slightly slow down the game or cause input lag in some situations.
    /// 
    /// **Adaptive (-1):** Adaptive VSync; uses regular VSync unless a vertical retrace was already missed, in which
    /// case the buffer will be swapped immediately instead of waiting, so as to avoid some
    /// of the issues VSync sometimes causes. **Not always supported.**
    /// </summary>
    public enum VSyncMode
    {
        Adaptive = -1,
        Disabled = 0,
        Enabled = 1
    }

    public class Renderer
    {
        #region Variable & property declaration

        private const int GL_ERROR_TYPES = 7;

        private readonly IntPtr context;
        private readonly IntPtr window;

        /// <summary>
        /// Whether or not this renderer's inner OpenGL context is current.
        /// </summary>
        public bool IsCurrentContext
        {
            get
            {
                // IntPtr.Zero is effectively a null pointer. It is not equal to C#'s null, however.
                // This is another case of "should really never be null", but stranger things have happened.
                if (context == IntPtr.Zero)
                {
                    Debug.LogError("Cannot check if a Renderer's context is current when it has none.");
                    return false;
                }

                IntPtr currentContext = SDL_GL_GetCurrentContext();
                if (currentContext == IntPtr.Zero)
                {
                    Debug.LogError("Failed to check current OpenGL context. SDL error: ", SDL_GetError());
                    return false;
                }

                return currentContext == context;
            }
        }

        /// <summary>
        /// The version of OpenGL this renderer's inner OpenGL context supports.
        /// </summary>
        public readonly GLVersion contextVersion;

        #endregion

        #region Lifecycle functionality (constructors, finalizers, etc)

        /// <summary>
        /// Create a new Renderer with the given pointers.
        /// </summary>
        private Renderer(IntPtr context, IntPtr window)
        {
            (this.context, this.window) = (context, window);

            SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, out int major);
            SDL_GL_GetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, out int minor);
            contextVersion = (major, minor) switch
            {
                (1, 0) => GLVersion.GL_1_0,
                (1, 1) => GLVersion.GL_1_1,
                (1, 2) => GLVersion.GL_1_2,
                (1, 3) => GLVersion.GL_1_3,
                (1, 4) => GLVersion.GL_1_4,
                (1, 5) => GLVersion.GL_1_5,
                (2, 0) => GLVersion.GL_2_0,
                (2, 1) => GLVersion.GL_2_1,
                (3, 0) => GLVersion.GL_3_0,
                (3, 1) => GLVersion.GL_3_1,
                (3, 2) => GLVersion.GL_3_2,
                (3, 3) => GLVersion.GL_3_3,
                (4, 0) => GLVersion.GL_4_0,
                (4, 1) => GLVersion.GL_4_1,
                (4, 2) => GLVersion.GL_4_2,
                (4, 3) => GLVersion.GL_4_3,
                (4, 4) => GLVersion.GL_4_4,
                (4, 5) => GLVersion.GL_4_5,
                (4, 6) => GLVersion.GL_4_6,
                _ => throw new ArgumentException(
                    "Encountered unrecognized OpenGL version during Renderer creation - this should be impossible!")
            };

            // TODO: Don't bother loading function delegates this context can't support anyways, e.g.
            // glNamedBufferData for a < GL4.5 context.
            #region OpenGL function loading

            // While a few OpenGL functions can be loaded immediately as part of static initialization,
            // most are context-dependent, hence all this.
            _glGetError = GetDelegate<glGetError>();
            _glGetString = GetDelegate<glGetString>();

            _glCreateShader = GetDelegate<glCreateShader>();
            _glShaderSource = GetDelegate<glShaderSource>();
            _glCompileShader = GetDelegate<glCompileShader>();
            _glGetShaderiv = GetDelegate<glGetShaderiv>();
            _glGetShaderInfoLog = GetDelegate<glGetShaderInfoLog>();
            _glAttachShader = GetDelegate<glAttachShader>();
            _glDetachShader = GetDelegate<glDetachShader>();
            _glDeleteShader = GetDelegate<glDeleteShader>();
            _glIsShader = GetDelegate<glIsShader>();

            _glCreateProgram = GetDelegate<glCreateProgram>();
            _glLinkProgram = GetDelegate<glLinkProgram>();
            _glGetProgramiv = GetDelegate<glGetProgramiv>();
            _glGetProgramInfoLog = GetDelegate<glGetProgramInfoLog>();
            _glUseProgram = GetDelegate<glUseProgram>();
            _glDeleteProgram = GetDelegate<glDeleteProgram>();
            _glIsProgram = GetDelegate<glIsProgram>();

            _glGenBuffers = GetDelegate<glGenBuffers>();
            _glCreateBuffers = GetDelegate<glCreateBuffers>();
            _glBindBuffer = GetDelegate<glBindBuffer>();
            _glBufferData = GetDelegate<glBufferData>();
            _glNamedBufferData = GetDelegate<glNamedBufferData>();
            _glDeleteBuffers = GetDelegate<glDeleteBuffers>();
            _glIsBuffer = GetDelegate<glIsBuffer>();

            _glEnableVertexAttribArray = GetDelegate<glEnableVertexAttribArray>();
            _glEnableVertexArrayAttrib = GetDelegate<glEnableVertexArrayAttrib>();
            _glVertexAttribPointer = GetDelegate<glVertexAttribPointer>();
            _glVertexAttribIPointer = GetDelegate<glVertexAttribIPointer>();
            _glVertexAttribLPointer = GetDelegate<glVertexAttribLPointer>();
            _glDisableVertexAttribArray = GetDelegate<glDisableVertexAttribArray>();
            _glDisableVertexArrayAttrib = GetDelegate<glDisableVertexArrayAttrib>();

            _glGenVertexArrays = GetDelegate<glGenVertexArrays>();
            _glCreateVertexArrays = GetDelegate<glCreateVertexArrays>();
            _glBindVertexArray = GetDelegate<glBindVertexArray>();
            _glDeleteVertexArrays = GetDelegate<glDeleteVertexArrays>();
            _glIsVertexArray = GetDelegate<glIsVertexArray>();

            _glClear = GetDelegate<glClear>();
            _glClearColor = GetDelegate<glClearColor>();
            _glDrawArrays = GetDelegate<glDrawArrays>();
            _glViewport = GetDelegate<glViewport>();

            #endregion
        }

        /// <summary>
        /// Create a new renderer using the given window and attributes.
        /// </summary>
        /// <param name="window">The window to create this renderer for.</param>
        /// <param name="attributes">The OpenGL attributes and the values to set them to for this renderer.</param>
        /// <returns>An option representing whether or not the renderer was successfully created.</returns>
        public static Result<Renderer, RendererException> CreateRenderer(IntPtr window)
        {
            if (!GL.ConfigAttributesSet)
                return new GLAttributesNotSetException(
                    "Cannot create an OpenGL renderer before vital OpenGL attributes have been set.")
                    .Invalid<RendererException>();

            IntPtr context = SDL_GL_CreateContext(window);
            if (context == IntPtr.Zero)
                return new ContextCreationFailedException(
                    $"Failed to create OpenGL context during Renderer creation. SDL error: {SDL_GetError()}")
                    .Invalid<RendererException>();

            return new Renderer(context, window).Valid();
        }

        ~Renderer()
        {
            // This is safe to call even if this is the current context, *provided it is on the same thread.*
            // See: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-wgldeletecontext
            SDL_GL_DeleteContext(context);
        }

        #endregion

        /// <summary>
        /// Set the VSync mode for this renderer.
        /// </summary>
        /// <param name="mode">The VSync mode to set this renderer to.</param>
        /// <param name="forceCurrentContext">Whether or not to force this renderer's inner OpenGL context
        /// to be current if it isn't already.</param>
        /// <returns>Whether or not the VSync mode was set successfully.</returns>
        public bool SetVSync(VSyncMode mode, bool forceCurrentContext)
        {
            if (!IsCurrentContext)
            {
                if (!forceCurrentContext) return false;
                else if (!MakeCurrent())
                {
                    Debug.LogError("Failed to set OpenGL VSync mode due to being unable to make this Renderer current.");
                    return false;
                }
            }

            if (SDL_GL_SetSwapInterval((int)mode) != 0)
            {
                Debug.LogError($"Failed to set OpenGL VSync mode. SDL error: {SDL_GetError()}");
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Make this renderer's inner OpenGL context current.
        /// </summary>
        /// <returns>Whether or not the context was made current successfully.</returns>
        public bool MakeCurrent()
        {
            if (SDL_GL_MakeCurrent(window, context) != 0)
            {
                Debug.LogError($"Failed to make a Renderer current. SDL error: {SDL_GetError()}");
                return false;
            }
            else return true;
        }

        #region GL function implementations

        #region Debugging

        /// <summary>
        /// Get the value of and reset an arbitrary OpenGL error flag that has recorded an error.
        /// Will not affect any flags if no errors have been recorded.
        /// </summary>
        /// <returns>An OpenGL enum constant representing a type of error that has been recorded at least once.</returns>
        public ErrorCode GetError() => _glGetError();

        /// <summary>
        /// Get the values of and reset all OpenGL error flags that have recorded an error.
        /// Will not affect any flags for which no errors have been recorded.
        /// </summary>
        /// <returns>
        /// An array of OpenGL enum constants representing which types of errors have been recorded at least once.
        /// </returns>
        public ErrorCode[] GetAllErrors() =>
            Enumerable.Range(0, GL_ERROR_TYPES).Select(_ => GetError()).Where(e => e != ErrorCode.NO_ERROR).ToArray();

        /// <summary>
        /// Get a string representing an aspect of the current OpenGL connection.
        /// </summary>
        /// <remarks>
        /// Using this with GL_VENDOR and GL_RENDERER together can be useful for identifying a unique platform,
        /// as they do not change from release to release.
        /// </remarks>
        /// <param name="name">
        /// Specifies a symbolic constant, one of GL_VENDOR, GL_RENDERER, GL_VERSION, or GL_SHADING_LANGUAGE_VERSION.
        /// </param>
        /// <returns>A string describing an aspect of the current OpenGL connection.</returns>
        public string GetString(ConnectionInfo name)
        {
            if (name != ConnectionInfo.EXTENSIONS) return Marshal.PtrToStringAnsi(_glGetString(name));
            else Debug.LogError("Cannot read a GL_EXTENSIONS string with 'GetString(GLConnectionInfo name)'. "
                              + "(Did you mean to use 'GetExtensionString(int index)'?)");
            return string.Empty;
        }

        /// <summary>
        /// Get an extension string supported by the OpenGL implementation at `index`.
        /// </summary>
        /// <param name="index">The index of the string to return.</param>
        /// <returns>The string at `index`.</returns>
        public Opt<string> GetExtensionString(uint index)
        {
            string str = Marshal.PtrToStringAnsi(_glGetString(ConnectionInfo.EXTENSIONS, index));
            if (GetError() == ErrorCode.INVALID_VALUE)
            {
                Debug.LogError("glGetStringi likely given out-of-range index value; encountered `GL_INVALID_VALUE` error.");
                return str.None();
            }
            else return str.Some();
        }

        /// <summary>
        /// Logs all non-extension OpenGL connection information to the console.
        /// </summary>
        public void LogConnectionInfo()
        {
            Debug.LogColored("<| OpenGL Connection Information", ConsoleColor.White);
            Debug.Log(
                $" | Vendor: {GetString(ConnectionInfo.VENDOR)}\n"
              + $" | Renderer: {GetString(ConnectionInfo.RENDERER)}\n"
              + $" | Version: {GetString(ConnectionInfo.VERSION)}\n"
              + $"<| GLSL version: {GetString(ConnectionInfo.SHADING_LANGUAGE_VERSION)}"
            );
        }

        #endregion

        #region Shaders

        /// <summary>
        /// Create a new OpenGL shader object and get its ID.
        /// </summary>
        /// <param name="type">The type of shader to create.</param>
        /// <returns>A new `Some` containing the shader object ID if creation was successful; `None` otherwise.</returns>
        public Opt<uint> CreateShader(ShaderType type) =>
            _glCreateShader(type)
                .SomeIf(x => x != 0)
                .LogErrorIfNone($"glCreateShader failed and returned 0. Last reported OpenGL error: {GetError()}");

        /// <summary>
        /// Sets an OpenGL shader object's source code, replacing any already associated.
        /// When done, this will also check for an unexpected OpenGL error and log it if found.
        /// </summary>
        /// <param name="shader">The shader to set the source of.</param>
        /// <param name="sources">An array of strings containing the source code to assign to the shader.</param>
        public void SetShaderSource(uint shader, string[] sources)
        {
            if (!IsShader(shader))
            {
                Debug.LogError($"Cannot set shader source of object '{shader}', as it is not an OpenGL shader object.");
                return;
            }

            _glShaderSource(shader, (uint)sources.Length, sources, sources.Select(x => x.Length).ToArray());
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while setting shader {shader}'s source. "
                                      + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Compile a given OpenGL shader's source code.
        /// When done, this will also check for an unexpected OpenGL error and log it if found.
        /// </summary>
        /// <param name="shader">The shader to compile.</param>
        public void CompileShader(uint shader)
        {
            if (!IsShader(shader))
            {
                Debug.LogError($"Cannot compile shader source of object '{shader}', as it is not an OpenGL shader object.");
                return;
            }

            _glCompileShader(shader);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while compiling shader '{shader}'. "
                                      + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Get a parameter from an OpenGL shader object.
        /// When done, this will also check for an unexpected OpenGL error and log it if found.
        /// </summary>
        /// <param name="shader">The shader to query.</param>
        /// <param name="param">The parameter to query.</param>
        /// <returns>
        /// `None` if the given ID did not represent an OpenGL shader, or a `Some` containing the retrieved value otherwise.
        /// </returns>
        public Opt<int> GetShaderParam(uint shader, ShaderParams param)
        {
            if (!IsShader(shader))
            {
                Debug.LogError($"Cannot get shader parameter of object '{shader}', as it is not an OpenGL shader object.");
                return 0.None();
            }

            _glGetShaderiv(shader, param, out int value);
            GetError().LogIfError(
                e => $"Encountered unexpected OpenGL error `{e}` while querying shader '{shader}' for parameter `{param}`. "
                    + "This should be impossible; perhaps an earlier error went uncaught?"
            );
            return value.Some();
        }

        /// <summary>
        /// Get the information log for a given OpenGL shader object, which details the outcome of its compilation.
        /// When done, this will also check for an unexpected OpenGL error and log it if found.
        /// </summary>
        /// <param name="shader">The shader object to get the information log of.</param>
        /// <returns>
        /// `None` if the given ID did not represent an OpenGL shader,
        /// or a `Some` containing the retrieved information log otherwise.
        /// </returns>
        public Opt<string> GetShaderInfoLog(uint shader)
        {
            if (!IsShader(shader))
            {
                Debug.LogError($"Cannot retrieve shader info log of object '{shader}', as it is not an OpenGL shader object.");
                return "".None();
            }

            _glGetShaderiv(shader, ShaderParams.INFO_LOG_LENGTH, out int length);
            StringBuilder infoLog = new StringBuilder(length);

            _glGetShaderInfoLog(shader, (uint)length, out _, infoLog);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while getting shader {shader}'s info log. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
            return infoLog.ToString().Some();
        }

        /// <summary>
        /// Attaches an OpenGL shader object to a given OpenGL program.
        /// </summary>
        /// <param name="program">The program object to attach the shader to.</param>
        /// <param name="shader">The shader object to attach.</param>
        public void AttachShader(uint program, uint shader)
        {
            if (!IsProgram(program))
            {
                Debug.LogError($"Cannot attach a shader to object '{program}' as it is not an OpenGL program object.");
                return;
            }
            else if (!IsShader(shader))
            {
                Debug.LogError($"Cannot attach object '{shader}' to a program as it is not an OpenGL shader object.");
                return;
            }

            _glAttachShader(program, shader);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attaching shader '{shader}' to program '{program}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION
                            => "This can happen if this shader object is already attached to the specified program.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?"
                    }
            );
        }

        /// <summary>
        /// Detaches an OpenGL shader object from a given OpenGL program.
        /// </summary>
        /// <param name="program">The program object to detach the shader from.</param>
        /// <param name="shader">The shader object to detach.</param>
        public void DetachShader(uint program, uint shader)
        {
            if (!IsProgram(program))
            {
                Debug.LogError($"Cannot detach a shader from object '{program}' as it is not an OpenGL program object.");
                return;
            }
            else if (!IsShader(shader))
            {
                Debug.LogError($"Cannot detach object '{shader}' from a program as it is not an OpenGL shader object.");
                return;
            }

            _glDetachShader(program, shader);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while detaching shader '{shader}' from program '{program}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION
                            => "This can happen if this shader object was not attached to the specified program.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?"
                    }
            );
        }

        /// <summary>
        /// Deletes a given OpenGL shader.
        /// </summary>
        /// <param name="shader">The shader to delete.</param>
        public void DeleteShader(uint shader)
        {
            if (!IsShader(shader)) return;
            _glDeleteShader(shader);
        }

        /// <summary>
        /// Checks whether a given OpenGL object is a shader.
        /// </summary>
        /// <param name="objectId">The OpenGL object ID to check. If this is zero,
        /// this function will *always* return false.</param>
        /// <returns>Whether or not the OpenGL object was a shader.</returns>
        public bool IsShader(uint objectId) => _glIsShader(objectId);

        #endregion

        #region Programs

        /// <summary>
        /// Create a new OpenGL program object and get its ID.
        /// </summary>
        /// <returns>A new `Some` containing the program object ID if creation was successful; `None` otherwise.</returns>
        public Opt<uint> CreateProgram() =>
            _glCreateProgram()
                .SomeIf(x => x != 0)
                .LogErrorIfNone($"glCreateProgram failed and returned 0. Last reported OpenGL error: {GetError()}");

        /// <summary>
        /// Link a given OpenGL shader program object.
        /// When done, this will also check for an OpenGL error and log it if found.
        /// </summary>
        /// <param name="program">The program object to link.</param>
        public void LinkProgram(uint program)
        {
            if (!IsProgram(program))
            {
                Debug.LogError("Cannot link shader program of object '{shader}', as it is not an OpenGL program object.");
                return;
            }

            _glLinkProgram(program);
            GetError().LogIfError(
                e => $"Encountered unexpected OpenGL error `{e}` while linking shader program '{program}'. " + e switch
                {
                    ErrorCode.INVALID_OPERATION
                        => "This can happen if this program object is currently active and using transform feedback mode.",
                    _ => "This should be impossible; perhaps an earlier error went uncaught?"
                }
            );
        }

        /// <summary>
        /// Get a parameter from an OpenGL program object.
        /// When done, this will also check for an OpenGL error and log it if found.
        /// </summary>
        /// <param name="shader">The program to query.</param>
        /// <param name="param">The parameter to query.</param>
        /// <returns>
        /// `None` if the given ID did not represent an OpenGL program, or a `Some` containing the retrieved value otherwise.
        /// </returns>
        public Opt<int> GetProgramParam(uint program, ProgramParams param)
        {
            if (!IsProgram(program))
            {
                Debug.LogError($"Cannot get program parameter of object '{program}', as it is not an OpenGL program object.");
                return 0.None();
            }

            _glGetProgramiv(program, param, out int value);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while querying program '{program}' for parameter `{param}`. " + e switch
                {
                    ErrorCode.INVALID_OPERATION when new[] {
                            ProgramParams.GEOMETRY_VERTICES_OUT,
                            ProgramParams.GEOMETRY_INPUT_TYPE,
                            ProgramParams.GEOMETRY_OUTPUT_TYPE
                        }.Contains(param)
                        => "This can be caused if the program being queried does not have a geometry shader.",
                    ErrorCode.INVALID_OPERATION when ProgramParams.COMPUTE_GROUP_WORK_SIZE == param
                        => "This can be caused if the program does not contain a binary for the compute shader stage.",
                    _ => "This should be impossible; perhaps an earlier error went uncaught?"
                }
            );
            return value.Some();
        }

        /// <summary>
        /// Get the information log for a given OpenGL program object, which details the outcome of its linking.
        /// </summary>
        /// <param name="program">The program object to get the information log of.</param>
        /// <returns>
        /// `None` if the given ID did not represent an OpenGL program,
        /// or a `Some` containing the retrieved information log otherwise.
        /// </returns>
        public Opt<string> GetProgramInfoLog(uint program)
        {
            if (!IsProgram(program))
            {
                Debug.LogError(
                    $"Cannot retrieve program info log of object '{program}', as it is not an OpenGL program object.");
                return "".None();
            }

            _glGetProgramiv(program, ProgramParams.INFO_LOG_LENGTH, out int length);
            StringBuilder infoLog = new StringBuilder(length);

            _glGetProgramInfoLog(program, (uint)length, out _, infoLog);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while getting program {program}'s"
                                  + "info log. This should be impossible; perhaps an earlier error went uncaught?");
            return infoLog.ToString().Some();
        }

        /// <summary>
        /// Sets the given program to be used as part of the current rendering state.
        /// </summary>
        /// <param name="program">The ID of the program to use.
        /// A value of zero represents no program and will leave no program being used.</param>
        public void UseProgram(uint program)
        {
            if (!IsProgram(program))
            {
                Debug.Log($"Cannot use program `{program}` for rendering, as it is not an OpenGL program object.");
                return;
            }

            _glUseProgram(program);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to use program `{program}`. " + e switch
                {
                    ErrorCode.INVALID_OPERATION
                        => "This can be caused if OpenGL failed to make the program part of the active state,"
                        + " or if transform feedback mode is active.",
                    _ => "This should be impossible; perhaps an earlier error went uncaught?",
                }
            );
        }

        /// <summary>
        /// Deletes a given OpenGL program.
        /// 
        /// If any shader objects are attached to the program, they will be detached as a result of this, though
        /// they will not be deleted unless already marked for such via `DeleteShader`.
        /// </summary>
        /// <param name="program">The program to delete.</param>
        public void DeleteProgram(uint program)
        {
            if (!IsProgram(program)) return;
            _glDeleteProgram(program);
        }

        /// <summary>
        /// Checks whether a given OpenGL object is a shader program.
        /// </summary>
        /// <param name="objectId">The OpenGL object ID to check. If this is zero,
        /// this function will *always* return false.</param>
        /// <returns>Whether or not the OpenGL object was a shader program.</returns>
        public bool IsProgram(uint objectId) => _glIsProgram(objectId);

        #endregion

        #region Buffers
        // TODO: Probably modify this an all opengl object stuff to use wrapper object types?

        /// <summary>
        /// Generate identifiers for one or more OpenGL buffer objects and store them in an array.
        /// 
        /// If this Renderer is using a 4.5 or higher OpenGL context, this method can also
        /// create buffer objects to fill these identifiers at the same time, provided `createObjects` is true.
        /// </summary>
        /// <param name="amount">The amount of identifiers to generate.</param>
        /// <param name="createObjects">
        /// If this is true, buffer objects will also be created to fill the generated identifiers.
        /// </param>
        /// <returns>An array containing the generated buffer object identifiers.
        /// No objects will be created with these identifiers if `createObjects` was false.</returns>
        public uint[] GenBuffers(uint amount, bool createObjects = false)
        {
            uint[] buffers = new uint[amount];
            if (!createObjects)
                _glGenBuffers(amount, buffers);
            else
                _glCreateBuffers(amount, buffers);
            return buffers;
        }

        /// <summary>
        /// Binds a given OpenGL buffer object to one of the various buffer targets.
        /// </summary>
        /// <param name="target">The OpenGL buffer target to bind the buffer to.</param>
        /// <param name="buffer">The OpenGL buffer object to bind.
        /// A value of zero represents no buffer and will unbind any currently bound without replacing them.</param>
        public void BindBuffer(BufferTarget target, uint buffer)
        {
            // TODO: IsBuffer can't be used as unbound buffers don't qualify for it for whatever reason.
            // Need a buffer wrapper for safety.
            _glBindBuffer(target, buffer);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while binding buffer '{buffer}'. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Unbinds any buffer bound to the provided OpenGL buffer target.
        /// </summary>
        /// <param name="target">The buffer target to unbind any currently bound buffer from.</param>
        public void UnbindBuffer(BufferTarget target) => BindBuffer(target, 0);

        /// <summary>
        /// Creates a new data store for the buffer object bound to `target` and fills it with `data`, while also
        /// providing a usage hint for this data to OpenGL. Old data stores will be deleted in this process.
        /// </summary>
        /// <param name="target">The buffer target the buffer in question is bound to.</param>
        /// <param name="data">The data to use to create the new data store for the buffer.</param>
        /// <param name="usage">The usage hint to give to OpenGL for optimization purposes.</param>
        /// <typeparam name="T">
        /// The [unmanaged] type to use.
        /// 
        /// [unmanaged]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types
        /// </typeparam>
        public void SetBufferData(BufferTarget target, float[] data, BufferUsageHint usage)
        {
            // It might be better to instead convert the data to a series of IntPtrs and pass those,
            // but really I'm not sure if it matters, and this is far simpler.
            _glBufferData(target, Marshal.SizeOf<float>() * data.Length, data, usage);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to crete a new data store for the buffer object "
                    + $" bound to buffer target `{target}`. " + e switch
                    {
                        ErrorCode.INVALID_OPERATION
                            => "This can be caused if no buffer is bound to this target, or if the"
                            + " `GL_BUFFER_IMMUTABLE_STORAGE` flag is enabled for the buffer.",
                        ErrorCode.OUT_OF_MEMORY
                            => "This can be caused if OpenGL was unable to create a data store with the specified byte size "
                            + $"of `{Marshal.SizeOf<float>() * data.Length}` "
                            + $"(type size of {Marshal.SizeOf<float>()} * array length of {data.Length}).",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        /// <summary>
        /// Creates a new data store for the given buffer object and fills it with `data`, while also
        /// providing a usage hint for this data to OpenGL. Old data stores will be deleted in this process.
        /// </summary>
        /// <param name="buffer">The buffer object to create the data store for.</param>
        /// <param name="data">The data to use to create the new data store for the buffer.</param>
        /// <param name="usage">The usage hint to give to OpenGL for optimization purposes.</param>
        /// <typeparam name="T">
        /// The [unmanaged] type to use.
        /// 
        /// [unmanaged]: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/unmanaged-types
        /// </typeparam>
        public void SetNamedBufferData<T>(uint buffer, T[] data, BufferUsageHint usage)
            where T : unmanaged
        {
            // TODO: Exit if GL context version is < 4.5, as NamedBufferData is 4.5+ only.
            if (!IsBuffer(buffer))
            {
                Debug.LogError(
                    $"Cannot create a buffer data store for object '{buffer}' as it is not an OpenGL buffer object. "
                    + "(Did you make sure to bind it first?)");
                return;
            }

            _glNamedBufferData(buffer, Marshal.SizeOf<T>() * data.Length, Array.ConvertAll(data, item => (object)item), usage);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to create a new data store for the buffer object"
                    + $" '{buffer}`. " + e switch
                    {
                        ErrorCode.INVALID_OPERATION
                            => "This can be caused if no buffer is bound to this target, or if the"
                            + " `GL_BUFFER_IMMUTABLE_STORAGE` flag is enabled for the buffer.",
                        ErrorCode.OUT_OF_MEMORY
                            => "This can be caused if OpenGL was unable to create a data store with the specified byte size "
                            + $"of `{Marshal.SizeOf<T>() * data.Length}` "
                            + $"(type size of {Marshal.SizeOf<T>()} * array length of {data.Length}).",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        /// <summary>
        /// Deletes each OpenGL buffer in a given array of them.
        /// </summary>
        /// <param name="buffers">The array of buffers to delete.</param>
        public void DeleteBuffers(uint[] buffers)
        {
            uint[] invalidBuffers = buffers.Where(buffer => !IsBuffer(buffer)).ToArray();
            if (invalidBuffers.Length > 0)
            {
                Debug.LogWarning("Cannot delete some elements of an array of buffers, as some were not valid buffer objects "
                    + $"(other elements of the array will still be deleted): [" + string.Join(", ", invalidBuffers)
                    + "] (Did you make sure to create them first?)");
            }

            // This method silently ignores values that are 0 or not valid buffer objects.
            _glDeleteBuffers((uint)buffers.Length, buffers);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while deleting an array of buffers. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Checks whether a given OpenGL object is a buffer.
        /// </summary>
        /// <param name="objectId">The OpenGL object ID to check. If this is zero,
        /// this function will *always* return false.</param>
        /// <returns>Whether or not the OpenGL object was a buffer.</returns>
        public bool IsBuffer(uint objectId) => _glIsBuffer(objectId);

        #endregion

        #region Vertex attributes

        /// <summary>
        /// Enable a given OpenGL vertex attribute array at `index` using the currently bound
        /// Vertex Array Object.
        /// </summary>
        /// <param name="index">The index of the vertex attribute array to enable. Values over 16 are not supported.</param>
        public void EnableVertexAttribArray(uint index)
        {
            // TODO: Check index against GL_MAX_VERTEX_ATTRIBUTES
            // That will require glGet functionality.
            // 16 is chosen b/c this is the minimum that can be reasonably expected. Virtually no system supports less,
            // and many don't support more either.
            // Only reason it's not a named constant here is because it'll be replaced by a glGet call later.
            if (index > 16)
            {
                Debug.LogError($"Cannot enable vertex attribute array at index '{index}'; indices over 16 are unsupported.");
                return;
            }

            _glEnableVertexAttribArray(index);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to enable the vertex attribute array at index '{index}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION => "This can be caused if no vertex array object is currently bound.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        /// <summary>
        /// Enable a given OpenGL vertex attribute array at `index`, using the Vertex Attribute Object
        /// specified by `vaobj`.
        /// </summary>
        /// <param name="vaobj">The Vertex Attribute Object to use.</param>
        /// <param name="index">The index of the vertex attribute array to enable. Values over 16 are not supported.</param>
        public void EnableVertexAttribArray(uint vaobj, uint index)
        {
            if (!IsVertexArray(vaobj))
            {
                Debug.LogError($"Cannot enable vertex attribute array '{vaobj}', as it is not a vertex array object.");
                return;
            }

            // TODO: Check index against GL_MAX_VERTEX_ATTRIBUTES
            // That will require glGet functionality.
            // 16 is chosen b/c this is the minimum that can be reasonably expected. Virtually no system supports less,
            // and many don't support more either.
            // Only reason it's not a named constant here is because it'll be replaced by a glGet call later.
            if (index > 16)
            {
                Debug.LogError($"Cannot enable vertex attribute array at index '{index}'; indices over 16 are unsupported.");
                return;
            }

            _glEnableVertexArrayAttrib(vaobj, index);
            GetError().LogIfError(e => $"Encountered OpenGL error `{e}` while attempting to enable the vertex attribute array "
                                  + $"at index '{index}' This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Define the layout of the data for a given vertex attribute array.
        /// </summary>
        /// <param name="index">The index of the vertex attribute array to define the data layout for.</param>
        /// <param name="size">Specifies the number of components per vertex attribute. Must be 1, 2, 3, or 4.</param>
        /// <param name="type">The data type of each component in the array.</param>
        /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
        /// <param name="pointer">
        /// Specifies the offset of the first component of the first vertex attribute in the array in the data store
        /// of the buffer currently bound to the `GL_ARRAY_BUFFER` target.
        /// </param>
        public void SetVertexAttributeDataLayout(uint index, int size, VertexDataType type, uint stride, IntPtr pointer)
        {
            if (index > 16)
            {
                Debug.LogError($"Cannot define vertex attribute array data layout at index '{index}'; "
                            + "indices over 16 are unsupported.");
                return;
            }
            if (size < 1 || size > 4)
            {
                Debug.LogError($"Cannot define vertex attribute array data layout at index '{index}'; "
                            + "`size` can only be 1, 2, 3, or 4.");
                return;
            }

            // VertexAttribPointer (for floats) is hanbdled in an overload, due to the `normalized` argument.
            if (type == VertexDataType.DOUBLE)
                _glVertexAttribLPointer(index, size, type, stride, pointer);
            else if (type != VertexDataType.FLOAT)
                _glVertexAttribIPointer(index, size, type, stride, pointer);
            else
                Debug.LogError("Cannot set vertex attribute data layouts for single-precision floating point types "
                    + "without a `normalized` boolean value being provided; an overload is available for this purpose.");

            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to define the data layout for the vertex attribute "
                    + $"array at index '{index}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION =>
                            "This can be caused if no buffer object is currently bound and `pointer` was not NULL.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        // TODO: Gotta revisit the docs for `normalized` once I understand it more, make sure I can't word it better.
        /// <summary>
        /// Define the layout of the float data for a given vertex attribute array.
        /// </summary>
        /// <param name="index">The index of the vertex attribute array to define the data layout for.</param>
        /// <param name="size">Specifies the number of components per vertex attribute. Must be 1, 2, 3, or 4.</param>
        /// <param name="normalized">
        /// Specifies whether fixed-point data values should be normalized or converted directly as fixed-point values
        /// when accessed.
        /// </param>
        /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
        /// <param name="pointer">
        /// Specifies the offset of the first component of the first vertex attribute in the array in the data store
        /// of the buffer currently bound to the `GL_ARRAY_BUFFER` target.
        /// </param>
        public void SetVertexAttributeDataLayout(
            uint index,
            int size,
            bool normalized,
            uint stride,
            IntPtr pointer)
        {
            if (index > 16)
            {
                Debug.LogError($"Cannot define vertex attribute array data layout at index '{index}'; "
                            + "indices over 16 are unsupported.");
                return;
            }
            if (size < 1 || size > 4)
            {
                Debug.LogError($"Cannot define vertex attribute array data layout at index '{index}'; "
                            + "`size` can only be 1, 2, 3, or 4. (`GL_BGRA` is unsupported.)");
                return;
            }

            _glVertexAttribPointer(index, size, VertexDataType.FLOAT, normalized, stride, pointer);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to define the data layout for the vertex attribute "
                    + $"array at index '{index}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION =>
                            "This can be caused if no buffer object is currently bound and `pointer` was not NULL.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        /// <summary>
        /// Disable a given OpenGL vertex attribute array at `index` using the currently bound
        /// Vertex Array Object.
        /// </summary>
        /// <param name="index">The index of the vertex attribute array to disable. Values over 16 are not supported.</param>
        public void DisableVertexAttribArray(uint index)
        {
            // TODO: Check index against GL_MAX_VERTEX_ATTRIBUTES
            // That will require glGet functionality.
            // 16 is chosen b/c this is the minimum that can be reasonably expected. Virtually no system supports less,
            // and many don't support more either.
            // Only reason it's not a named constant here is because it'll be replaced by a glGet call later.
            if (index > 16)
            {
                Debug.LogError($"Cannot disable vertex attribute array at index '{index}'; indices over 16 are unsupported.");
                return;
            }

            _glDisableVertexAttribArray(index);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to disable the vertex attribute array at index '{index}'. "
                    + e switch
                    {
                        ErrorCode.INVALID_OPERATION => "This can be caused if no vertex array object is currently bound.",
                        _ => "This should be impossible; perhaps an earlier error went uncaught?",
                    }
            );
        }

        /// <summary>
        /// Disable a given OpenGL vertex attribute array at `index`, using the Vertex Attribute Object
        /// specified by `vaobj`.
        /// </summary>
        /// <param name="vaobj">The Vertex Attribute Object to use.</param>
        /// <param name="index">The index of the vertex attribute array to disable. Values over 16 are not supported.</param>
        public void DisableVertexAttribArray(uint vaobj, uint index)
        {
            if (!IsVertexArray(vaobj))
            {
                Debug.LogError($"Cannot disable vertex attribute array '{vaobj}', as it is not a vertex array object.");
                return;
            }

            // TODO: Check index against GL_MAX_VERTEX_ATTRIBUTES
            // That will require glGet functionality.
            // 16 is chosen b/c this is the minimum that can be reasonably expected. Virtually no system supports less,
            // and many don't support more either.
            // Only reason it's not a named constant here is because it'll be replaced by a glGet call later.
            if (index > 16)
            {
                Debug.LogError($"Cannot disable vertex attribute array at index '{index}'; indices over 16 are unsupported.");
                return;
            }

            _glDisableVertexArrayAttrib(vaobj, index);
            GetError().LogIfError(e => $"Encountered OpenGL error `{e}` while attempting to disable the vertex attribute array "
                                  + $"at index '{index}' This should be impossible; perhaps an earlier error went uncaught?");
        }

        #endregion

        #region Vertex Arrays

        // TODO: Update the createObjects = true variant of this method to only work in 4.5+ contexts
        /// <summary>
        /// Generate identifiers for one or more OpenGL Vertex Array Objects and store them in an array.
        /// 
        /// If this Renderer is using a 4.5 or higher OpenGL context, this method can also
        /// create VAOs to fill these identifiers at the same time, provided `createObjects` is true.
        /// </summary>
        /// <param name="amount">The amount of names to generate.</param>
        /// <param name="createObjects">
        /// If this is true, Vertex Array Objects will also be created to fill the generated identifiers.
        /// </param>
        /// <returns>An array containing the generated Vertex Array Object identifiers.
        /// No objects will be created with these identifiers if `createObjects` was false.</returns>
        public uint[] GenVertexArrays(uint amount, bool createObjects = false)
        {
            uint[] vaos = new uint[amount];
            if (!createObjects)
                _glGenVertexArrays(amount, vaos);
            else
                _glCreateVertexArrays(amount, vaos);
            return vaos;
        }

        /// <summary>
        /// Binds a given OpenGL Vertex Array Object identifier, creating an object for it if necessary.
        /// </summary>
        /// <param name="array">The Vertex Array Object identifier to bind.</param>
        public void BindVertexArray(uint array)
        {
            // TODO: Need wrapper type for safety b/c IsVertexArray won't work if the name was generated
            // but the object not created (which you can only completely avoid in 4.5+ contexts)
            _glBindVertexArray(array);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while binding VAO '{array}'. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Unbinds any Vertex Array Object currently bound to this OpenGL context.
        /// </summary>
        public void UnbindVertexArray() => BindVertexArray(0);

        /// <summary>
        /// Deletes each OpenGL Vertex Array Object in a given array of them.
        /// </summary>
        /// <param name="vaos">An array of Vertex Array Objects to delete.</param>
        public void DeleteVertexArrays(uint[] vaos)
        {
            uint[] invalidVAOs = vaos.Where(item => !IsVertexArray(item)).ToArray();
            if (invalidVAOs.Length > 0)
            {
                Debug.LogWarning("Cannot delete some elements of an array of VAOs, as some were not valid VAOs "
                    + $"(other elements of the array will still be deleted): [" + string.Join(", ", invalidVAOs)
                    + "] (Did you make sure to create them first?)");
            }

            _glDeleteVertexArrays((uint)vaos.Length, vaos);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while deleting an array of VAOs. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        // TODO: All the IsXYZ functions could probably be rendered unnecessary by refactoring the functions
        // that use them to instead require wrapper types? Idk. Investigate this.
        // HAHA GET IT? RENDERED? HA HA HA HA HA
        /// <summary>
        /// Checks whether a given OpenGL object is a vertex array.
        /// </summary>
        /// <param name="objectId">The OpenGL object ID to check. If this is zero,
        /// this function will *always* return false.</param>
        /// <returns>Whether or not the OpenGL object was a vertex array.</returns>
        public bool IsVertexArray(uint objectId) => _glIsVertexArray(objectId);

        #endregion

        /// <summary>
        /// Clear the given OpenGL buffers and reset them to preset values.
        /// </summary>
        /// <param name="mask">A bitmask of the buffers to clear.</param>
        public void Clear(ClearMask mask)
        {
            _glClear(mask);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while clearing buffer bit(s). "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Set the clear values for the OpenGL color buffers.
        /// 
        /// When `GL_COLOR_BUFFER_BIT` is cleared using `GL.Clear`, it will be set to these values.
        /// </summary>
        /// <param name="red">The red value to use.</param>
        /// <param name="green">The green value to use.</param>
        /// <param name="blue">The blue value to use.</param>
        /// <param name="alpha">The alpha value to use.</param>
        public void ClearColor(float red, float green, float blue, float alpha)
        {
            _glClearColor(red, green, blue, alpha);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while setting the OpenGL clear color. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
        }

        /// <summary>
        /// Render primitives using data pre-specified via buffer objects in connection with vertex attributes.
        /// </summary>
        /// <param name="mode">The rendering mode to use.</param>
        /// <param name="start">The index to start at in the vertex data array.</param>
        /// <param name="count">The amount of vertices to read from after (and including) `start`.</param>
        public void DrawArrays(DrawMode mode, int start, uint count)
        {
            _glDrawArrays(mode, start, count);
            GetError().LogIfError(
                e => $"Encountered OpenGL error `{e}` while attempting to use glDrawArrays using mode `{mode}`. " + e switch
                {
                    ErrorCode.INVALID_OPERATION
                        => "\nThis can be caused if the data store of a buffer object bound to an enabled array "
                        + "is currently mapped, or if a geometry shader is active and the abovementioned mode is "
                        + "incompatible with its input primitive type. Lastly, this can also occur if no VAO was used, as "
                        + "VAOs are non-optional in strict OpenGL contexts.",
                    _ => "This should be impossible; perhaps an earlier error went uncaught?",
                }
            );
        }

        /// <summary>
        /// Sets the dimensions of the OpenGL viewport for this context.
        /// </summary>
        /// <param name="x">The X-axis position of the lower-left corner of the viewport.</param>
        /// <param name="y">The Y-axis position of the lower-left corner of the viewport.</param>
        /// <param name="width">The width of the viewport.</param>
        /// <param name="height">The height of the viewport.</param>
        public void SetViewportDimensions(int x, int y, uint width, uint height)
        {
            _glViewport(x, y, width, height);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while setting the OpenGL viewport "
                                  + "dimensions. This should be impossible; perhaps an earlier error went uncaught?");
        }

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
        private T GetDelegate<T>(string funcName = null) where T : Delegate
        {
            if (string.IsNullOrWhiteSpace(funcName)) funcName = typeof(T).Name.Replace("_", "");
            return Marshal.GetDelegateForFunctionPointer<T>(SDL_GL_GetProcAddress(funcName));
        }

        #region Debugging and querying

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ErrorCode glGetError();
        private readonly glGetError _glGetError;

        // This also serves as glGetStringi since `index` appears to go unused if `name` isn't GL_EXTENSIONS.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr glGetString(ConnectionInfo name, uint index = 0);
        private readonly glGetString _glGetString;

        // TODO: glGet functionality. That'll probably take a while, since there's like ten of them
        // and they have absolute shitloads of accepted constants, of which all delegates can query
        // but which are usually meant for a specific one.
        // (i.e., it's totally valid to query an integer with glGetBooleanv, but it'll be converted to a boolean.
        // and you should be using glGetIntegerv.)

        #endregion

        #region Shaders & Programs

        // errors due to the fact that on windows most OpenGL pointers cant be loaded outside of the current context
        // and those pointers will be specific TO that context
        // So, probably move all this to Rendering and make it nonstatic.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint glCreateShader(ShaderType type);
        private readonly glCreateShader _glCreateShader;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glShaderSource(uint shader, uint count, string[] strings, int[] length);
        private readonly glShaderSource _glShaderSource;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glCompileShader(uint shader);
        private readonly glCompileShader _glCompileShader;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetShaderiv(uint shader, ShaderParams pname, out int @params);
        private readonly glGetShaderiv _glGetShaderiv;

        #region Explanation for [Out] StringBuilder
        // The function this calls needs a mutable, pre-initialized character array to fill with data.
        // Hence, a StringBuilder must be used, as String is both immutable and cannot be created with a base capacity.
        // Because the StringBuilder must be pre-initialized with sufficient capacity, an 'out' keyword cannot be used,
        // as it allows for uninitialized instances to be passed. [Out] is not strictly necessary, but is used anyways
        // as StringBuilder (unlike other reference types) defaults to [In, Out], which is unnecessary due to
        // its contents being set, but not retrieved, by this function.
        // See this StackOveflow post for details on the In/Out attributes vs. their respective keywords:
        // https://stackoverflow.com/questions/56097222/keywords-in-out-ref-vs-attributes-in-out-in-out
        #endregion
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetShaderInfoLog(uint shader, uint maxLength, out uint length, [Out] StringBuilder infoLog);
        private readonly glGetShaderInfoLog _glGetShaderInfoLog;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glAttachShader(uint program, uint shader);
        private readonly glAttachShader _glAttachShader;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDetachShader(uint program, uint shader);
        private readonly glDetachShader _glDetachShader;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDeleteShader(uint shader);
        private readonly glDeleteShader _glDeleteShader;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool glIsShader(uint shader);
        private readonly glIsShader _glIsShader;

        // Programs

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint glCreateProgram();
        private readonly glCreateProgram _glCreateProgram;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glLinkProgram(uint program);
        private readonly glLinkProgram _glLinkProgram;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetProgramiv(uint program, ProgramParams pname, out int @params);
        private readonly glGetProgramiv _glGetProgramiv;

        // This uses a StringBuilder with [Out] for the same reasons as glGetShaderInfoLog.
        // See the comments there for details.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetProgramInfoLog(uint program, uint maxLength, out uint length, [Out] StringBuilder infoLog);
        private readonly glGetProgramInfoLog _glGetProgramInfoLog;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glUseProgram(uint program);
        private readonly glUseProgram _glUseProgram;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDeleteProgram(uint program);
        private readonly glDeleteProgram _glDeleteProgram;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool glIsProgram(uint program);
        private readonly glIsProgram _glIsProgram;

        #endregion

        #region Buffers

        // Once again, [Out] is used instead of 'out' as the buffer array must have already been initialized.
        // ('ref' is not used because glGenBuffers does not read data from this array)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGenBuffers(uint n, [Out] uint[] buffers);
        private readonly glGenBuffers _glGenBuffers;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glCreateBuffers(uint n, [Out] uint[] buffers);
        private readonly glCreateBuffers _glCreateBuffers;

        // TODO: If I write a buffer object wrapper type, maybe I can find a way to make a inner type
        // that implements IDisposable, such that you can bind it via 'using' blocks only, or through other
        // funcs that do that for you? That way you can safely assume buffer objects will always be unbound afterward.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glBindBuffer(BufferTarget target, uint buffer);
        private readonly glBindBuffer _glBindBuffer;

        // object[] doesn't work here TODO:
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glBufferData(BufferTarget target, int size, float[] data, BufferUsageHint usage);
        private readonly glBufferData _glBufferData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glNamedBufferData(uint buffer, int size, object[] data, BufferUsageHint usage);
        private readonly glNamedBufferData _glNamedBufferData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDeleteBuffers(uint n, uint[] buffers);
        private readonly glDeleteBuffers _glDeleteBuffers;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool glIsBuffer(uint buffer);
        private readonly glIsBuffer _glIsBuffer;

        #endregion

        #region Vertex attributes

        // For reference, an attribute index is usually defined using layout(location = x) in GLSL.
        // That said, it's also possible not to specify them and instead later retrieve whatever
        // was automatically picked using glGetAttribLocation, although Vectoray does not currently support this.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glEnableVertexAttribArray(uint index);
        private readonly glEnableVertexAttribArray _glEnableVertexAttribArray;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glEnableVertexArrayAttrib(uint vaobj, uint index);
        private readonly glEnableVertexArrayAttrib _glEnableVertexArrayAttrib;

        // The difference between the below three functions is in the types the shader expects.
        // For single-precision floating point types, the regular VertexAttribPointer is used.
        // For integer types, VertexAttribIPointer is used.
        // Finally, for doubles (or types that use them such as dvec3), VertexAttribLPointer is used.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glVertexAttribPointer(
            uint index,
            int size,
            VertexDataType type,
            bool normalized,
            uint stride,
            IntPtr pointer
        );
        private readonly glVertexAttribPointer _glVertexAttribPointer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glVertexAttribIPointer(
            uint index,
            int size,
            VertexDataType type,
            uint stride,
            IntPtr pointer
        );
        private readonly glVertexAttribIPointer _glVertexAttribIPointer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glVertexAttribLPointer(
            uint index,
            int size,
            VertexDataType type,
            uint stride,
            IntPtr pointer
        );
        private readonly glVertexAttribLPointer _glVertexAttribLPointer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDisableVertexAttribArray(uint index);
        private readonly glDisableVertexAttribArray _glDisableVertexAttribArray;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDisableVertexArrayAttrib(uint vaobj, uint index);
        private readonly glDisableVertexArrayAttrib _glDisableVertexArrayAttrib;

        #endregion

        #region Vertex Arrays

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGenVertexArrays(uint n, [Out] uint[] arrays);
        private readonly glGenVertexArrays _glGenVertexArrays;

        // The difference between glGenXYZ and glCreateXYZ is that GenXYZ only creates an identifier that that type
        // of object can use - it doesn't create one. The latter, as you might guess, creates one to fill that identifier
        // as well. (Usually, glBindXYZ creates the objects when called if they don't exist)
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glCreateVertexArrays(uint n, [Out] uint[] arrays);
        private readonly glCreateVertexArrays _glCreateVertexArrays;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glBindVertexArray(uint array);
        private readonly glBindVertexArray _glBindVertexArray;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDeleteVertexArrays(uint n, uint[] buffers);
        private readonly glDeleteVertexArrays _glDeleteVertexArrays;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool glIsVertexArray(uint array);
        private readonly glIsVertexArray _glIsVertexArray;

        #endregion

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClear(ClearMask mask);
        private readonly glClear _glClear;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClearColor(float red, float green, float blue, float alpha);
        private readonly glClearColor _glClearColor;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDrawArrays(DrawMode mode, int first, uint count);
        private readonly glDrawArrays _glDrawArrays;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glViewport(int x, int y, uint width, uint height);
        private readonly glViewport _glViewport;

        #endregion
    }

    #region Exception definitions

    // TODO: Emmet snippets for these exceptions? Probably also see if there's a way to simplify
    // their definitions.
    /// <summary>
    /// Base exception type used by the `Renderer` class for `Result` error types.
    /// </summary>
    public class RendererException : Exception
    {
        protected RendererException() : base() { }
        protected RendererException(string message) : base(message) { }
        protected RendererException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// An exception used to indicate that a function failed due to relevant OpenGL configuration
    /// attributes not having been set.
    /// </summary>
    public class GLAttributesNotSetException : RendererException
    {
        public GLAttributesNotSetException(string message) : base(message) { }
        public GLAttributesNotSetException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// An exception used to indicate that SDL2 has failed to create an OpenGL context.
    /// </summary>
    public class ContextCreationFailedException : RendererException
    {
        public ContextCreationFailedException(string message) : base(message) { }
        public ContextCreationFailedException(string message, Exception inner) : base(message, inner) { }
    }

    #endregion
}