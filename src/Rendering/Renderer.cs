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

        #endregion

        #region Lifecycle functionality (constructors, finalizers, etc)

        /// <summary>
        /// Create a new Renderer with the given pointers.
        /// </summary>
        private Renderer(IntPtr context, IntPtr window)
        {
            (this.context, this.window) = (context, window);

            #region OpenGL function loading

            _glGetError = GetDelegate<glGetError>();
            _glGetString = GetDelegate<glGetString>();
            _glGetStringi = GetDelegate<glGetStringi>();

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
            _glDeleteProgram = GetDelegate<glDeleteProgram>();
            _glIsProgram = GetDelegate<glIsProgram>();

            _glClear = GetDelegate<glClear>();
            _glClearColor = GetDelegate<glClearColor>();

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
        public string GetString(GLConnectionInfo name)
        {
            if (name != GLConnectionInfo.EXTENSIONS) return Marshal.PtrToStringAnsi(_glGetString(name));
            else Debug.LogError("Cannot read a GL_EXTENSIONS string with 'GetString(GLConnectionInfo name)'. "
                              + "(Did you mean to use 'GetExtensionString(int index)'?)");
            return string.Empty;
        }

        /// <summary>
        /// Get an extension string supported by the OpenGL implementation at `index`.
        /// </summary>
        /// <param name="index">The index of the string to return.</param>
        /// <returns>The string at `index`.</returns>
        public Opt<string> GetExtensionString(int index)
        {
            string str = Marshal.PtrToStringAnsi(_glGetStringi(GLConnectionInfo.EXTENSIONS, index));
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
                $" | Vendor: {GetString(GLConnectionInfo.VENDOR)}\n"
              + $" | Renderer: {GetString(GLConnectionInfo.RENDERER)}\n"
              + $" | Version: {GetString(GLConnectionInfo.VERSION)}\n"
              + $"<| GLSL version: {GetString(GLConnectionInfo.SHADING_LANGUAGE_VERSION)}"
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
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while setting shader source. "
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
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while compiling a shader. "
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
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while querying shader parameter `{param}`. "
                                      + "This should be impossible; perhaps an earlier error went uncaught?");
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

            _glGetShaderiv(shader, ShaderParams.INFO_LOG_LENGTH, out int value);
            _glGetShaderInfoLog(shader, (uint)value, out _, out string infoLog);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while getting a shader's info log. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
            return infoLog.Some();
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
                e => $"Encountered unexpected OpenGL error `{e}` while linking a shader program. " + e switch
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
                e => $"Encountered OpenGL error `{e}` while querying a program parameter `{param}`. " + e switch
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

            _glGetProgramiv(program, ProgramParams.INFO_LOG_LENGTH, out int value);
            _glGetProgramInfoLog(program, (uint)value, out _, out string infoLog);
            GetError().LogIfError(e => $"Encountered unexpected OpenGL error `{e}` while getting a program's info log. "
                                  + "This should be impossible; perhaps an earlier error went uncaught?");
            return infoLog.Some();
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

        /// <summary>
        /// Clear the given OpenGL buffers and reset them to preset values.
        /// </summary>
        /// <param name="mask">A bitmask of the buffers to clear.</param>
        public void Clear(GLClearMask mask) => _glClear(mask);

        /// <summary>
        /// Set the clear values for the OpenGL color buffers.
        /// 
        /// When `GL_COLOR_BUFFER_BIT` is cleared using `GL.Clear`, it will be set to these values.
        /// </summary>
        /// <param name="red">The red value to use.</param>
        /// <param name="green">The green value to use.</param>
        /// <param name="blue">The blue value to use.</param>
        /// <param name="alpha">The alpha value to use.</param>
        public void ClearColor(float red, float green, float blue, float alpha) =>
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
        private T GetDelegate<T>(string funcName = null) where T : Delegate
        {
            if (string.IsNullOrWhiteSpace(funcName)) funcName = typeof(T).Name.Replace("_", "");
            return Marshal.GetDelegateForFunctionPointer<T>(SDL_GL_GetProcAddress(funcName));
        }

        #region Debugging

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate ErrorCode glGetError();
        private readonly glGetError _glGetError;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr glGetString(GLConnectionInfo name);
        private readonly glGetString _glGetString;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr glGetStringi(GLConnectionInfo name, int index);
        // glGetString includes glGetStringi, but delegates cannot be overloaded,
        // and the delegates cannot be combined because glGetStringi only accepts one type of
        // GLConnectionInfo value.
        private readonly glGetStringi _glGetStringi;

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

        // Previous versions of this program (prior to the total rewrite) used StdCall for most OpenGL functions,
        // and ended up having to use [MarshalAs(UnmanagedType.LPStr)] on the infoLog parameter.
        // Cdecl should prevent MarshalAs or StringBuilder from being necessary (citation needed),
        // TODO: test the above?
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetShaderInfoLog(uint shader, uint maxLength, out uint length, out string infoLog);
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetProgramInfoLog(uint program, uint maxLength, out uint length, out string infoLog);
        private readonly glGetProgramInfoLog _glGetProgramInfoLog;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glDeleteProgram(uint program);
        private readonly glDeleteProgram _glDeleteProgram;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool glIsProgram(uint program);
        private readonly glIsProgram _glIsProgram;

        #endregion

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClear(GLClearMask mask);
        private readonly glClear _glClear;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glClearColor(float red, float green, float blue, float alpha);
        private readonly glClearColor _glClearColor;

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