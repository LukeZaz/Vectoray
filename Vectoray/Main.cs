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

#region Using statements

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using OpenGL.CSharpWrapper;
using static OpenGL.OpenGL;
using static OpenGL.OpenGL.GLenum;
using static OpenGL.OpenGL.GLboolean;

using static SDL2.SDL;
using static SDL2.SDL_image; // Unused, outside of the deprecated LoadImage function, kept for future use
using static SDL2.SDL_ttf; // Unused, kept for future use

#endregion

// TODO: Should probably take all the todos in this project and move them over to github issues.

namespace Vectoray
{
	static class Vectoray
	{
		#region Variable declaration

		// Settings
		static int
			screenWidth = 1024,
			screenHeight = 576;

		// IO
		static Assembly assembly;
		static StreamReader shaderReader;

		// OpenGL
		static GLuint glProgramID;

		static GLint vertexPos3DLocation = -1;
		static GLint vertexColorLocation = -1;

		static GLuint[] posVBO = new GLuint[1];
		static GLuint[] colVBO = new GLuint[1];
		static GLuint[] VAO = new GLuint[1];

		static int triangleCount = 0;

		// Matrices
		static Matrix4x4 model;
		static Matrix4x4 view;
		static Matrix4x4 projection;
		/// <summary>
		/// A combined model-view-projection matrix, sent to OpenGL for rendering.
		/// </summary>
		static Matrix4x4 MVP;

		// SDL2
		static Window glWindow = new Window();

		// Sculptition-specific
		static Camera camera;

		// TODO: Consider making this a double instead
		static double deltaTime;

		static float mouseSpeed = 0.0005f,
			moveSpeed = 0.01f;

		// Constants
		/// <summary>
		/// Amount of float values needed to make one triangle.
		/// </summary>
		const short triangleFloatSize = 9;
		/// <summary>
		/// Amount of Vector3 values needed to make one triangle.
		/// </summary>
		const short triangleVector3Size = 3;

		#endregion

		static void Main()
		{
			bool errorOccurred = false;

			ulong now = SDL_GetPerformanceCounter(),
				last = 0;

			if (!Init())
			{
				Extensions.ConsoleWriteError("Initialization failed!");
				errorOccurred = true;
			}
			else
			{
				bool quitProgram = false;
				SDL_Event SDLEvent;
				
				SDL_RaiseWindow(glWindow.window);

				// While running
				while (!quitProgram)
				{
					int mouseMovementX = 0, mouseMovementY = 0;
					InputManager.UpdateStates();

					// Handle all waiting events
					while (SDL_PollEvent(out SDLEvent) != 0)
					{
						if (SDLEvent.type == SDL_EventType.SDL_QUIT)
						{
							SDL_SetRelativeMouseMode(SDL_bool.SDL_FALSE);
							quitProgram = true;
							continue;
						}
						else if (SDLEvent.type == SDL_EventType.SDL_KEYDOWN || SDLEvent.type == SDL_EventType.SDL_KEYUP
							|| SDLEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN || SDLEvent.type == SDL_EventType.SDL_MOUSEBUTTONUP)
						{
							InputManager.HandleEvent(SDLEvent);
						}
						else if (SDLEvent.type == SDL_EventType.SDL_MOUSEMOTION)
						{
							// Catch relative mouse movement
							mouseMovementX = SDLEvent.motion.xrel;
							mouseMovementY = SDLEvent.motion.yrel;
						}
					}

					// Quit the program if the escape button is pressed
					if (InputManager.IsPressed(SDL_Keycode.SDLK_ESCAPE))
					{
						quitProgram = true;
						continue;
					}

					// Window events
					glWindow.HandleEvent(SDLEvent);

					// Calculate delta time
					last = now;
					now = SDL_GetPerformanceCounter();
					deltaTime = (((now - last) * 1000) / SDL_GetPerformanceFrequency());

					// Lock the mouse and track movement only while the right mouse button is down
					if (InputManager.PressedThisFrame(InputManager.MouseKeycode.SDL_BUTTON_RIGHT))
					{
						SDL_SetRelativeMouseMode(SDL_bool.SDL_TRUE);
					}
					else if (InputManager.ReleasedThisFrame(InputManager.MouseKeycode.SDL_BUTTON_RIGHT))
					{
						SDL_SetRelativeMouseMode(SDL_bool.SDL_FALSE);
						// Warp mouse to the center of the screen, for consistency.
						SDL_WarpMouseInWindow(IntPtr.Zero, screenWidth / 2, screenHeight / 2);
					}

					// Only move if the right mouse button is down
					if (InputManager.IsPressed(InputManager.MouseKeycode.SDL_BUTTON_RIGHT))
					{
						// Adjust camera orientation according to mouse movement
						camera.horizontalAngle -= mouseSpeed * (float)deltaTime * mouseMovementX;
						camera.verticalAngle -= mouseSpeed * (float)deltaTime * mouseMovementY;

						// Move camera position according to keypresses
						if (InputManager.IsPressed(SDL_Keycode.SDLK_w) || InputManager.IsPressed(SDL_Keycode.SDLK_UP))
						{
							camera.position += camera.GetForwardDirection() * (float)deltaTime * moveSpeed;
						}
						if (InputManager.IsPressed(SDL_Keycode.SDLK_a) || InputManager.IsPressed(SDL_Keycode.SDLK_LEFT))
						{
							camera.position -= camera.GetRightDirection() * (float)deltaTime * moveSpeed;
						}
						if (InputManager.IsPressed(SDL_Keycode.SDLK_s) || InputManager.IsPressed(SDL_Keycode.SDLK_DOWN))
						{
							camera.position -= camera.GetForwardDirection() * (float)deltaTime * moveSpeed;
						}
						if (InputManager.IsPressed(SDL_Keycode.SDLK_d) || InputManager.IsPressed(SDL_Keycode.SDLK_RIGHT))
						{
							camera.position += camera.GetRightDirection() * (float)deltaTime * moveSpeed;
						}

						// Update matrices
						projection = Matrix4x4.CreatePerspectiveFieldOfView(Extensions.DegreesToRadians(camera.fieldOfView), screenWidth / screenHeight, 0.1f, 100f);
						view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.GetForwardDirection(), camera.GetUpDirection());
						MVP = model * view * projection;
					}

					RenderQuad();

					SDL_GL_SwapWindow(glWindow.window);
				}
			}

			GLenum glError = glGetError();
			if (glError != GL_NO_ERROR)
			{
				Extensions.ConsoleWriteError("OpenGL encountered an error!");
				Extensions.ConsoleWriteError("GL error: " + glError);
				errorOccurred = true;
			}

			if (errorOccurred) Console.ReadKey(); // Pause before quitting so the console can be reviewed
			Quit();
		}

		#region Rendering functions

		/// <summary>
		/// Renders a multicolored quad to the screen.
		/// </summary>
		static void RenderQuad()
		{
			// Clear color buffer
			glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

			glUseProgram(glProgramID);

			// Send most valuable matrix to GLSL code
			GLuint matrixID = glGetUniformLocation(glProgramID, new StringBuilder("MVP"));

			unsafe
			{
				fixed (float* matrixPtr = &MVP.M11)
				{
					glUniformMatrix4fv(matrixID, 1, GL_FALSE, matrixPtr);
				}
			}

			glBindVertexArray(VAO[0]);

			glDrawArrays(GL_TRIANGLES, 0, (uint)(triangleVector3Size * triangleCount));

			// Unbind program
			glUseProgram(0);
		}

		#endregion

		#region Init & quit functions

		/// <summary>
		/// Initializes SDL and the associated OpenGL context, then initializes OpenGL itself.
		/// </summary>
		/// <returns>Boolean representing execution success.</returns>
		static bool Init()
		{
			bool success = true;

			// Initialize assembly
			assembly = Assembly.GetExecutingAssembly();

			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				Extensions.ConsoleWriteError("SDL could not initialize! SDL error: " + SDL_GetError());
				success = false;
			}
			else
			{
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 5);
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_MULTISAMPLEBUFFERS, 1);
				SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES, 2);

				if (!glWindow.Init("OpenGL Test", SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, screenWidth, screenHeight,
					SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL_WindowFlags.SDL_WINDOW_RESIZABLE))
				{
					Extensions.ConsoleWriteError("OpenGL window initialization failed! SDL error: " + SDL_GetError());
					success = false;
				}
				else
				{
					IntPtr glContext = SDL_GL_CreateContext(glWindow.window);

					if (glContext == IntPtr.Zero)
					{
						Extensions.ConsoleWriteError("Failed to create OpenGL context! SDL error: " + SDL_GetError());
						success = false;
					}
					else
					{
						//Console.WriteLine("GLEW support for OpenGL 4.0 availability: " + glewIsSupported("GL_VERSION_4_0  GL_ARB_point_sprite"));

						// VSync
						if (SDL_GL_SetSwapInterval(1) < 0)
						{
							Extensions.ConsoleWriteWarning("Failed to set OpenGL VSync. SDL error: " + SDL_GetError());
							success = false;
						}

						// Create camera
						camera = new Camera(new Vector3(1, 1, 1));

						if (!InitGL())
						{
							Extensions.ConsoleWriteError("Failed to initialize OpenGL!");
							success = false;
						}
					}
				}
			}

			if (success) Console.WriteLine("Initialization completed successfully");

			return success;
		}

		// TODO: Move this to CSharpWrapper?
		/// <summary>
		/// Initializes OpenGL, then creates and links shaders, shader programs, VBOs and VAOs.
		/// Writes OpenGL string information to console when finished.
		/// </summary>
		/// <returns>Boolean representing execution success.</returns>
		static bool InitGL()
		{
			// Initialize VBO, IBO and VAO
			posVBO[0] = 0;
			colVBO[0] = 0;
			VAO[0] = 0;

			bool success = true;
			LoadGLFunctions();

			glProgramID = glCreateProgram();
			// Important: Test depth before drawing. If this is disabled, image will be extremely mangled.
			glEnable(GL_DEPTH_TEST);
			// Multisampling
			glEnable(GL_MULTISAMPLE);
			// Face culling. If enabled, one side of every triangle will be culled under the assumption that it won't be seen.
			//glEnable(GL_CULL_FACE);
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
				Extensions.ConsoleWriteError("Failed to compile vertex shader {0}!", vertexShader);
				writeShaderLog(vertexShader);
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
					Extensions.ConsoleWriteError("Failed to compile fragment shader {0}!", fragmentShader);
					writeShaderLog(fragmentShader);
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
						Extensions.ConsoleWriteError("Failed to link program {0}!", (uint)glProgramID);
						writeProgramLog(glProgramID);
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
							Extensions.ConsoleWriteError("vertexPos3D is not a valid glsl program variable!");
							success = false;
						}
						else if (vertexColorLocation == -1)
						{
							Extensions.ConsoleWriteError("vertexColor is not a valid glsl program variable!");
							success = false;
						}
						else
						{
							// Set up VBO and VAO
							glClearColor(0.8f, 0.8f, 0.8f, 1);
							
							Triangle[] triangleData =
							{
								// X / Y / Z marker triangles
								new Triangle(Vector3.Zero, new Vector3(2, 0, 0), new Vector3(0, 0, 2)),
								new Triangle(Vector3.Zero, new Vector3(2, 0, 0), new Vector3(0, 2, 0)),
								new Triangle(Vector3.Zero, new Vector3(0, 0, 2), new Vector3(0, 2, 0)),

								// Cube is 2x2x2 units
								// Bottom face
								new Triangle(new Vector3(1, -1, 1), new Vector3(-1, -1, 1), new Vector3(1, -1, -1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(-1, -1, 1), new Vector3(4, 1, 4)),
								// Front face
								new Triangle(new Vector3(1, -1, 1), new Vector3(-1, -1, 1), new Vector3(1, 1, 1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(-1, 1, 1), new Vector3(-1, -1, 1), new Vector3(1, 1, 1), new Vector3(4, 1, 4)),
								// Left face
								new Triangle(new Vector3(-1, -1, -1), new Vector3(-1, -1, 1), new Vector3(-1, 1, -1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(-1, 1, -1), new Vector3(-1, 1, 1), new Vector3(-1, -1, 1), new Vector3(4, 1, 4)),
								// Right face
								new Triangle(new Vector3(1, -1, -1), new Vector3(1, -1, 1), new Vector3(1, 1, -1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(1, 1, -1), new Vector3(1, 1, 1), new Vector3(1, -1, 1), new Vector3(4, 1, 4)),
								// Back face
								new Triangle(new Vector3(1, -1, -1), new Vector3(-1, -1, -1), new Vector3(1, 1, -1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(-1, 1, -1), new Vector3(-1, -1, -1), new Vector3(1, 1, -1), new Vector3(4, 1, 4)),
								// Top face
								new Triangle(new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, 1, -1), new Vector3(4, 1, 4)),
								new Triangle(new Vector3(-1, 1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, 1), new Vector3(4, 1, 4))
							};
							triangleCount = triangleData.Length;

							GLfloat[] vertexData = Array.ConvertAll(triangleData.ToFloatArray(SpacialContext.World), item => (GLfloat)item);

							GLfloat[] colorData =
							{
								1.0f, 1.0f, 1.0f, // White (0, 0, 0)
								1.0f, 0.0f, 0.0f, // Red (X)
								0.0f, 0.0f, 1.0f, // Blue (Z)
								1.0f, 1.0f, 1.0f, // White
								1.0f, 0.0f, 0.0f, // Red
								0.0f, 1.0f, 0.0f, // Green (Y)
								1.0f, 1.0f, 1.0f, // White
								0.0f, 0.0f, 1.0f, // Blue
								0.0f, 1.0f, 0.0f, // Green

								// Bottom face
								0.2f, 1.0f, 0.2f,
								0.2f, 1.0f, 0.2f,
								0.2f, 1.0f, 0.2f,
								0.0f, 1.0f, 0.0f,
								0.0f, 1.0f, 0.0f,
								0.0f, 1.0f, 0.0f,
								// Front face
								0.0f, 0.0f, 1.0f,
								0.0f, 0.0f, 1.0f,
								0.0f, 0.0f, 1.0f,
								0.2f, 0.2f, 1.0f,
								0.2f, 0.2f, 1.0f,
								0.2f, 0.2f, 1.0f,
								// Left face
								1.0f, 0.2f, 0.2f,
								1.0f, 0.2f, 0.2f,
								1.0f, 0.2f, 0.2f,
								1.0f, 0.0f, 0.0f,
								1.0f, 0.0f, 0.0f,
								1.0f, 0.0f, 0.0f,
								// Right face
								1.0f, 0.0f, 0.0f,
								1.0f, 0.0f, 0.0f,
								1.0f, 0.0f, 0.0f,
								1.0f, 0.2f, 0.2f,
								1.0f, 0.2f, 0.2f,
								1.0f, 0.2f, 0.2f,
								// Back face
								0.2f, 0.2f, 1.0f,
								0.2f, 0.2f, 1.0f,
								0.2f, 0.2f, 1.0f,
								0.0f, 0.0f, 1.0f,
								0.0f, 0.0f, 1.0f,
								0.0f, 0.0f, 1.0f,
								// Top face
								0.0f, 1.0f, 0.0f,
								0.0f, 1.0f, 0.0f,
								0.0f, 1.0f, 0.0f,
								0.2f, 1.0f, 0.2f,
								0.2f, 1.0f, 0.2f,
								0.2f, 1.0f, 0.2f
							};

							// Create position VBO
							glGenBuffers(1, posVBO);
							glBindBuffer(GL_ARRAY_BUFFER, posVBO[0]);
							glBufferDataf(GL_ARRAY_BUFFER, triangleFloatSize * triangleCount * sizeof(float), vertexData, GL_STATIC_DRAW);

							// Create color VBO
							glGenBuffers(1, colVBO);
							glBindBuffer(GL_ARRAY_BUFFER, colVBO[0]);
							glBufferDataf(GL_ARRAY_BUFFER, triangleFloatSize * triangleCount * sizeof(float), colorData, GL_STATIC_DRAW);

							// Create VAO
							glGenVertexArrays(1, VAO);
							glBindVertexArray(VAO[0]);
							glEnableVertexAttribArray(vertexPos3DLocation);

							glBindBuffer(GL_ARRAY_BUFFER, posVBO[0]);
							glVertexAttribPointer(vertexPos3DLocation, 3, GL_FLOAT, GL_FALSE, 0, IntPtr.Zero);

							glBindBuffer(GL_ARRAY_BUFFER, colVBO[0]);
							glVertexAttribPointer(vertexColorLocation, 3, GL_FLOAT, GL_FALSE, 0, IntPtr.Zero);

							glEnableVertexAttribArray(vertexPos3DLocation);
							glEnableVertexAttribArray(vertexColorLocation);

							// Create matrices
							projection = Matrix4x4.CreatePerspectiveFieldOfView(Extensions.DegreesToRadians(camera.fieldOfView), screenWidth / screenHeight, 0.1f, 100f);
							view = Matrix4x4.CreateLookAt(camera.position, camera.position + camera.GetForwardDirection(), camera.GetUpDirection());
							model = Matrix4x4.Identity;

							MVP = model * view * projection; // C# multiplies differently from other things like GLM; multiplication order is inverted compared to them.

							// Get OpenGL data
							Extensions.ConsoleWriteColored("\n<------OpenGL Information------>", ConsoleColor.White);

							// Renderer
							Console.WriteLine("Vendor: " + Marshal.PtrToStringAnsi(glGetString(GL_VENDOR)));
							Console.WriteLine("Renderer: " + Marshal.PtrToStringAnsi(glGetString(GL_RENDERER)));
							Console.WriteLine("Version: " + Marshal.PtrToStringAnsi(glGetString(GL_VERSION)));
							Console.WriteLine("Shading Language Version: " + Marshal.PtrToStringAnsi(glGetString(GL_SHADING_LANGUAGE_VERSION)));

							Extensions.ConsoleWriteColored("<------OpenGL Information------>\n", ConsoleColor.White);
						}
					}
				}
			}

			Console.WriteLine("OpenGL initialization finished. glGetError: " + glGetError());

			return success;
		}

		/// <summary>
		/// Frees SDL windows and calls SDL & SDL extension quit functions.
		/// </summary>
		static void Quit()
		{
			glWindow.Free();

			// Commented out because these are not initialized anymore
			//TTF_Quit();
			//IMG_Quit();
			SDL_Quit();
		}

		#endregion

		#region Debugging & logging functions

		/// <summary>
		/// Writes given program info log to the console.
		/// </summary>
		/// <param name="program">The program to write the info log of.</param>
		static void writeProgramLog(GLuint program)
		{
			if (glIsProgram(program) == GL_TRUE)
			{
				GLuint infoLogLength = 0;
				GLint maxLength = 0;

				glGetProgramiv(program, GL_INFO_LOG_LENGTH, out maxLength);
				
				StringBuilder infoLog = new StringBuilder(maxLength);

				// Untested; if this doesn't work, change infoLogLength to a uint
				glGetProgramInfoLog(program, (uint)(int)maxLength, out infoLogLength, infoLog);
				if (infoLogLength > 0)
				{
					Console.WriteLine(infoLog);
				}
			}
			else
			{
				Extensions.ConsoleWriteError("GLuint {0} is not a program!", (uint)program);
			}
		}

		/// <summary>
		/// Writes given shader info log to the console.
		/// </summary>
		/// <param name="shader">Shader to write the info log of.</param>
		static void writeShaderLog(GLuint shader)
		{
			if (glIsShader(shader) == GL_TRUE)
			{
				GLuint infoLogLength = 0;
				GLint maxLength = 0;

				glGetShaderiv(shader, GL_INFO_LOG_LENGTH, out maxLength);

				StringBuilder infoLog = new StringBuilder(maxLength);

				glGetShaderInfoLog(shader, (uint)(int)maxLength, out infoLogLength, infoLog);
				if (infoLogLength > 0)
				{
					Console.WriteLine(infoLog);
				}
			}
			else
			{
				Extensions.ConsoleWriteError("GLuint {1} is not a shader!", (uint)shader);
			}
		}

		#endregion

		#region Loading functions

		/// <summary>
		/// Load an image from a given string path, then converts it for use with a SDL renderer.
		/// </summary>
		/// <param name="path">Path to image to load. Accepts a PNG or BMP.</param>
		/// <returns></returns>
		[Obsolete("Carried over from pre-OpenGL and may be unusable. Call at your own risk.")]
		static IntPtr LoadImage(string path)
		{
			IntPtr convertedTexture = IntPtr.Zero;
			IntPtr loadedSurface = IMG_Load(path);
			
			if (loadedSurface == IntPtr.Zero)
			{
				Extensions.ConsoleWriteError("Failed to load image at path '{0}'. SDL Error: {1}", path, SDL_GetError());
			}
			else
			{
				// Convert to texture
				convertedTexture = SDL_CreateTextureFromSurface(glWindow.renderer, loadedSurface);
				if (convertedTexture == IntPtr.Zero)
				{
					Extensions.ConsoleWriteError("Failed to convert image from path '{0}'. SDL Error: {1}", path, SDL_GetError());
				}

				SDL_FreeSurface(loadedSurface);
			}

			return convertedTexture;
		}

		#endregion

		#region Other functions

		/// <summary>
		/// Create a new SDL_Rect from given values.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		/// <param name="w">Width.</param>
		/// <param name="h">Height.</param>
		/// <returns>Newly created SDL_Rect.</returns>
		public static SDL_Rect createRect(int x, int y, int w, int h)
		{
			SDL_Rect newRect;
			newRect.x = x;
			newRect.y = y;
			newRect.w = w;
			newRect.h = h;

			return newRect;
		}

		#endregion
	}

	public static class Extensions
	{
		/// <summary>
		/// Allocates memory for and then converts given SDL_Rect to an IntPtr.
		/// Be sure to keep a reference to this and to free the used up memory when done.
		/// </summary>
		/// <param name="rect">The rect to be converted.</param>
		/// <param name="fDeleteOld">Passed to Marshal.StructureToPtr.</param>
		/// <returns>IntPtr representing the provided rect.</returns>
		public static IntPtr getRaw(this SDL_Rect rect, bool fDeleteOld = true)
		{
			IntPtr newRectRaw = Marshal.AllocHGlobal(Marshal.SizeOf(rect));
			Marshal.StructureToPtr(rect, newRectRaw, fDeleteOld);

			return newRectRaw;
		}

		/// <summary>
		/// Populate an array with objects.
		/// </summary>
		/// <typeparam name="T">Array object type.</typeparam>
		/// <param name="array">Array to populate</param>
		/// <param name="provider">Object to populate with.</param>
		/// <returns>The populated array.</returns>
		public static T[] Populate<T>(this T[] array, Func<T> provider)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = provider();
			}

			return array;
		}

		/// <summary>
		/// Write an error message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void ConsoleWriteError(string message, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message, arg0, arg1, arg2, arg3);
			Console.ResetColor();
		}

		/// <summary>
		/// Write an warning message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void ConsoleWriteWarning(string message, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(message, arg0, arg1, arg2, arg3);
			Console.ResetColor();
		}

		/// <summary>
		/// Write a colored message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="foregroundColor">The foreground color to use.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void ConsoleWriteColored(string message, ConsoleColor foregroundColor, object arg0 = null, object arg1 = null, object arg2 = null,
			object arg3 = null)
		{
			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message, arg0, arg1, arg2, arg3);
			Console.ResetColor();
		}

		/// <summary>
		/// Convert degrees to radians.
		/// </summary>
		/// <param name="angle">Angle to convert.</param>
		/// <returns>Angle converted to radians.</returns>
		public static float DegreesToRadians(float angle)
		{
			return angle * ((float)Math.PI / 180);
		}

		/// <summary>
		/// Convert degrees to radians.
		/// </summary>
		/// <param name="angle">Angle to convert.</param>
		/// <returns>Angle converted to radians.</returns>
		public static double DegreesToRadians(double angle)
		{
			return angle * (Math.PI / 180);
		}

		/// <summary>
		/// Converts a given 4x4 matrix to a one-dimensional float array.
		/// </summary>
		/// <param name="matrix">The matrix to convert.</param>
		/// <returns>A float array containing the matrix values. Ordered left-right -> top-down.</returns>
		public static float[] MatrixToFloatArray4x4(this Matrix4x4 matrix)
		{
			return new float[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14,
								matrix.M21, matrix.M22, matrix.M23, matrix.M24,
								matrix.M31, matrix.M32, matrix.M33, matrix.M34,
								matrix.M41, matrix.M42, matrix.M43, matrix.M44 };
		}
	}

	// TODO: Test the different key states to see if they change and update as they should.
	/// <summary>
	/// A class for managing SDL keyboard events and keeping track of key states.
	/// </summary>
	public static class InputManager
	{
		public enum KeyState
		{
			IsPressed = 0,
			IsReleased = 1,
			PressedThisFrame = 2,
			ReleasedThisFrame = 3
		}

		// SDL2# provides the SDL buttons named in this enum as a series of constants as opposed to an enum, thereby necessitating this
		public enum MouseKeycode : byte
		{
			SDL_BUTTON_LEFT = 1,
			SDL_BUTTON_MIDDLE = 2,
			SDL_BUTTON_RIGHT = 3,
			SDL_BUTTON_X1 = 4,
			SDL_BUTTON_X2 = 5
		}

		// TODO: Change this over to a KeyedCollection. Should improve iteration speed about 5.5x (which would still be rather small, hence why I haven't done it yet)
		// KeyedCollection ToArray is AFAIK the fastest key/value data structure in terms of constant iteration.
		private static Dictionary<SDL_Keycode, KeyState> keyStates = new Dictionary<SDL_Keycode, KeyState>
		{
			#region Default key state definitions
			// Letter keys
			{ SDL_Keycode.SDLK_a, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_b, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_c, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_d, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_e, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_f, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_g, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_h, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_i, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_j, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_k, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_l, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_m, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_n, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_o, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_p, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_q, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_r, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_s, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_t, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_u, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_v, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_w, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_x, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_y, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_z, KeyState.IsReleased },
			// Arrow keys
			{ SDL_Keycode.SDLK_RIGHT, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_DOWN, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_LEFT, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_UP, KeyState.IsReleased },
			// Modifier keys
			{ SDL_Keycode.SDLK_RCTRL, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_RALT, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_RSHIFT, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_LCTRL, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_LALT, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_LSHIFT, KeyState.IsReleased },
			// Action keys
			{ SDL_Keycode.SDLK_RETURN, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_BACKSPACE, KeyState.IsReleased },
			{ SDL_Keycode.SDLK_ESCAPE, KeyState.IsReleased }
			#endregion
		};

		// Mouse key values
		// These do not use SDL_Keycode, and so require a seperate dictionary
		private static Dictionary<MouseKeycode, KeyState> mouseKeyStates = new Dictionary<MouseKeycode, KeyState>
		{
			{ MouseKeycode.SDL_BUTTON_LEFT, KeyState.IsReleased },
			{ MouseKeycode.SDL_BUTTON_MIDDLE, KeyState.IsReleased },
			{ MouseKeycode.SDL_BUTTON_RIGHT, KeyState.IsReleased },
			{ MouseKeycode.SDL_BUTTON_X1, KeyState.IsReleased },
			{ MouseKeycode.SDL_BUTTON_X2, KeyState.IsReleased }
		};
		
		#region Internal key state handling & updating

		/// <summary>
		/// Iterates over the key state dictionary, finding PressedThisFrame and ReleasedThisFrame results and swapping them out for their not-just-this-frame counterparts.
		/// </summary>
		internal static void UpdateStates()
		{
			foreach (SDL_Keycode key in keyStates.Keys.ToArray())
			{
				KeyState state = keyStates[key];
				if (state == KeyState.PressedThisFrame)
				{
					keyStates[key] = KeyState.IsPressed;
				}
				else if (state == KeyState.ReleasedThisFrame)
				{
					keyStates[key] = KeyState.IsReleased;
				}
			}

			foreach (MouseKeycode key in mouseKeyStates.Keys.ToArray())
			{
				KeyState state = mouseKeyStates[key];
				if (state == KeyState.PressedThisFrame)
				{
					mouseKeyStates[key] = KeyState.IsPressed;
				}
				else if (state == KeyState.ReleasedThisFrame)
				{
					mouseKeyStates[key] = KeyState.IsReleased;
				}
			}
		}

		/// <summary>
		/// Handles a given SDL event.
		/// </summary>
		/// <param name="SDLEvent">The SDL event to handle. Will do nothing if it is neither SDL_KEYDOWN or SDL_KEYUP.</param>
		internal static void HandleEvent(SDL_Event SDLEvent)
		{
			if (SDLEvent.type == SDL_EventType.SDL_KEYDOWN)
			{
				keyStates[SDLEvent.key.keysym.sym] = KeyState.PressedThisFrame;
			}
			else if (SDLEvent.type == SDL_EventType.SDL_KEYUP)
			{
				keyStates[SDLEvent.key.keysym.sym] = KeyState.ReleasedThisFrame;
			}
			else if (SDLEvent.type == SDL_EventType.SDL_MOUSEBUTTONDOWN)
			{
				mouseKeyStates[(MouseKeycode)SDLEvent.button.button] = KeyState.PressedThisFrame;
			}
			else if (SDLEvent.type == SDL_EventType.SDL_MOUSEBUTTONUP)
			{
				mouseKeyStates[(MouseKeycode)SDLEvent.button.button] = KeyState.ReleasedThisFrame;
			}
		}

		#endregion

		#region Key state inspection functions

		/// <summary>
		/// Returns the KeyState of the given key directly.
		/// A middle-man for other InputManager key state retrieval functions.
		/// </summary>
		/// <param name="key">The key to probe the state of.</param>
		/// <returns>The KeyState for the given key.</returns>
		private static KeyState GetKeyState(SDL_Keycode key)
		{
			return keyStates[key];
		}

		/// <summary>
		/// Returns the KeyState of the given mouse key directly.
		/// A middle-man for other InputManager key state retrieval functions.
		/// </summary>
		/// <param name="key">The mouse key to probe the state of.</param>
		/// <returns>The KeyState for the given mouse key.</returns>
		private static KeyState GetKeyState(MouseKeycode key)
		{
			return mouseKeyStates[key];
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key is currently pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key is currently pressed.</returns>
		public static bool IsPressed(SDL_Keycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsPressed || state == KeyState.PressedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key is currently pressed.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key is currently pressed.</returns>
		public static bool IsPressed(MouseKeycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsPressed || state == KeyState.PressedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key is currently released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key is currently released.</returns>
		public static bool IsReleased(SDL_Keycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsReleased || state == KeyState.ReleasedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key is currently released.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key is currently released.</returns>
		public static bool IsReleased(MouseKeycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsReleased || state == KeyState.ReleasedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key was just pressed this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key was just pressed this frame.</returns>
		public static bool PressedThisFrame(SDL_Keycode key)
		{
			return (keyStates[key] == KeyState.PressedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key was just pressed this frame.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key was just pressed this frame.</returns>
		public static bool PressedThisFrame(MouseKeycode key)
		{
			return (mouseKeyStates[key] == KeyState.PressedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key was just released this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key was just released this frame.</returns>
		public static bool ReleasedThisFrame(SDL_Keycode key)
		{
			return (keyStates[key] == KeyState.ReleasedThisFrame) ? true : false;
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key was just released this frame.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key was just released this frame.</returns>
		public static bool ReleasedThisFrame(MouseKeycode key)
		{
			return (mouseKeyStates[key] == KeyState.ReleasedThisFrame) ? true : false;
		}

		#endregion
	}
}
