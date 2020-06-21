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
using System.Runtime.InteropServices;
using System.Text;

using Vectoray.Rendering.OpenGL;

using static SDL2.SDL;

namespace Vectoray.Rendering
{
    public partial class Renderer
    {
        /// <summary>
        /// Shorthand call for GetDelegateForFunctionPointer using SDL_GL_GetProcAddress.
        /// </summary>
        /// <typeparam name="T">Function delegate type to get and return.</typeparam>
        /// <param name="funcName">Name of the function for GetProcAddress, if it's different from the type name.
        /// 
        /// If not given, this will be set to the type name of T</param>
        /// <returns>Delegate for the function.</returns>
        private T GetDelegate<T>(string funcName = null) where T : Delegate
        {
            if (string.IsNullOrWhiteSpace(funcName)) funcName = typeof(T).Name;
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetBooleanv(BooleanQuery pname, [Out] bool[] data);
        private readonly glGetBooleanv _glGetBooleanv;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetFloatv(FloatQuery pname, [Out] float[] data);
        private readonly glGetFloatv _glGetFloatv;

        // TODO: What about uints? Are there ANY negative values returned from these?
        // If not, just return a uint.
        // Otherwise, create glGetUnsignedIntegerv and glGetUnsignedInteger64v
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetIntegerv(IntegerQuery pname, [Out] int[] data);
        private readonly glGetIntegerv _glGetIntegerv;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetInteger64v(IntegerQuery64 pname, [Out] long[] data);
        private readonly glGetInteger64v _glGetInteger64v;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetBooleani_v(IndexedBooleanQuery target, uint index, [Out] bool[] data);
        private readonly glGetBooleani_v _glGetBooleani_v;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetFloati_v(IndexedFloatQuery target, uint index, [Out] float[] data);
        private readonly glGetFloati_v _glGetFloati_v;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetIntegeri_v(IndexedIntegerQuery target, uint index, [Out] int[] data);
        private readonly glGetIntegeri_v _glGetIntegeri_v;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glGetInteger64i_v(IndexedIntegerQuery64 target, uint index, [Out] long[] data);
        private readonly glGetInteger64i_v _glGetInteger64i_v;

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
        private delegate void glBufferData(BufferTarget target, int size, IntPtr data, BufferUsageHint usage);
        private readonly glBufferData _glBufferData;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void glNamedBufferData(uint buffer, int size, IntPtr data, BufferUsageHint usage);
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
    }
}