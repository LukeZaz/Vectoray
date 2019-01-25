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

// Note 1-16-19: I've decided to swap this project to a 3D game engine as a learning project. While I may still include
// modeling program features here and there if they prove useful, that will no longer be the goal.
// Seeing as Probuilder is now free and Blender is finally improving their UI, making another program for the sake of usability is now unnecessary.

using System;
using System.Numerics;
using System.Runtime.InteropServices;

using Vectoray.Rendering;
using static Vectoray.OpenGL.Definitions;
using static Vectoray.OpenGL.Definitions.GLenum;

using static SDL2.SDL;
using static SDL2.SDL_image; // Unused, outside of the deprecated LoadImage function, kept for future use
using static SDL2.SDL_ttf; // Unused, kept for future use

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

		// SDL2
		static Window glWindow = new Window();

		// Vectoray-specific
		static Camera camera;
		
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

			if (!Initialize())
			{
				Debugging.LogError("Initialization failed!");
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
					deltaTime = (now - last) * 1000 / SDL_GetPerformanceFrequency();

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
						
						// Ascending/descending movement
						if (InputManager.IsPressed(SDL_Keycode.SDLK_q)) camera.position += Vector3.UnitY * (float)deltaTime * (moveSpeed / 2);
						if (InputManager.IsPressed(SDL_Keycode.SDLK_e)) camera.position -= Vector3.UnitY * (float)deltaTime * (moveSpeed / 2);

						// Update matrices
						Renderer.UpdateMVP(camera);
					}

					Renderer.RenderFrame();

					SDL_GL_SwapWindow(glWindow.window);
				}
			}

			GLenum glError = glGetError();
			if (glError != GL_NO_ERROR)
			{
				Debugging.LogError("OpenGL encountered an error!");
				Debugging.LogError("GL error: " + glError);
				errorOccurred = true;
			}

			if (errorOccurred) Console.ReadKey(); // Pause before quitting so the console can be reviewed
			Quit();
		}

		#region Init & quit functions

		/// <summary>
		/// Initializes SDL and the associated OpenGL context, then initializes OpenGL itself.
		/// </summary>
		/// <returns>Boolean representing execution success.</returns>
		static bool Initialize()
		{
			bool success = true;

			if (SDL_Init(SDL_INIT_VIDEO) != 0)
			{
				Debugging.LogError("SDL could not initialize! SDL error: " + SDL_GetError());
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
					Debugging.LogError("OpenGL window initialization failed! SDL error: " + SDL_GetError());
					success = false;
				}
				else
				{
					IntPtr glContext = SDL_GL_CreateContext(glWindow.window);

					if (glContext == IntPtr.Zero)
					{
						Debugging.LogError("Failed to create OpenGL context! SDL error: " + SDL_GetError());
						success = false;
					}
					else
					{
						// VSync
						if (SDL_GL_SetSwapInterval(1) < 0)
						{
							Debugging.LogWarning("Failed to set OpenGL VSync. SDL error: " + SDL_GetError());
							success = false;
						}

						// Create camera
						camera = new Camera(new Vector3(1, 1, 3));

						if (!Renderer.Initialize(screenWidth, screenHeight, camera))
						{
							Debugging.LogError("Failed to initialize OpenGL!");
							success = false;
						}
					}
				}
			}

			if (success) Console.WriteLine("Initialization completed successfully");

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
				Debugging.LogError("Failed to load image at path '{0}'. SDL Error: {1}", path, SDL_GetError());
			}
			else
			{
				// Convert to texture
				convertedTexture = SDL_CreateTextureFromSurface(glWindow.renderer, loadedSurface);
				if (convertedTexture == IntPtr.Zero)
				{
					Debugging.LogError("Failed to convert image from path '{0}'. SDL Error: {1}", path, SDL_GetError());
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

	// TODO clean this up
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
		/// Convert degrees to radians.
		/// </summary>
		/// <param name="angle">Angle to convert.</param>
		/// <returns>Angle converted to radians.</returns>
		public static float DegreesToRadians(float angle) => angle* ((float) Math.PI / 180);

		/// <summary>
		/// Convert degrees to radians.
		/// </summary>
		/// <param name="angle">Angle to convert.</param>
		/// <returns>Angle converted to radians.</returns>
		public static double DegreesToRadians(double angle) => angle * (Math.PI / 180);

		/// <summary>
		/// Converts a given 4x4 matrix to a one-dimensional float array.
		/// </summary>
		/// <param name="matrix">The matrix to convert.</param>
		/// <returns>A float array containing the matrix values. Ordered left-right -> top-down.</returns>
		public static float[] MatrixToFloatArray4x4(this Matrix4x4 matrix)
			=> new float[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14,
							 matrix.M21, matrix.M22, matrix.M23, matrix.M24,
							 matrix.M31, matrix.M32, matrix.M33, matrix.M34,
							 matrix.M41, matrix.M42, matrix.M43, matrix.M44 };
	}
}
