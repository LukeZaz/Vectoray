/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2019 LukeZaz

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
using static SDL2.SDL;

namespace Vectoray
{
	// These namespace summaries don't play nice without addons to VS, but hey, you're still reading them now, aren't you? It's something.
	/// <summary>
	/// Raw OpenGL functionality, unaltered in both use and syntax where possible.
	/// Most of this namespace is held in the Definitions static class, where everything is declared.
	/// This namespace is primarily utilized as a middle-man for the Rendering namespace.
	/// For most situations, it's recommended you use functionality from the Rendering namespace rather than this.
	/// </summary>
	namespace OpenGL
	{
		/// <summary>
		/// Functions obtained off OpenGL utilizing GetProcAddress.
		/// All names will be identical wherever possible.
		/// </summary>
		public static class Definitions
		{
			#region Type declaration

			public enum GLenum
			{
				GL_NO_ERROR = 0,
				GL_DEPTH_BUFFER_BIT = 0x00000100,
				GL_COLOR_BUFFER_BIT = 0x00004000,
				GL_LINES = 0x0001,
				GL_LINE_LOOP = 0x0002,
				GL_LINE_STRIP = 0x0003,
				GL_TRIANGLES = 0x0004,
				GL_TRIANGLE_FAN = 0x0006,
				GL_LESS = 0x0201,
				GL_INVALID_ENUM = 0x0500,
				GL_INVALID_VALUE = 0x0501,
				GL_INVALID_OPERATION = 0x0502,
				GL_STACK_OVERFLOW = 0x0503,
				GL_STACK_UNDERFLOW = 0x0504,
				GL_OUT_OF_MEMORY = 0x0505,
				GL_INVALID_FRAMEBUFFER_OPERATION = 0x0506,
				GL_CULL_FACE = 0x0B44,
				GL_UNSIGNED_INT = 0x1405,
				GL_FLOAT = 0x1406,
				GL_VENDOR = 0x1F00,
				GL_RENDERER = 0x1F01,
				GL_VERSION = 0x1F02,
				GL_EXTENSIONS = 0x1F03,
				GL_MULTISAMPLE = 0x809D,
				GL_ARRAY_BUFFER = 0x8892,
				GL_ELEMENT_ARRAY_BUFFER = 0x8893,
				GL_STATIC_DRAW = 0x88E4,
				GL_DEPTH_TEST = 0x0B71,
				GL_FRAGMENT_SHADER = 0x8B30,
				GL_VERTEX_SHADER = 0x8B31,
				GL_COMPILE_STATUS = 0x8B81,
				GL_LINK_STATUS = 0x8B82,
				GL_INFO_LOG_LENGTH = 0x8B84,
				GL_SHADING_LANGUAGE_VERSION = 0x8B8C
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
					Debugging.LogError("Failed to load OpenGL functions! OpenGL error: " + glError.ToString());
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
			public unsafe delegate void _glUniformMatrix4fv(GLint location, uint count, GLboolean transpose, float[] value);
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
	}

	/// <summary>
	/// Contains most OpenGL functions wrapped up and modified to be easier to use or more effective.
	/// Typically more limited than directly working with OpenGL as a tradeoff.
	/// If you need more freedom, use the OpenGL namespace instead.
	/// </summary>
	namespace Rendering
	{
		using System.Drawing;
		using System.IO;
		using System.Numerics;
		using System.Reflection;

		using static OpenGL.Definitions;
		using static OpenGL.Definitions.GLenum;
		using static OpenGL.Definitions.GLboolean;

		/* Idea for experimental new layout, akin to that used by Unity [update: this namespace is for rendering, not physics, this does not belong here]
		   'WorldObject' class, which is just a basis - essentially a position, and host for other classes.
		   It can, from there, support other shapes. Like Unity, this will include a differentiation between
		   World Space and Local Space, with WorldObject existing primarily to easily organize all local data (such as local position).
		   This might not be a good idea, but the general premise sounds promising, and so I will be trying it. */
		
		// TODO: WorldObject-based objects need color handling. (?)

		/// <summary>
		/// A two-value enum used to define the spacial context in which something is done.
		/// Actions can be relative to the given object, or the entire world; this is used for determining that.
		/// </summary>
		public enum SpatialContext
		{
			World = 0,
			Local = 1
		}

		// It should be noted that Alpha doesn't currently do anything. I haven't added alpha blending yet (July 2018), so it serves no purpose.
		// Still, I've included functionality for it, since I'm almost certainly going to be using it eventually.
		/// <summary>
		/// A point in space that connects with others to form a model.
		/// </summary>
		public struct Vertex
		{
			public Vector3 position;
			public Vector4 color;

			public const int SizeOf = sizeof(float) * 7;

			public Vertex(Vector3 _position, Vector4 _color)
			{
				position = _position;
				color = _color;
			}

			public Vertex(Vector3 _position, Color _color)
			{
				position = _position;
				color = new Vector4((float)_color.R / 255, (float)_color.G / 255, (float)_color.B / 255, (float)_color.A / 255);
			}
		}
		
		// TODO: I THOUGHT THIS NAMESPACE WAS "OPENGL" NOT "OPENGL AND ALSO SOME PHYSICS FRAMEWORKS", GET OOOOOUUUUUUTT
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
					// UPDATE 1-14-18: I've since learned of ways that this can be done, then promptly forgot them. It is almost certainly possible, however.
					Debugging.LogWarning("WorldObject created with null position variable. Defaulting to Vector3.Zero.");
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
			public float horizontalAngle, verticalAngle, fieldOfView, nearPlane, farPlane;

			/// <summary>
			/// Initializes a new camera with the default values of: 0/0/0 position, Horizontal Angle 3.14, Vertical Angle 0, Field of View (Horizontal FoV, in degrees) 75,
			/// near clipping plane of 0.1 units and a far clipping plane of 100.
			/// </summary>
			public Camera() : base()
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				nearPlane = 0.1f;
				farPlane = 100;
				fieldOfView = 75;
			}

			/// <summary>
			/// Initializes a new camera with a default position of 0/0/0, Horizontal Angle of 3.14, a default Vertical Angle of 0,
			/// near clipping plane of 0.1 units, far clipping plane of 100, and a provided horizontal Field of View.
			/// </summary>
			/// <param name="_fieldOfView">The horizontal field of view (FoV) for this camera, in degrees.</param>
			public Camera(float _fieldOfView) : base()
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				nearPlane = 0.1f;
				farPlane = 100;
				fieldOfView = _fieldOfView;
			}

			/// <summary>
			/// Initializes a new camera with a default position of 0/0/0, Horizontal Angle of 3.14, a default Vertical Angle of 0,
			/// and the provided near/far clipping planes and horizontal FoV.
			/// </summary>
			/// <param name="_nearPlane">The near clipping plane distance of this camera.</param>
			/// <param name="_farPlane">The far clipping plane distance of this camera.</param>
			/// <param name="_fieldOfView">The horizontal field of view (FoV) of this camera.</param>
			public Camera(float _nearPlane, float _farPlane, float _fieldOfView) : base()
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				nearPlane = _nearPlane;
				farPlane = _farPlane;
				fieldOfView = _fieldOfView;
			}

			/// <summary>
			/// Initializes a new camera with a default Horizontal Angle of 3.14, a default Vertical Angle of 0,
			/// near/far clipping planes of 0.1 and 100 units respectively, and a provided Field of View and position.
			/// </summary>
			/// <param name="_position">The starting position for this camera.</param>
			/// <param name="_fieldOfView">The horizontal field of view (FoV) for this camera, in degrees. If not provided, defaults to 75.</param>
			public Camera(Vector3 _position, float _fieldOfView = 75) : base(_position)
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				nearPlane = 0.1f;
				farPlane = 100;
				fieldOfView = _fieldOfView;
			}

			/// <summary>
			/// Initializes a new camera with a default Horizontal Angle of 3.14, a default Vertical Angle of 0,
			/// and the provided near/far clipping planes, horizontal FoV and position.
			/// </summary>
			/// <param name="_nearPlane">The near clipping plane distance of this camera.</param>
			/// <param name="_farPlane">The far clipping plane distance of this camera.</param>
			/// <param name="_fieldOfView">The horizontal field of view (FoV) of this camera.</param>
			public Camera(Vector3 _position, float _nearPlane, float _farPlane = 100, float _fieldOfView = 75) : base(_position)
			{
				horizontalAngle = 3.14f;
				verticalAngle = 0;
				nearPlane = _nearPlane;
				farPlane = _farPlane;
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

			/// <summary>
			/// Gets the vertical FoV of this camera.
			/// </summary>
			/// <param name="screenWidth">Width of the screen.</param>
			/// <param name="screenHeight">Height of the screen.</param>
			/// <param name="returnInRadians">Whether or not to return the FoV in radians.</param>
			/// <returns>Vertical FoV of this camera.</returns>
			public float GetVerticalFoV(float screenWidth, float screenHeight, bool returnInRadians = false)
			{
				float hFoVInRadians = fieldOfView * (float)Math.PI / 180;
				float vFoVInRadians = 2 * (float)Math.Atan(Math.Tan(hFoVInRadians / 2) * (screenHeight / screenWidth));

				if (returnInRadians) return vFoVInRadians;
				else return vFoVInRadians * 180 / (float)Math.PI;
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
			/// <param name="spatialContext">Determines the context of the positions. Local will provide the raw values (i.e. their positions relative to the position of this object),
			/// whereas World will provide the positions relative to the Worlds 0, 0, 0.</param>
			/// <returns>One-dimensional float array containing every XYZ value of all points.</returns>
			public float[] ToFloatArray(SpatialContext spatialContext = SpatialContext.Local)
			{
				if (spatialContext == SpatialContext.World)
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

		public static class Renderer
		{
			#region Variable declaration

			// Settings
			static int
				screenWidth = 1024,
				screenHeight = 576;

			// IO
			static StreamReader shaderReader;

			// OpenGL
			static GLuint glProgramID;

			static GLint vertexPos3DLocation = -1;
			static GLint vertexColorLocation = -1;

			static GLuint[] posVBO = new GLuint[1];
			static GLuint[] colVBO = new GLuint[1];
			static GLuint[] IBO = new GLuint[1];
			static GLuint[] VAO = new GLuint[1];

			//static int triangleCount = 0;
			static int indexCount = 0;

			// Matrices
			static Matrix4x4 model;
			static Matrix4x4 view;
			static Matrix4x4 projection;
			/// <summary>
			/// A combined model-view-projection matrix, sent to OpenGL for rendering.
			/// </summary>
			static Matrix4x4 MVP;

			#endregion

			public static bool Initialize(int _screenWidth, int _screenHeight, Camera camera)
			{
				screenWidth = _screenWidth;
				screenHeight = _screenHeight;

				return InitGL(Assembly.GetExecutingAssembly(), camera);
			}

			/// <summary>
			/// Initializes OpenGL, then creates and links shaders, shader programs, VBOs and VAOs.
			/// Writes OpenGL string information to console when finished.
			/// </summary>
			/// <returns>Boolean representing execution success.</returns>
			private static bool InitGL(Assembly assembly, Camera camera)
			{
				// Initialize VBOs, IBO and VAO
				posVBO[0] = 0;
				colVBO[0] = 0;
				IBO[0] = 0;
				VAO[0] = 0;

				bool success = true;
				LoadGLFunctions();

				glProgramID = glCreateProgram();
				// Important: Test depth before drawing. If this is disabled, image will be extremely mangled.
				glEnable(GL_DEPTH_TEST);
				// Multisampling
				glEnable(GL_MULTISAMPLE);
				// Face culling. If enabled, one side of every triangle will be culled under the assumption that it won't be seen.
				glEnable(GL_CULL_FACE);
				// Depth comparison function. Determines how GL_DEPTH_TEST will work.
				glDepthFunc(GL_LESS);

				// Vertex shader
				GLuint vertexShader = glCreateShader(GL_VERTEX_SHADER);

				shaderReader = new StreamReader(assembly.GetManifestResourceStream("Vectoray.vertexShader.txt"));
				string[] vertexShaderSource = shaderReader.ReadToEnd().Replace("\\n", "\n").Split(new[] { Environment.NewLine }, StringSplitOptions.None);

				glShaderSource(vertexShader, (uint)vertexShaderSource.Length, vertexShaderSource, null);

				glCompileShader(vertexShader);

				GLint vShaderCompiled = GL_FALSE;
				glGetShaderiv(vertexShader, GL_COMPILE_STATUS, out vShaderCompiled);
				if (vShaderCompiled != GL_TRUE)
				{
					Debugging.LogError("Failed to compile vertex shader {0}!", vertexShader);
					Debugging.WriteShaderLog(vertexShader);
					success = false;
				}
				else
				{
					// Vertex Shader creation successful, attach it
					glAttachShader(glProgramID, vertexShader);

					// Fragment shader
					GLuint fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);

					shaderReader = new StreamReader(assembly.GetManifestResourceStream("Vectoray.fragmentShader.txt"));
					string[] fragmentShaderSource = shaderReader.ReadToEnd().Replace("\\n", "\n").Split(new[] { Environment.NewLine }, StringSplitOptions.None);

					glShaderSource(fragmentShader, (uint)fragmentShaderSource.Length, fragmentShaderSource, null);

					glCompileShader(fragmentShader);

					GLint fShaderCompiled = GL_FALSE;
					glGetShaderiv(fragmentShader, GL_COMPILE_STATUS, out fShaderCompiled);
					if (fShaderCompiled != GL_TRUE)
					{
						Debugging.LogError($"Failed to compile fragment shader {fragmentShader}!");
						Debugging.WriteShaderLog(fragmentShader);
						success = false;
					}
					else
					{
						// Fragment Shader creation successful, attach it
						glAttachShader(glProgramID, fragmentShader);

						glLinkProgram(glProgramID);

						GLint programSuccess = GL_FALSE;
						glGetProgramiv(glProgramID, GL_LINK_STATUS, out programSuccess);
						if (programSuccess != GL_TRUE)
						{
							Debugging.LogError("Failed to link program {0}!", (uint)glProgramID);
							Debugging.WriteProgramLog(glProgramID);
							success = false;
						}
						else
						{
							// TODO: Better way of doing this; manually entering a name for every variable is dumb
							// Program linked successfully, set up shader variables
							vertexPos3DLocation = glGetAttribLocation(glProgramID, new StringBuilder("vertexPos3D"));
							vertexColorLocation = glGetAttribLocation(glProgramID, new StringBuilder("vertexColor"));
							if (vertexPos3DLocation == -1)
							{
								Debugging.LogError("vertexPos3D is not a valid glsl program variable!");
								success = false;
							}
							else if (vertexColorLocation == -1)
							{
								Debugging.LogError("vertexColor is not a valid glsl program variable!");
								success = false;
							}
							else
							{
								// Set up VBO and VAO
								glClearColor(0.8f, 0.8f, 0.8f, 1);

								Vertex[] vertices =
								{
								// X/Y/Z marker vertices
								new Vertex(Vector3.Zero, Color.WhiteSmoke),
								new Vertex(new Vector3(0.2f, 0.2f, 0), Color.WhiteSmoke),
								new Vertex(new Vector3(0.2f, 0, 0.2f), Color.WhiteSmoke),
								new Vertex(new Vector3(0, 0.2f, 0.2f), Color.WhiteSmoke),
								new Vertex(new Vector3(2.5f, 0, 0), Color.Red),
								new Vertex(new Vector3(0, 2.5f, 0), Color.Lime), // Why is Lime RGB 0,1,0 and not Green? What?
								new Vertex(new Vector3(0, 0, 2.5f), Color.Blue),

								// Cube vertices (Cube is 2x2x2 and has an offset of 4x, 4z, 0y. "Origin" is considered bottom center.)
								// Bottom face (Y-negative)
								new Vertex(new Vector3(3, 0, 3), Color.Lime), // Bottom left
								new Vertex(new Vector3(5, 0, 3), Color.Lime), // Top left
								new Vertex(new Vector3(5, 0, 5), Color.Lime), // Top right
								new Vertex(new Vector3(3, 0, 5), Color.Lime), // Bottom right
								// Top face (Y-positive)
								new Vertex(new Vector3(3, 2, 3), Color.Lime),
								new Vertex(new Vector3(5, 2, 3), Color.Lime),
								new Vertex(new Vector3(5, 2, 5), Color.Lime),
								new Vertex(new Vector3(3, 2, 5), Color.Lime),
								// X-negative face
								new Vertex(new Vector3(3, 0, 3), Color.Red),
								new Vertex(new Vector3(3, 2, 3), Color.Red),
								new Vertex(new Vector3(3, 2, 5), Color.Red),
								new Vertex(new Vector3(3, 0, 5), Color.Red),
								// X-plosive face
								new Vertex(new Vector3(5, 0, 3), Color.Red),
								new Vertex(new Vector3(5, 2, 3), Color.Red),
								new Vertex(new Vector3(5, 2, 5), Color.Red),
								new Vertex(new Vector3(5, 0, 5), Color.Red),
								// Z-negative face
								new Vertex(new Vector3(5, 0, 3), Color.Blue),
								new Vertex(new Vector3(5, 2, 3), Color.Blue),
								new Vertex(new Vector3(3, 2, 3), Color.Blue),
								new Vertex(new Vector3(3, 0, 3), Color.Blue),
								// Z-positive face
								new Vertex(new Vector3(5, 0, 5), Color.Blue),
								new Vertex(new Vector3(5, 2, 5), Color.Blue),
								new Vertex(new Vector3(3, 2, 5), Color.Blue),
								new Vertex(new Vector3(3, 0, 5), Color.Blue)
							};
								// TODO: Doublecheck vertices for duplicates (requiring duplicate color), show warning if found. (do via Distinct().Count())

								GLfloat[] vertexData = Array.ConvertAll(Array.ConvertAll(vertices, vertex => vertex.position).ToFloatArray(), item => (GLfloat)item);

								GLfloat[] colorData = Array.ConvertAll(Array.ConvertAll(vertices,
									vertex => new Vector3(vertex.color.X, vertex.color.Y, vertex.color.Z)).ToFloatArray(), item => (GLfloat)item);

								GLuint[] indices = { 4, 1, 2, 5, 3, 1, 6, 2, 3, 2, 1, 3, // "Front" piece of X/Y/Z marker
												 0, 1, 4, 0, 4, 2, // X "Back" piece
												 0, 5, 1, 0, 3, 5, // Y "Back" piece
												 0, 6, 3, 0, 2, 6, // Z "Back" piece
												 7, 8, 10, 9, 10, 8, // Cube Y-negative
												 11, 14, 12, 13, 12, 14, // Cube Y-positive
												 15, 18, 16, 17, 16, 18, // Cube X-negative
												 19, 20, 22, 21, 22, 20, // Cube X-positive
												 23, 26, 24, 25, 24, 26, // Cube Z-negative
												 27, 28, 30, 29, 30, 28 // Cube Z-positive
												 };
								indexCount = indices.Length;

								// Create position VBO
								glGenBuffers(1, posVBO);
								glBindBuffer(GL_ARRAY_BUFFER, posVBO[0]);
								glBufferDataf(GL_ARRAY_BUFFER, vertices.Length * 3 * sizeof(float), vertexData, GL_STATIC_DRAW);

								// Create color VBO
								glGenBuffers(1, colVBO);
								glBindBuffer(GL_ARRAY_BUFFER, colVBO[0]);
								glBufferDataf(GL_ARRAY_BUFFER, vertices.Length * 4 * sizeof(float), colorData, GL_STATIC_DRAW);

								// Create IBO
								glGenBuffers(1, IBO);
								glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, IBO[0]);
								glBufferDataui(GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), indices, GL_STATIC_DRAW);

								// Create VAO
								glGenVertexArrays(1, VAO);
								glBindVertexArray(VAO[0]);

								glBindBuffer(GL_ARRAY_BUFFER, posVBO[0]);
								glVertexAttribPointer(vertexPos3DLocation, 3, GL_FLOAT, GL_FALSE, 0, IntPtr.Zero);

								glBindBuffer(GL_ARRAY_BUFFER, colVBO[0]);
								glVertexAttribPointer(vertexColorLocation, 3, GL_FLOAT, GL_FALSE, 0, IntPtr.Zero);

								glEnableVertexAttribArray(vertexPos3DLocation);
								glEnableVertexAttribArray(vertexColorLocation);

								// Create initial model matrix and do first MVP update
								model = Matrix4x4.Identity;
								UpdateMVP(camera);

								MVP = model * view * projection; // C# multiplies differently from other things like GLM; multiplication order is inverted compared to them.

								// Get OpenGL data
								Debugging.LogColored("\n<------OpenGL Information------>", ConsoleColor.White);

								// Renderer
								Console.WriteLine("Vendor: " + Marshal.PtrToStringAnsi(glGetString(GL_VENDOR)));
								Console.WriteLine("Renderer: " + Marshal.PtrToStringAnsi(glGetString(GL_RENDERER)));
								Console.WriteLine("Version: " + Marshal.PtrToStringAnsi(glGetString(GL_VERSION)));
								Console.WriteLine("Shading Language Version: " + Marshal.PtrToStringAnsi(glGetString(GL_SHADING_LANGUAGE_VERSION)));

								Debugging.LogColored("<------OpenGL Information------>\n", ConsoleColor.White);
							}
						}
					}
				}

				Console.WriteLine("OpenGL initialization finished. glGetError: " + glGetError());

				return success;
			}

			public static void RenderFrame()
			{
				// Clear color buffer
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

				glUseProgram(glProgramID);

				// Send most valuable matrix to GLSL code
				GLuint matrixID = glGetUniformLocation(glProgramID, new StringBuilder("MVP"));

				glUniformMatrix4fv(matrixID, 1, GL_FALSE, MVP.MatrixToFloatArray4x4());

				glBindVertexArray(VAO[0]);

				glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, IBO[0]);
				glDrawElements(GL_TRIANGLES, (uint)indexCount, GL_UNSIGNED_INT, IntPtr.Zero);

				// Unbind program
				glUseProgram(0);
			}

			/// <summary>
			/// Update Model/View/Projection matrix from the provided camera.
			/// </summary>
			/// <param name="cam">Camera to use for the update, including its FoV and near/far clipping planes.</param>
			public static void UpdateMVP(Camera cam)
			{
				projection = Matrix4x4.CreatePerspectiveFieldOfView(
					cam.GetVerticalFoV(screenWidth, screenHeight, true),
					(float)screenWidth / screenHeight, cam.nearPlane, cam.farPlane);

				view = Matrix4x4.CreateLookAt(cam.position, cam.position + cam.GetForwardDirection(), cam.GetUpDirection());
				MVP = model * view * projection;
			}
		}

		public static class Extensions
		{
			// TODO: Why is this not just in the Triangle class? Also, do I still need the Triangle class?
			/// <summary>
			/// Convert this triangle array to a float array.
			/// </summary>
			/// <param name="spatialContext">Spacial context used to determine what the returned values will be relative to.</param>
			/// <returns>One-dimensional float array containing every XYZ value of all points of every triangle.</returns>
			public static float[] ToFloatArray(this Triangle[] triArray, SpatialContext spatialContext = SpatialContext.Local)
			{
				float[] triFloatArray = new float[triArray.Length * 9];

				for (int i = 0; i < triArray.Length; i++)
				{
					float[] pointArray = triArray[i].ToFloatArray(spatialContext);

					for (int n = 0; n < 9; n++)
					{
						triFloatArray[n + (9 * i)] = pointArray[n];
					}
				}

				return triFloatArray;
			}

			// TODO: ...surely there's a better way than this...
			/// <summary>
			/// Convert this Vector3 array to a float array.
			/// </summary>
			/// <returns>One-dimensional float array containing every XYZ value of every Vector3.</returns>
			public static float[] ToFloatArray(this Vector3[] vecArray)
			{
				float[] vecFloatArray = new float[vecArray.Length * 3];

				for (int i = 0; i < vecArray.Length; i++)
				{
					vecFloatArray[(i * 3)] = vecArray[i].X;
					vecFloatArray[(i * 3) + 1] = vecArray[i].Y;
					vecFloatArray[(i * 3) + 2] = vecArray[i].Z;
				}

				return vecFloatArray;
			}

			/// <summary>
			/// Convert this Vector4 array to a float array.
			/// </summary>
			/// <returns>One-dimensional float array containing every XYZW value of every Vector4.</returns>
			public static float[] ToFloatArray(this Vector4[] vecArray)
			{
				float[] vecFloatArray = new float[vecArray.Length * 4];

				for (int i = 0; i < vecArray.Length; i++)
				{
					vecFloatArray[(i * 4)] = vecArray[i].X;
					vecFloatArray[(i * 4) + 1] = vecArray[i].Y;
					vecFloatArray[(i * 4) + 2] = vecArray[i].Z;
					vecFloatArray[(i * 4) + 3] = vecArray[i].W;
				}

				return vecFloatArray;
			}
		}
	}
}
