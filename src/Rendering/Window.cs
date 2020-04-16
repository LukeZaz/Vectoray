/*
Vectoray; Home-brew 3D C# game engine.
Copyright (C) 2020 LukeZaz

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
using static SDL2.SDL.SDL_WindowFlags;

namespace Vectoray.Rendering
{
    public class Window
    {
        #region Variable & property declaration

        private IntPtr windowPointer;

        public Renderer renderer { get; private set; }

        /// <summary>
        /// Whether or not this window has been fully created and initialized.
        /// </summary>
        public bool initialized => windowPointer != IntPtr.Zero;

        /// <summary>
        /// Whether or not this window is in fullscreen mode.
        /// 
        /// Different from borderless fullscreen in that this results in a video mode change.
        /// </summary>
        public bool isFullscreen => CheckFlag(SDL_WINDOW_FULLSCREEN);
        /// <summary>
        /// Whether or not this window is in borderless fullscreen mode.
        /// 
        /// Different from regular fullscreen in that this is effectively windowed mode,
        /// but scaled to cover the monitor and with window decorations removed.
        /// </summary>
        public bool isFullscreenBorderless => CheckFlag(SDL_WINDOW_FULLSCREEN_DESKTOP);
        /// <summary>
        /// Whether or not this window is in regular windowed mode.
        /// 
        /// Effectively equal to checking both <code>isFullscreen</code> and <code>isFullscreenBorderless</code> at once.
        /// </summary>
        public bool isWindowed => !isFullscreen && !isFullscreenBorderless;
        /// <summary>
        /// Whether or not this window can support an OpenGL context.
        /// </summary>
        public bool supportsOpenGL => CheckFlag(SDL_WINDOW_OPENGL);
        /// <summary>
        /// Whether or not this window is hidden.
        /// </summary>
        public bool isHidden => CheckFlag(SDL_WINDOW_HIDDEN);
        /// <summary>
        /// Whether or not this window has window decorations enabled (i.e. a border).
        /// </summary>
        public bool hasDecoration => !CheckFlag(SDL_WINDOW_BORDERLESS);
        /// <summary>
        /// Whether or not this window is resizable.
        /// </summary>
        public bool isResizable => CheckFlag(SDL_WINDOW_RESIZABLE);
        /// <summary>
        /// Whether or not this window is minimized.
        /// </summary>
        public bool isMinimized => CheckFlag(SDL_WINDOW_MINIMIZED);
        /// <summary>
        /// Whether or not this window is maximized.
        /// </summary>
        public bool isMaximized => CheckFlag(SDL_WINDOW_MAXIMIZED);
        /// <summary>
        /// Whether or not this window has grabbed input. If true, this means both that
        /// the mouse is currently locked to the window and that it has keyboard focus.
        /// </summary>
        public bool inputGrabbed => CheckFlag(SDL_WINDOW_INPUT_GRABBED);
        /// <summary>
        /// Whether or not this window has input focus.
        /// 
        /// (This does not include mouse focus.)
        /// </summary>
        public bool hasInputFocus => CheckFlag(SDL_WINDOW_INPUT_FOCUS);
        /// <summary>
        /// Whether or not the mouse is currently hovering over this window.
        /// </summary>
        public bool mouseIsHovering => CheckFlag(SDL_WINDOW_MOUSE_FOCUS);
        /// <summary>
        /// Whether or not this window is set to use High DPI mode, provided it is supported.
        /// </summary>
        public bool usesHighDPI => CheckFlag(SDL_WINDOW_ALLOW_HIGHDPI);

        #endregion

        #region Lifecycle functionality (constructors, finalizers, etc)

        /// <summary>
        /// Create a new window with the given pointer.
        /// </summary>
        private Window(IntPtr windowPointer) => this.windowPointer = windowPointer;

        // TODO: Once Results are implemented, this should return Result<Window, WindowError>.
        /// <summary>
        /// Create a window with the given values.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="x">The starting position of the window on the X axis.</param>
        /// <param name="y">The starting position of the window on the Y axis.</param>
        /// <param name="w">The width of the window.</param>
        /// <param name="h">The height of the window.</param>
        /// <param name="flags">SDL_WindowFlags to enable on this window.</param>
        /// <returns>An option representing whether or not window creation was successful.</returns>
        public static Opt<Window> CreateWindow(
            string title = "Untitled",
            int x = SDL_WINDOWPOS_UNDEFINED,
            int y = SDL_WINDOWPOS_UNDEFINED,
            int w = 800,
            int h = 600,
            SDL_WindowFlags flags = 0)
        {
            Window window = new Window(SDL_CreateWindow(title, x, y, w, h, flags));
            if (!window.initialized)
            {
                Debug.LogError($"Failed to create window '{title}'! SDL error: {SDL_GetError()}");
                return new None<Window>();
            }
            else return new Some<Window>(window);
        }

        /// <summary>
        /// Destroy this window. Does nothing if this window is not initialized.
        /// Automatically called by this object's finalizer.
        /// </summary>
        public void Free()
        {
            if (initialized)
            {
                SDL_DestroyWindow(windowPointer);
                windowPointer = IntPtr.Zero;
            }
            else Debug.LogError("Cannot call Window.Free() on an uninitialized window.");
        }

        ~Window() => Free();

        #endregion

        #region Rendering

        // TODO: Result<T,E> here
        /// <summary>
        /// Create a new OpenGL-based renderer for this window.
        /// </summary>
        /// <returns>An option representing whether or not the Renderer was successfully created.</returns>
        public Opt<Renderer> CreateRenderer()
        {
            if (this.renderer != null)
            {
                Debug.LogError("Cannot create an OpenGL context for a window that already has one.");
                return new None<Renderer>();
            }

            if (!supportsOpenGL)
            {
                Debug.LogError("Cannot create an OpenGL context for a window that does not support it.");
                return new None<Renderer>();
            }

            // TODO: Once this is changed to a result, the error that this would return should be wrapped
            // and returned instead of just a detail-lacking 'None' value.
            if (Renderer.CreateRenderer(windowPointer) is Some<Renderer> some)
            {
                this.renderer = some.Unwrap();
                return some;
            }
            else return new None<Renderer>();
        }

        #endregion

        /// <summary>
        /// Get the width and height of this window.
        /// </summary>
        /// <returns>A tuple of the width & height of this window, or null if this window is not yet initialized.</returns>
        public (int, int)? GetSize()
        {
            if (!initialized)
            {
                Debug.LogError($"Cannot use GetSize method on uninitialized window '{SDL_GetWindowTitle(windowPointer)}'.");
                return null;
            }

            int width, height;
            SDL_GetWindowSize(windowPointer, out width, out height);
            return (width, height);
        }

        /// <summary>
        /// Raises this window above others and gets input focus.
        /// </summary>
        public void Raise()
        {
            if (initialized) SDL_RaiseWindow(windowPointer);
            else Debug.LogError($"Cannot use Raise method on uninitialized window '{SDL_GetWindowTitle(windowPointer)}'.");
        }

        public void SwapWindow()
        {
            if (!initialized)
                Debug.LogError($"Cannot use SwapWindow method on uninitialized window '{SDL_GetWindowTitle(windowPointer)}'.");
            else if (!supportsOpenGL)
                Debug.LogError("Cannot use SwapWindow method on OpenGL-incompatible"
                            + $" window '{SDL_GetWindowTitle(windowPointer)}'.");
            else SDL_GL_SwapWindow(windowPointer);
        }

        /// <summary>
        /// Check to see if a given SDL_WindowFlags flag is set for this window.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns>Whether or not the flag was set for this window.
        /// 
        /// Also returns false if this window is not yet initialized.</returns>
        private bool CheckFlag(SDL_WindowFlags flag) =>
            initialized ? (SDL_GetWindowFlags(windowPointer) & (uint)flag) != 0 : false;
    }
}