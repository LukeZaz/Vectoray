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
using System.Runtime.InteropServices;
using static SDL2.SDL;
using static SDL2.SDL_image;
using static SDL2.SDL_ttf;

namespace Sculptition
{
	// TODO: Subclass of Texture designed for and providing functionality for text rendering
	public class Texture
	{
		public IntPtr texture { get; private set; }

		public int width { get; private set; }
		public int height { get; private set; }

		#region Constructors & destructors

		public Texture()
		{
			texture = IntPtr.Zero;

			width = 0;
			height = 0;
		}

		/// <summary>
		/// Creates an empty Texture object, then automatically calls LoadFromFile with the given values.
		/// Be aware that this will not return a boolean for load success.
		/// </summary>
		/// <param name="path">Path to the image to load.</param>
		/// <param name="renderer">Renderer to be used for texture conversion.</param>
		/// <param name="setColorKey">Whether or not to color key the texture.</param>
		/// <param name="colorKeyR">If colorkeying, the R value to be used for the key.</param>
		/// <param name="colorKeyG">If colorkeying, the G value to be used for the key.</param>
		/// <param name="colorKeyB">If colorkeying, the B value to be used for the key.</param>
		public Texture(string path, IntPtr renderer, SDL_bool setColorKey = SDL_bool.SDL_FALSE,
			byte colorKeyR = 0, byte colorKeyG = 0, byte colorKeyB = 0, bool logErrors = true) : this()
		{
			LoadFromFile(path, renderer, setColorKey, colorKeyR, colorKeyG, colorKeyB, logErrors);
		}

		~Texture()
		{
			Free();
		}

		#endregion

		/// <summary>
		/// Loads and converts an image from a file to be used for this texture.
		/// </summary>
		/// <param name="path">Path to the image to load.</param>
		/// <param name="renderer">Renderer to be used for texture conversion.</param>
		/// <param name="setColorKey">Whether or not to color key the texture.</param>
		/// <param name="colorKeyR">If colorkeying, the R value to be used for the key.</param>
		/// <param name="colorKeyG">If colorkeying, the G value to be used for the key.</param>
		/// <param name="colorKeyB">If colorkeying, the B value to be used for the key.</param>
		/// <param name="logErrors">Whether or not to log errors to the console.</param>
		/// <returns>Whether or not the texture was created and is present.</returns>
		public bool LoadFromFile(string path, IntPtr renderer, SDL_bool setColorKey = SDL_bool.SDL_FALSE,
			byte colorKeyR = 0, byte colorKeyG = 0, byte colorKeyB = 0, bool logErrors = true)
		{
			// Deallocate current texture if present
			Free();

			// Load image
			IntPtr loadedSurface = IMG_Load(path);

			if (loadedSurface == IntPtr.Zero)
			{
				if (logErrors) Console.WriteLine("Failed to load image at path '{0}'. SDL error: {1}", path, SDL_GetError());
			}
			else
			{
				// Get SDL_Surface to access its properties
				SDL_Surface loadedSurfaceProcessed = (SDL_Surface)Marshal.PtrToStructure(loadedSurface, typeof(SDL_Surface));

				SDL_SetColorKey(loadedSurface, (int)setColorKey, SDL_MapRGB(loadedSurfaceProcessed.format, colorKeyR, colorKeyG, colorKeyB));

				// Convert to texture
				texture = SDL_CreateTextureFromSurface(renderer, loadedSurface);
				
				if (texture == IntPtr.Zero)
				{
					if (logErrors) Console.WriteLine("Failed to create texture from image at path '{0}'. SDL error: {1}", path, SDL_GetError());
				}
				else
				{
					width = loadedSurfaceProcessed.w;
					height = loadedSurfaceProcessed.h;
				}

				SDL_FreeSurface(loadedSurface);
			}

			return texture != IntPtr.Zero;
		}

		/// <summary>
		/// Renders text using given font and converts it for use with this texture.
		/// </summary>
		/// <param name="font">Font to render into texture.</param>
		/// <param name="renderer">Renderer to use.</param>
		/// <param name="text">Text to render.</param>
		/// <param name="textColor">Color to use for text.</param>
		/// <param name="logErrors">Whether or not to log errors to the console.</param>
		/// <returns>Whether or not the texture was created and is present.</returns>
		public bool LoadFromRenderedFont(IntPtr font, IntPtr renderer, string text, SDL_Color textColor, bool logErrors = true)
		{
			// Deallocate current texture if present
			Free();

			IntPtr loadedSurface = TTF_RenderText_Blended_Wrapped(font, text, textColor, 600);

			if (loadedSurface == IntPtr.Zero)
			{
				if (logErrors) Console.WriteLine("Failed to render text surface. SDL error: " + SDL_GetError());
			}
			else
			{
				// Get SDL_Surface to access its properties
				SDL_Surface loadedSurfaceProcessed = (SDL_Surface)Marshal.PtrToStructure(loadedSurface, typeof(SDL_Surface));

				// Convert to texture
				texture = SDL_CreateTextureFromSurface(renderer, loadedSurface);

				if (texture == IntPtr.Zero)
				{
					if (logErrors) Console.WriteLine("Failed to create texture from rendered font. SDL error: " + SDL_GetError());
				}
				else
				{
					width = loadedSurfaceProcessed.w;
					height = loadedSurfaceProcessed.h;
				}

				SDL_FreeSurface(loadedSurface);
			}

			return texture != IntPtr.Zero;
		}

		/// <summary>
		/// Renders the texture at the given coordinates with default width and height.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		/// <param name="renderer">Renderer to use.</param>
		public void Render(int x, int y, IntPtr renderer)
		{
			IntPtr renderRectRaw = Sculptition.createRect(x, y, width, height).getRaw();

			SDL_RenderCopy(renderer, texture, IntPtr.Zero, renderRectRaw);

			Marshal.FreeHGlobal(renderRectRaw);
		}

		/// <summary>
		/// Free memory taken by and then destroy the texture if present.
		/// Automatically called by deconstructor.
		/// </summary>
		public void Free()
		{
			if (texture != IntPtr.Zero)
			{
				SDL_DestroyTexture(texture);
				texture = IntPtr.Zero;

				width = 0;
				height = 0;
			}
		}
	}
}
