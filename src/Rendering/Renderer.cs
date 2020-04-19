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
using Vectoray.Rendering.OpenGL;

using static SDL2.SDL;

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
        private Renderer(IntPtr context, IntPtr window) => (this.context, this.window) = (context, window);

        // TODO: Result<T,E> here
        /// <summary>
        /// Create a new renderer using the given window and attributes.
        /// </summary>
        /// <param name="window">The window to create this renderer for.</param>
        /// <param name="attributes">The OpenGL attributes and the values to set them to for this renderer.</param>
        /// <returns>An option representing whether or not the renderer was successfully created.</returns>
        public static Opt<Renderer> CreateRenderer(IntPtr window)
        {
            if (!GL.ConfigAttributesSet)
            {
                Debug.LogError("Cannot create an OpenGL renderer before vital OpenGL attributes have been set.");
                return new None<Renderer>();
            }

            IntPtr context = SDL_GL_CreateContext(window);
            if (context == IntPtr.Zero)
            {
                Debug.LogError($"Failed to create OpenGL context during Renderer creation. SDL error: {SDL_GetError()}");
                return new None<Renderer>();
            }

            return new Renderer(context, window).Some();
        }

        /// <summary>
        /// Create a new renderer using the given window and attributes.
        /// </summary>
        /// <param name="window">The window to create this renderer for.</param>
        /// <param name="attributes">The OpenGL attributes and the values to set them to for this renderer.</param>
        /// <returns>An option representing whether or not the renderer was successfully created.</returns>
        public static Opt<Renderer> CreateRenderer(Window window)
            => window.CreateRenderer();

        ~Renderer()
        {
            // This is safe to call even if this is the current context, *provided it is on the same thread.*
            // See: https://docs.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-wgldeletecontext
            SDL_GL_DeleteContext(context);
        }

        #endregion

        // TODO: Result<E> here.
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
    }
}