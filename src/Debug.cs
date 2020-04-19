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

#region What to use for error handling, and when
// TODO: Write something more expansive here probably
// Exceptions: as a substitute for the lack of panic!(), otherwise never
// Result<T, E>: For code that has multiple failure states, or for which any failure is unexpected and invalid
// and should be handled.
// Opt<T>: For code that has one failure state and for which failures are expected and valid outcomes.
// Debug.LogError: As part and parcel of Result and Opt failures, to help document and trace problems via the console.
// Debug.LogWarning: Same as LogError, but for less serious problems or things that might not even be problems.
#endregion

namespace Vectoray
{
    public static class Debug
    {
        public static bool ErrorHasOccurred { get; private set; } = false;

        /// <summary>
        /// Log a message to the console using Console.WriteLine.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        public static void Log(
            string message,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null) => Console.WriteLine(message, arg0, arg1, arg2, arg3);

        /// <summary>
        /// Log a yellow warning message to the console using Console.WriteLine.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        public static void LogWarning(
            string message,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null) => LogColored(message, ConsoleColor.Yellow, arg0, arg1, arg2, arg3);

        /// <summary>
        /// Log a red error message to the console using Console.WriteLine.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        public static void LogError(
            string message,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null)
        {
            LogColored(message, ConsoleColor.Red, arg0, arg1, arg2, arg3);
            ErrorHasOccurred = true;
        }

        /// <summary>
        /// Log a message to the console using WriteLine and a given foreground color.
        /// </summary>
        /// <param name="message">The message to print.</param>
        /// <param name="color">The foreground color to use.</param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        public static void LogColored(
            string message,
            ConsoleColor color,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message, arg0, arg1, arg2, arg3);
            Console.ResetColor();
        }
    }
}