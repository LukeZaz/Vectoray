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
using static SDL2.SDL;

namespace Vectoray
{
	public class Window
	{
		public IntPtr window { get; private set; }

		public int width { get; private set; }
		public int height { get; private set; }

		public bool mouseFocus { get; private set; }
		public bool keyboardFocus { get; private set; }
		public bool fullscreen { get; private set; }
		public bool minimized { get; private set; }

		public IntPtr renderer { get; private set; }

		#region Constructors & destructors

		public Window()
		{
			window = IntPtr.Zero;

			mouseFocus = false;
			keyboardFocus = false;
			fullscreen = false;
			minimized = false;

			width = 0;
			height = 0;
		}

		/// <summary>
		/// Creates the empty window object, then automatically calls Init with the given values.
		/// Be aware that this will not return a boolean for initialization success.
		/// </summary>
		/// <param name="title">Window title.</param>
		/// <param name="x">X position.</param>
		/// <param name="y">Y position.</param>
		/// <param name="w">Width.</param>
		/// <param name="h">Height.</param>
		/// <param name="flags">Window flags to be used on creation.</param>
		/// <param name="logErrors">Whether or not to write initialization errors to console.</param>
		public Window(string title = "Untitled", int x = SDL_WINDOWPOS_UNDEFINED, int y = SDL_WINDOWPOS_UNDEFINED,
			int w = 0, int h = 0, SDL_WindowFlags flags = 0, bool logErrors = true) : this()
		{
			Init(title, x, y, w, h, flags, logErrors);
		}

		~Window()
		{
			Free();
		}

		#endregion

		/// <summary>
		/// Initialize and create this window with the given values.
		/// </summary>
		/// <param name="title">Window title.</param>
		/// <param name="x">X position.</param>
		/// <param name="y">Y position.</param>
		/// <param name="w">Width.</param>
		/// <param name="h">Height.</param>
		/// <param name="flags">Window flags to be used on creation.</param>
		/// <param name="logErrors">Whether or not to write initialization errors to console.</param>
		/// <returns>Whether or not initialization succeeded and the window was created.</returns>
		public bool Init(string title = "Untitled", int x = SDL_WINDOWPOS_UNDEFINED, int y = SDL_WINDOWPOS_UNDEFINED,
			int w = 0, int h = 0, SDL_WindowFlags flags = 0, bool logErrors = true)
		{
			window = SDL_CreateWindow(title, x, y, w, h, flags);
			if (window != IntPtr.Zero)
			{
				mouseFocus = ((flags & SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS) == SDL_WindowFlags.SDL_WINDOW_MOUSE_FOCUS);
				keyboardFocus = ((flags & SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) == SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS);
				width = w;
				height = h;
			}
			else if (logErrors) Console.WriteLine("Attempted to create window '{0}' but failed. SDL error: {1}", title, SDL_GetError());

			return window != IntPtr.Zero;
		}

		/// <summary>
		/// Create and set the renderer for this window.
		/// </summary>
		/// <param name="index">Index.</param>
		/// <param name="flags">Renderer flags to be used on creation.</param>
		/// <returns>Whether or not the renderer was successfully created.</returns>
		public bool CreateRenderer(int index = -1, SDL_RendererFlags flags = 0)
		{
			renderer = SDL_CreateRenderer(window, index, flags);
			return (renderer != IntPtr.Zero);
		}

		/// <summary>
		/// Handle a given SDL_WINDOWEVENT event.
		/// </summary>
		/// <param name="e">The event to handle.</param>
		public void HandleEvent(SDL_Event e)
		{
			if (e.type == SDL_EventType.SDL_WINDOWEVENT)
			{
				switch (e.window.windowEvent)
				{
					case SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED: // Repaint on size change
						width = e.window.data1;
						height = e.window.data2;
						SDL_RenderPresent(renderer);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED: // Repaint on exposure
						SDL_RenderPresent(renderer);
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_ENTER: // Mouse focus boolean
						mouseFocus = true;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
						mouseFocus = false;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED: // Keyboard focus boolean
						keyboardFocus = true;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
						keyboardFocus = false;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED: // Minimization boolean
						minimized = true;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
						minimized = false;
						break;
					case SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
						minimized = false;
						break;
				}
			}
			else if (e.type == SDL_EventType.SDL_KEYDOWN)
			{
				if (e.key.keysym.sym == SDL_Keycode.SDLK_f)
				{
					// Toggle fullscreen
					SDL_SetWindowFullscreen(window, (uint)((fullscreen) ? SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP : 0));
					fullscreen = !fullscreen;
					minimized = (fullscreen) ? false : minimized;
				}
			}
		}

		/// <summary>
		/// Free memory taken by and then destroy renderer and window if present.
		/// Automatically called by deconstructor.
		/// </summary>
		public void Free()
		{
			if (window != IntPtr.Zero)
			{
				SDL_DestroyWindow(window);
				window = IntPtr.Zero;
			}

			if (renderer != IntPtr.Zero)
			{
				SDL_DestroyRenderer(renderer);
				renderer = IntPtr.Zero;
			}
		}
	}
}
