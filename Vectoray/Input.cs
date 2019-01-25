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

using System.Linq;
using System.Collections.Generic;
using static SDL2.SDL;

namespace Vectoray
{
	// TODO: Test the different key states to see if they change and update as they should. [pretty sure I did this already?]
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
			// MouseKeyCode casts are not redundant, despite what VS is saying; I get 'cannot convert' errors if I remove them.
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
		private static KeyState GetKeyState(SDL_Keycode key) => keyStates[key];

		/// <summary>
		/// Returns the KeyState of the given mouse key directly.
		/// A middle-man for other InputManager key state retrieval functions.
		/// </summary>
		/// <param name="key">The mouse key to probe the state of.</param>
		/// <returns>The KeyState for the given mouse key.</returns>
		private static KeyState GetKeyState(MouseKeycode key) => mouseKeyStates[key];

		/// <summary>
		/// Returns a value as to whether or not the specified key is currently pressed.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key is currently pressed.</returns>
		public static bool IsPressed(SDL_Keycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsPressed || state == KeyState.PressedThisFrame);
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key is currently pressed.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key is currently pressed.</returns>
		public static bool IsPressed(MouseKeycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsPressed || state == KeyState.PressedThisFrame);
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key is currently released.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key is currently released.</returns>
		public static bool IsReleased(SDL_Keycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsReleased || state == KeyState.ReleasedThisFrame);
		}

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key is currently released.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key is currently released.</returns>
		public static bool IsReleased(MouseKeycode key)
		{
			KeyState state = GetKeyState(key);
			return (state == KeyState.IsReleased || state == KeyState.ReleasedThisFrame);
		}

		/// <summary>
		/// Returns a value as to whether or not the specified key was just pressed this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key was just pressed this frame.</returns>
		public static bool PressedThisFrame(SDL_Keycode key) => (keyStates[key] == KeyState.PressedThisFrame);

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key was just pressed this frame.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key was just pressed this frame.</returns>
		public static bool PressedThisFrame(MouseKeycode key) => (mouseKeyStates[key] == KeyState.PressedThisFrame);

		/// <summary>
		/// Returns a value as to whether or not the specified key was just released this frame.
		/// </summary>
		/// <param name="key">The key to check.</param>
		/// <returns>A boolean value representing whether or not the key was just released this frame.</returns>
		public static bool ReleasedThisFrame(SDL_Keycode key) => (keyStates[key] == KeyState.ReleasedThisFrame);

		/// <summary>
		/// Returns a value as to whether or not the specified mouse key was just released this frame.
		/// </summary>
		/// <param name="key">The mouse key to check.</param>
		/// <returns>A boolean value representing whether or not the mouse key was just released this frame.</returns>
		public static bool ReleasedThisFrame(MouseKeycode key) => (mouseKeyStates[key] == KeyState.ReleasedThisFrame);

		#endregion
	}
}