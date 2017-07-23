/*
Sculptition; 3D modeling program with an intuitive interface in mind.
Copyright (C) 2017 LukeZaz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Numerics;
using static SDL2.SDL;

using static Vectoray.Extensions;

namespace OpenGL
{
	/* Make-do GLEW bindings because I couldn't find any
	// Currently entirely unused. Kept until I am sure this won't be needed.
	/// <summary>
	/// C# bindings to GLEW functions. All function names will be identical wherever possible.
	/// Functions provided DO NOT include those straight from OpenGL. Use the OpenGL class for functions from it.
	/// </summary>
	static class GLEW
	{
		public const string nativeLibName = "glew32.dll";

		public enum GLEW_ErrorCode
		{
			GLEW_OK = 0,
			GLEW_NO_ERROR = 0,
			GLEW_ERROR_NO_GL_VERSION = 1,
			GLEW_ERROR_GL_VERSION_10_ONLY = 2,
			GLEW_ERROR_GLX_VERSION_11_ONLY = 3,
			GLEW_ERROR_NO_GLX_DISPLAY = 4
		}

		// For future reference: Most imports will require a entry point beginning with two underscores. Those that don't
		// will require an @ followed by a number, usually 4, at the end
		// Only glewExperimental has no underscores preceding it.
		// For an exact check as to what the entry point for something is, use Dependency Walker.

		[DllImport(nativeLibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "_glewInit@0")]
		private static extern uint _glewInit();

		public static GLEW_ErrorCode glewInit()
		{
			return (GLEW_ErrorCode)_glewInit();
		}

		[DllImport(nativeLibName, CallingConvention = CallingConvention.StdCall, EntryPoint = "_glewGetErrorString@4")]
		private static extern ushort _glewGetErrorString(uint error);

		public static string glewGetErrorString(GLEW_ErrorCode error)
		{
			ushort reportedError = _glewGetErrorString((uint)error);
			// TODO: Convert from integer representation to string
			return reportedError.ToString();
		}

		[DllImport(nativeLibName, CallingConvention = CallingConvention.StdCall, EntryPoint = "_glewIsSupported@4")]
		public static extern bool glewIsSupported(string checks);
	} */

	/// <summary>
	/// Functions obtained off OpenGL utilizing GetProcAddress.
	/// All names will be identical wherever possible.
	/// </summary>
	static class OpenGL
	{
		#region Type declaration

		public enum GLenum
		{
			GL_NO_ERROR =                           0,
			GL_DEPTH_BUFFER_BIT =          0x00000100,
			GL_COLOR_BUFFER_BIT =          0x00004000,
			GL_LINES =                         0x0001,
			GL_LINE_LOOP =                     0x0002,
			GL_LINE_STRIP =                    0x0003,
			GL_TRIANGLES =                     0x0004,
			GL_TRIANGLE_FAN =                  0x0006,
			GL_LESS =                          0x0201,
			GL_INVALID_ENUM =                  0x0500,
			GL_INVALID_VALUE =                 0x0501,
			GL_INVALID_OPERATION =             0x0502,
			GL_STACK_OVERFLOW =                0x0503,
			GL_STACK_UNDERFLOW =               0x0504,
			GL_OUT_OF_MEMORY =                 0x0505,
			GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506,
			GL_CULL_FACE =                     0x0B44,
			GL_UNSIGNED_INT =                  0x1405,
			GL_FLOAT =                         0x1406,
			GL_VENDOR =                        0x1F00,
			GL_RENDERER =                      0x1F01,
			GL_VERSION =                       0x1F02,
			GL_EXTENSIONS =                    0x1F03,
			GL_MULTISAMPLE =                   0x809D,
			GL_ARRAY_BUFFER =                  0x8892,
			GL_ELEMENT_ARRAY_BUFFER =          0x8893,
			GL_STATIC_DRAW =                   0x88E4,
			GL_DEPTH_TEST =                    0x0B71,
			GL_FRAGMENT_SHADER =               0x8B30,
			GL_VERTEX_SHADER =                 0x8B31,
			GL_COMPILE_STATUS =                0x8B81,
			GL_LINK_STATUS =                   0x8B82,
			GL_INFO_LOG_LENGTH =               0x8B84,
			GL_SHADING_LANGUAGE_VERSION =      0x8B8C
		}

		public enum GLboolean
		{
			GL_FALSE = 0,
			GL_TRUE = 1
		}

		// Alias for uint
		public struct GLuint
		{
			private uint _GLuint;

			public static implicit operator GLuint(uint i)
			{
				return new GLuint { _GLuint = i };
			}

			public static implicit operator GLuint(string s)
			{
				GLuint g = new GLuint { _GLuint = 0 };
				uint.TryParse(s, out g._GLuint);
				return g;
			}

			public static implicit operator GLuint(GLint g)
			{
				return new GLuint { _GLuint = (uint)(int)g };
			}

			public static implicit operator uint(GLuint g)
			{
				return g._GLuint;
			}
		}

		// Alias for int
		public struct GLint
		{
			private int _GLint;

			public static implicit operator GLint(int i)
			{
				return new GLint { _GLint = i };
			}

			public static implicit operator GLint(GLboolean g)
			{
				return new GLint { _GLint = (int)g };
			}

			public static implicit operator GLint(string s)
			{
				GLint g = new GLint { _GLint = 0 };
				int.TryParse(s, out g._GLint);
				return g;
			}

			public static implicit operator GLint(GLuint g)
			{
				return new GLint { _GLint = (int)(uint)g };
			}

			public static implicit operator int(GLint g)
			{
				return g._GLint;
			}

			public static implicit operator GLboolean(GLint g)
			{
				return (GLboolean)g._GLint;
			}
		}

		// Alias for float
		public struct GLfloat
		{
			private float _GLfloat;

			public static implicit operator GLfloat(float f)
			{
				return new GLfloat { _GLfloat = f };
			}

			public static implicit operator GLfloat(GLclampf g)
			{
				return new GLfloat { _GLfloat = g };
			}

			public static implicit operator float(GLfloat g)
			{
				return g._GLfloat;
			}
		}

		// Alias for float, clamped between 0 and 1 (inclusive)
		public struct GLclampf
		{
			private float glclampf;
			private float _GLclampf
			{
				get
				{
					return glclampf;
				}
				set
				{
					if (value > 1.0f) glclampf = 1.0f;
					else if (value < 0.0f) glclampf = 0.0f;
					else glclampf = value;
				}
			}

			public static implicit operator GLclampf(float f)
			{
				return new GLclampf { _GLclampf = f };
			}

			public static implicit operator GLclampf(GLfloat g)
			{
				return new GLclampf { _GLclampf = g };
			}

			public static implicit operator float(GLclampf g)
			{
				return g._GLclampf;
			}
		}

		#endregion

		/// <summary>
		/// Shorthand call for GetDelegateForFunctionPointer using SDL_GL_GetProcAddress.
		/// </summary>
		/// <typeparam name="T">Function type to get and return.</typeparam>
		/// <param name="funcName">Name of the function for GetProcAddress, if different from the type name.
		/// If not given, this will be set to the type name of T with underscores removed.</param>
		/// <returns>Delegate for the function.</returns>
		static T GetGLFunc<T>(string funcName = null) where T : class
		{
			if (string.IsNullOrEmpty(funcName)) funcName = typeof(T).Name.Replace("_", "");
			return Marshal.GetDelegateForFunctionPointer(SDL_GL_GetProcAddress(funcName), typeof(T)) as T;
		}

		/// <summary>
		/// Acquire and set all OpenGL functions.
		/// Must be run before any other functions in ths class can be used.
		/// </summary>
		/// <returns>Boolean value representing execution success.</returns>
		public static bool LoadGLFunctions()
		{
			bool success = true;

			glCreateProgram = GetGLFunc<_glCreateProgram>();
			glCreateShader = GetGLFunc<_glCreateShader>();
			glShaderSource = GetGLFunc<_glShaderSource>();
			glGetError = GetGLFunc<_glGetError>();

			glCompileShader = GetGLFunc<_glCompileShader>();
			glGetShaderiv = GetGLFunc<_glGetShaderiv>();
			glAttachShader = GetGLFunc<_glAttachShader>();
			glLinkProgram = GetGLFunc<_glLinkProgram>();

			glGetProgramiv = GetGLFunc<_glGetProgramiv>();
			glIsProgram = GetGLFunc<_glIsProgram>();
			glGetProgramInfoLog = GetGLFunc<_glGetProgramInfoLog>();
			glGetAttribLocation = GetGLFunc<_glGetAttribLocation>();

			glClearColor = GetGLFunc<_glClearColor>();
			glGenBuffers = GetGLFunc<_glGenBuffers>();
			glBindBuffer = GetGLFunc<_glBindBuffer>();
			glBufferDataf = GetGLFunc<_glBufferDataf>("glBufferData");

			glBufferDataui = GetGLFunc<_glBufferDataui>("glBufferData");
			glIsShader = GetGLFunc<_glIsShader>();
			glGetShaderInfoLog = GetGLFunc<_glGetShaderInfoLog>();
			glClear = GetGLFunc<_glClear>();

			glUseProgram = GetGLFunc<_glUseProgram>();
			glEnableVertexAttribArray = GetGLFunc<_glEnableVertexAttribArray>();
			glDisableVertexAttribArray = GetGLFunc<_glDisableVertexAttribArray>();
			glVertexAttribPointer = GetGLFunc<_glVertexAttribPointer>();

			glDrawElements = GetGLFunc<_glDrawElements>();
			glGetString = GetGLFunc<_glGetString>();
			glGenVertexArrays = GetGLFunc<_glGenVertexArrays>();
			glBindVertexArray = GetGLFunc<_glBindVertexArray>();

			glDrawArrays = GetGLFunc<_glDrawArrays>();
			glEnable = GetGLFunc<_glEnable>();
			glGetUniformLocation = GetGLFunc<_glGetUniformLocation>();
			glUniformMatrix4fv = GetGLFunc<_glUniformMatrix4fv>();

			glDepthFunc = GetGLFunc<_glDepthFunc>();
			glCullFace = GetGLFunc<_glCullFace>();

			GLenum glError = glGetError();
			if (glError != GLenum.GL_NO_ERROR)
			{
				ConsoleWriteError("Failed to load OpenGL functions! OpenGL error: " + glError.ToString());
				success = false;
			}

			return success;
		}

		#region GL function declaration

		// TODO: Organize this shit!
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate GLuint _glCreateProgram();
		public static _glCreateProgram glCreateProgram;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLuint _glCreateShader(GLenum shaderType);
		public static _glCreateShader glCreateShader;

		// TODO: Make GLsizei for 'count' variable
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glShaderSource(GLuint shader, uint count, string[] str, GLint[] length);
		public static _glShaderSource glShaderSource;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLenum _glGetError();
		public static _glGetError glGetError;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glCompileShader(GLuint shader);
		public static _glCompileShader glCompileShader;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGetShaderiv(GLuint shader, GLenum pname, out GLint prms);
		public static _glGetShaderiv glGetShaderiv;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glAttachShader(GLuint program, GLuint shader);
		public static _glAttachShader glAttachShader;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glLinkProgram(GLuint program);
		public static _glLinkProgram glLinkProgram;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGetProgramiv(GLuint program, GLenum pname, out GLint prms);
		public static _glGetProgramiv glGetProgramiv;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLboolean _glIsProgram(GLuint program);
		public static _glIsProgram glIsProgram;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGetProgramInfoLog(GLuint program, uint maxLength, out GLuint length,
			[MarshalAs(UnmanagedType.LPStr)] [Out()] StringBuilder infoLog);
		public static _glGetProgramInfoLog glGetProgramInfoLog;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLint _glGetAttribLocation(GLuint program,
			[MarshalAs(UnmanagedType.LPStr)] StringBuilder name);
		public static _glGetAttribLocation glGetAttribLocation;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glClearColor(GLclampf red, GLclampf blue, GLclampf green, GLclampf alpha);
		public static _glClearColor glClearColor;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGenBuffers(uint n, [Out] GLuint[] buffers);
		public static _glGenBuffers glGenBuffers;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glBindBuffer(GLenum target, GLuint buffer);
		public static _glBindBuffer glBindBuffer;

		// THERE HAS TO BE A BETTER WAY TO DO THIS
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glBufferDataf(GLenum target, GLint size, GLfloat[] data, GLenum usage);
		public static _glBufferDataf glBufferDataf;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glBufferDataui(GLenum target, GLint size, GLuint[] data, GLenum usage);
		public static _glBufferDataui glBufferDataui;
		// END THERE HAS TO BE A BETTER WAY TO DO THIS

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLboolean _glIsShader(GLuint shader);
		public static _glIsShader glIsShader;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGetShaderInfoLog(GLuint shader, uint maxLength, out GLuint length,
			[MarshalAs(UnmanagedType.LPStr)] [Out()] StringBuilder infoLog);
		public static _glGetShaderInfoLog glGetShaderInfoLog;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glClear(GLenum mask);
		public static _glClear glClear;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glUseProgram(GLuint program);
		public static _glUseProgram glUseProgram;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glEnableVertexAttribArray(GLuint index);
		public static _glEnableVertexAttribArray glEnableVertexAttribArray;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glDisableVertexAttribArray(GLuint index);
		public static _glDisableVertexAttribArray glDisableVertexAttribArray;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glVertexAttribPointer(GLuint index, GLint size, GLenum type, GLboolean normalized, uint stride, IntPtr pointer);
		public static _glVertexAttribPointer glVertexAttribPointer;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glDrawElements(GLenum mode, uint count, GLenum type, IntPtr indices);
		public static _glDrawElements glDrawElements;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate IntPtr _glGetString(GLenum name);
		/// <summary>
		/// Return a string describing the current GL connection.
		/// Use Marshal.PtrToStringAnsi to retrieve the returned string.
		/// </summary>
		/// <param name="name">Specifies a symbolic constant, one of GL_VENDOR, GL_RENDERER, GL_VERSION, GL_SHADING_LANGUAGE_VERSION, or GL_EXTENSIONS.</param>
		/// <returns>Pointer to returned string.</returns>
		public static _glGetString glGetString;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glGenVertexArrays(uint n, [Out] GLuint[] arrays);
		public static _glGenVertexArrays glGenVertexArrays;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glBindVertexArray(GLuint array);
		public static _glBindVertexArray glBindVertexArray;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glDrawArrays(GLenum mode, GLint first, uint count);
		public static _glDrawArrays glDrawArrays;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glEnable(GLenum cap);
		public static _glEnable glEnable;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate GLint _glGetUniformLocation(GLuint program,
			[MarshalAs(UnmanagedType.LPStr)] StringBuilder name);
		public static _glGetUniformLocation glGetUniformLocation;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void _glUniformMatrix4fv(GLint location, uint count, GLboolean transpose, float* value);
		public static _glUniformMatrix4fv glUniformMatrix4fv;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glDepthFunc(GLenum func);
		public static _glDepthFunc glDepthFunc;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void _glCullFace(GLenum mode);
		public static _glCullFace glCullFace;

		[DllImport("OpenGL32.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr wglGetCurrentContext();

		#endregion
	}

	namespace CSharpWrapper
	{
		/* Idea for experimental new layout, akin to that used by Unity
		   'WorldObject' class, which is just a basis - essentially a position, and host for other classes.
		   It can, from there, support other shapes. Like Unity, this will include a differentiation between
		   World Space and Local Space, with WorldObject existing primarily to easily organize all local data (such as local position).
		   This might not be a good idea, but the general premise sounds promising, and so I will be trying it. */
		
		// TODO: WorldObject-based objects need color handling.

		/// <summary>
		/// A two-value enum used to define the spacial context in which something is done.
		/// Actions can be relative to the given object, or the entire world; this is used for determining that.
		/// </summary>
		public enum SpacialContext
		{
			World = 0,
			Local = 1
		}
		
		/// <summary>
		/// A basic object class. Has nothing besides a position.
		/// </summary>
		public class WorldObject
		{
			public Vector3 position;

			/// <summary>
			/// Create a new WorldObject. Position will default to Vector3.Zero.
			/// </summary>
			public WorldObject()
			{
				position = Vector3.Zero;
			}

			/// <summary>
			/// Create a new WorldObject with the given position.
			/// </summary>
			/// <param name="_position">The position for the new WorldObject.</param>
			public WorldObject(Vector3 _position)
			{
				if (_position == null)
				{
					// TODO: There might be a way to replace the 'WorldObject' text in this with the name of whatever class (derived or otherwise) it was that was actually created
					ConsoleWriteWarning("WorldObject created with null position variable. Defaulting to Vector3.Zero.");
					_position = Vector3.Zero;
				}

				position = _position;
			}
		}

		/// <summary>
		/// A camera object used to view the model and world around it. Only one should be active at any given time.
		/// </summary>
		public class Camera : WorldObject
		{
			public float horizontalAngle, verticalAngle, fieldOfView;

			/// <summary>
			/// Initializes a new camera with the default values of: Horizontal Angle 3.14, Vertical Angle 0, and Field of View (FoV, in degrees) 75.
			/// </summary>
			public Camera() : base()
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				fieldOfView = 75;
			}

			/// <summary>
			/// Initializes a new camera with a default Horizontal Angle of 3.14, a default Vertical Angle of 0, and a provided Field of View.
			/// </summary>
			/// <param name="_fieldOfView">The field of view (FoV) for this camera, in degrees.</param>
			public Camera(float _fieldOfView) : base()
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				fieldOfView = _fieldOfView;
			}

			/// <summary>
			/// Initializes a new camera with a default Horizontal Angle of 3.14, a default Vertical Angle of 0, and a provided Field of View and position.
			/// </summary>
			/// <param name="_position">The starting position for this camera.</param>
			/// <param name="_fieldOfView">The field of view (FoV) for this camera, in degrees. If not provided, defaults to 75.</param>
			public Camera(Vector3 _position, float _fieldOfView = 75) : base(_position)
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				fieldOfView = _fieldOfView;
			}

			/// <summary>
			/// Gets the direction of the camera in Cartesian coordinates.
			/// </summary>
			/// <returns>The direction of this camera in Cartesian coordinates.</returns>
			public Vector3 GetForwardDirection()
			{
				return new Vector3((float)(Math.Cos(verticalAngle) * Math.Sin(horizontalAngle)),
								   (float)Math.Sin(verticalAngle),
								   (float)(Math.Cos(verticalAngle) * Math.Cos(horizontalAngle)));
			}

			/// <summary>
			/// Gets the direction to the right of the camera.
			/// </summary>
			/// <returns>The direction to the right of the camera.</returns>
			public Vector3 GetRightDirection()
			{
				return new Vector3((float)Math.Sin(horizontalAngle - Math.PI / 2),
								   0,
								   (float)Math.Cos(horizontalAngle - Math.PI / 2));
			}

			/// <summary>
			/// Get this cameras upwards direction.
			/// </summary>
			/// <returns>The upwards direction for this camera.</returns>
			public Vector3 GetUpDirection()
			{
				return Vector3.Cross(GetRightDirection(), GetForwardDirection());
			}
		}

		/// <summary>
		/// An object composed of a single triangle.
		/// </summary>
		public class Triangle : WorldObject
		{
			// Points are stored in a Local Space context
			public Vector3 A, B, C;

			public Triangle() : base()
			{
				A = new Vector3();
				B = new Vector3();
				C = new Vector3();
			}

			// TODO: This function might not be necessary? Double check.
			/// <summary>
			/// Create a new triangle from the given points. Object position will default to Vector3.Zero.
			/// </summary>
			/// <param name="firstPoint">The first point.</param>
			/// <param name="secondPoint">The second point.</param>
			/// <param name="thirdPoint">The third point.</param>
			public Triangle(Vector3 firstPoint, Vector3 secondPoint, Vector3 thirdPoint) : base()
			{
				A = firstPoint;
				B = secondPoint;
				C = thirdPoint;
			}

			/// <summary>
			/// Create a new triangle from the given points and position.
			/// </summary>
			/// <param name="firstPoint">The first point.</param>
			/// <param name="secondPoint">The second point.</param>
			/// <param name="thirdPoint">The third point.</param>
			/// <param name="_position">Overall position of this triangle which the points will be relative to.</param>
			public Triangle(Vector3 firstPoint, Vector3 secondPoint, Vector3 thirdPoint, Vector3 _position) : base(_position)
			{
				A = firstPoint;
				B = secondPoint;
				C = thirdPoint;
			}

			/// <summary>
			/// Convert this triangle to a float array.
			/// </summary>
			/// <param name="spacialContext">Determines the context of the positions. Local will provide the raw values (i.e. their positions relative to the position of this object),
			/// whereas World will provide the positions relative to the Worlds 0, 0, 0.</param>
			/// <returns>One-dimensional float array containing every XYZ value of all points.</returns>
			public float[] ToFloatArray(SpacialContext spacialContext = SpacialContext.Local)
			{
				if (spacialContext == SpacialContext.World)
				{
					return new float[] { A.X + position.X, A.Y + position.Y, A.Z + position.Z,
										 B.X + position.X, B.Y + position.Y, B.Z + position.Z,
										 C.X + position.X, C.Y + position.Y, C.Z + position.Z, };
				}
				else
				{
					return new float[] { A.X, A.Y, A.Z,
										 B.X, B.Y, B.Z,
										 C.X, C.Y, C.Z, };
				}
			}
		}

		public static class Extensions
		{
			/// <summary>
			/// Convert this triangle array to a float array.
			/// </summary>
			/// <param name="ignoreRelativity">If true, relativity to an overall position will be ignored and points will be returned unaltered.</param>
			/// <returns>One-dimensional float array containing every XYZ value of all points of every triangle.</returns>
			public static float[] ToFloatArray(this Triangle[] triArray, SpacialContext spacialContext = SpacialContext.Local)
			{
				float[] triFloatArray = new float[triArray.Length * 9];

				for (int i = 0; i < triArray.Length; i++)
				{
					float[] pointArray = triArray[i].ToFloatArray(spacialContext);

					for (int n = 0; n < 9; n++)
					{
						triFloatArray[n + (9 * i)] = pointArray[n];
					}
				}

				return triFloatArray;
			}
		}
	}
}
