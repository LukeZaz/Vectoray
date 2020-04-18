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

// Note: Currently using SDL 2.0.12. SDL2-CS should be compatible up to this version as well.

using System;

using Vectoray.Rendering;
using Vectoray.Rendering.OpenGL;

using static SDL2.SDL;

namespace Vectoray
{
    class Program
    {
        #region Variable declaration

        public const int screenWidth = 1024,
            screenHeight = 576;

        #endregion

        static void Main(string[] args)
        {
            Debug.Log("Initializing...");

            if (Initialize() is Some<Window>(Window mainWindow))
            {
                Debug.Log($"Initialization worked fine! Last reported OpenGL error was: {GL.GetError()}");
                GL.LogConnectionInfo();

                GL.ClearColor(0.5f, 0.5f, 0.5f, 1);
                GL.Clear(GLClearMask.COLOR_BUFFER_BIT);
                Debug.Log($"GL error (if any) pre-loop: {GL.GetError()}");

                mainWindow.Raise();
                mainWindow.SwapWindow();
                bool quitMainLoop = false;
                SDL_Event sdlEvent;

                while (!quitMainLoop)
                {
                    while (SDL_PollEvent(out sdlEvent) != 0)
                    {
                        switch (sdlEvent.type)
                        {
                            case SDL_EventType.SDL_QUIT:
                                quitMainLoop = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            Quit();
        }

        // TODO: This should return a result instead
        static Opt<Window> Initialize()
        {
            if (SDL_Init(SDL_INIT_VIDEO) != 0)
            {
                // Since SDL is an external library, error messages need to be printed manually.
                Debug.LogError("Failed to initialize SDL! Error: " + SDL_GetError());
                return new None<Window>();
            }

            // Internal methods already print error messages on failure, however, so only meager detail is needed.
            if (Window.CreateWindow(
                "OpenGL Test",
                SDL_WINDOWPOS_UNDEFINED,
                SDL_WINDOWPOS_UNDEFINED,
                screenWidth,
                screenHeight,
                SDL_WindowFlags.SDL_WINDOW_SHOWN |
                SDL_WindowFlags.SDL_WINDOW_OPENGL |
                SDL_WindowFlags.SDL_WINDOW_RESIZABLE) is Some<Window> windowSome)
            {
                GL.SetConfigAttributes(GLVersion.GL_4_6, SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

                if (windowSome.Unwrap().CreateRenderer() is Some<Renderer>(Renderer renderer))
                {
                    Debug.Log($"Renderer is current context: {renderer.isCurrentContext} "
                            + $"(Pointer is {SDL_GL_GetCurrentContext()})");
                    return windowSome;
                }
                else
                {
                    Debug.LogError("Initialization failed during Renderer creation!");
                    // Technically unnecessary, but it ensures the window doesn't stick around wasting space.
                    windowSome.Unwrap().Free();
                    return new None<Window>();
                }
            }
            else
            {
                Debug.LogError("Initialization failed during window creation!");
                return new None<Window>();
            }
        }

        /// <summary>
        /// Prepares to shut down the program, quitting SDL and pausing the console to allow
        /// error messages to be read.
        /// </summary>
        static void Quit()
        {
            Debug.Log($"GL error (if any) before quitting: {GL.GetError()}");
            if (Debug.errorHasOccurred)
            {
                Debug.Log("One or more errors have occurred; execution has paused before " +
                    "quitting to give time to read them. Press any key to continue.");
                Console.ReadKey(true);
            }

            // This is safe to call even if one or more SDL Init functions failed.
            SDL_Quit();
        }
    }

    public static class Extensions
    {
        /// <summary>
        /// Check if a given integer is within `range`.
        /// Just as for `System.Range`, this check is start-inclusive and end-exclusive.
        /// </summary>
        /// <param name="n">The integer to check.</param>
        /// <param name="range">The range of numbers the integer should be within.</param>
        /// <returns>Whether or not the integer was within the given range.</returns>
        public static bool IsWithin(this int n, Range range) =>
            range.Start.Value <= n && n < range.End.Value;
    }
}