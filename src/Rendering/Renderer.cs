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

    public partial class Renderer
    {
        #region Variable & property declaration

        private const int GL_ERROR_TYPES = 7;

        private readonly IntPtr context;
        private readonly IntPtr window;

        /// <summary>
        /// The version of OpenGL this renderer's inner OpenGL context supports.
        /// </summary>
        public readonly GLVersion contextVersion;

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
            // See Rendering/Interop.cs for delegate definitions.
            _glGetError = GetDelegate<glGetError>();
            _glGetString = GetDelegate<glGetString>();

            _glGetBooleanv = GetDelegate<glGetBooleanv>();
            _glGetFloatv = GetDelegate<glGetFloatv>();
            _glGetIntegerv = GetDelegate<glGetIntegerv>();
            _glGetInteger64v = GetDelegate<glGetInteger64v>();

            _glGetBooleani_v = GetDelegate<glGetBooleani_v>();
            _glGetFloati_v = GetDelegate<glGetFloati_v>();
            _glGetIntegeri_v = GetDelegate<glGetIntegeri_v>();
            _glGetInteger64i_v = GetDelegate<glGetInteger64i_v>();

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
        /// </summary>
        /// <param name="amount">The amount of identifiers to generate.</param>
        /// <returns>An array containing the generated buffer object identifiers.</returns>
        public uint[] GenBuffers(uint amount)
        {
            uint[] buffers = new uint[amount];
            _glGenBuffers(amount, buffers);
            return buffers;
        }

        /// <summary>
        /// Generates an identifier for an OpenGL buffer object.
        /// </summary>
        /// <returns>The buffer object identifier generated.</returns>
        public uint GenBuffer() => GenBuffers(1)[0];

        /// <summary>
        /// Generate identifiers for and then initialize one or more OpenGL buffer objects and store them in
        /// an array. Cannot be used if this renderer does not support OpenGL 4.5 or higher.
        /// </summary>
        /// <param name="amount">The amount of buffer objects to create.</param>
        /// <returns>A `Some` wrapping an array containing the generated buffer object identifiers, unless
        /// OpenGL 4.5 or higher is unsupported, in which case a `None` instance is returned instead.</returns>
        public Opt<uint[]> CreateBuffers(uint amount)
        {
            if (contextVersion < GLVersion.GL_4_5)
            {
                Debug.LogError("Cannot call Renderer.CreateBuffers when the OpenGL context it uses precedes "
                    + $"OpenGL 4.5. (current version is {contextVersion.AsString()})");
                return new None<uint[]>();
            }

            uint[] buffers = new uint[amount];
            _glCreateBuffers(amount, buffers);
            return buffers;
        }

        /// <summary>
        /// Generates an identifier for and then initializes an OpenGL buffer object.
        /// Cannot be used if this renderer does not support OpenGL 4.5 or higher.
        /// </summary>
        /// <returns>A `Some` wrapping the generated buffer object identifier, unless
        /// OpenGL 4.5. or higher is unsupported, in which case a `None` instance is returned instead.</returns>
        public Opt<uint> CreateBuffer() => CreateBuffers(1).Map(item => item[0]);

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
        /// Delete a give OpenGL buffer.
        /// </summary>
        /// <param name="buffer">The buffer to delete.</param>
        public void DeleteBuffer(uint buffer)
        {
            if (!IsBuffer(buffer))
            {
                Debug.LogError($"Cannot delete buffer object '{buffer}' as it is not a valid OpenGL buffer object.");
                return;
            }

            _glDeleteBuffers(1, new[] { buffer });
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while deleting a buffer object. "
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

        /// <summary>
        /// Generate identifiers for one or more OpenGL vertex array objects and store them in an array.
        /// </summary>
        /// <param name="amount">The amount of identifiers to generate.</param>
        /// <returns>An array containing the generated vertex array object identifiers.</returns>
        public uint[] GenVertexArrays(uint amount)
        {
            uint[] vaos = new uint[amount];
            _glGenVertexArrays(amount, vaos);
            return vaos;
        }

        /// <summary>
        /// Generates an identifier for an OpenGL vertex array object.
        /// </summary>
        /// <returns>The vertex array object identifier generated.</returns>
        public uint GenVertexArray() => GenVertexArrays(1)[0];

        /// <summary>
        /// Generate identifiers for and then initialize one or more OpenGL vertex array objects and store them in
        /// an array. Cannot be used if this renderer does not support OpenGL 4.5 or higher.
        /// </summary>
        /// <param name="amount">The amount of vertex array objects to create.</param>
        /// <returns>A `Some` wrapping an array containing the generated vertex array object identifiers, unless
        /// OpenGL 4.5 or higher is unsupported, in which case a `None` instance is returned instead.</returns>
        public Opt<uint[]> CreateVertexArrays(uint amount)
        {
            if (contextVersion < GLVersion.GL_4_5)
            {
                Debug.LogError("Cannot call Renderer.CreateVertexArrays when the OpenGL context it uses precedes "
                    + $"OpenGL 4.5. (current version is {contextVersion.AsString()})");
                return new None<uint[]>();
            }

            uint[] vaos = new uint[amount];
            _glCreateVertexArrays(amount, vaos);
            return vaos;
        }

        /// <summary>
        /// Generates an identifier for and then initializes an OpenGL vertex array object.
        /// Cannot be used if this renderer does not support OpenGL 4.5 or higher.
        /// </summary>
        /// <returns>A `Some` wrapping the generated vertex array object identifier, unless
        /// OpenGL 4.5. or higher is unsupported, in which case a `None` instance is returned instead.</returns>
        public Opt<uint> CreateVertexArray() => CreateVertexArrays(1).Map(item => item[0]);

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