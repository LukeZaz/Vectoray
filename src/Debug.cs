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
// Exceptions: as a substitute for the lack of panic!(), and as a base class for Result error types, otherwise never
// Result<T, E>: For code that has multiple failure states, or for which any failure is unexpected and invalid
// and should be handled. (TODO: last part questionable; no [must_use] attribute in C#, how to deal with this?)
// Opt<T>: For code that has one failure state and for which failures are expected and valid outcomes.
// (e.g. user input errors)
// Debug.LogError: As part and parcel of Result and Opt failures, to help document and trace problems via the console.
// TODO: Above might not be necessary now since Result error types are exceptions, which *should* enable quality stack tracing.
// Test this.
// Debug.LogWarning: Same as LogError, but for less serious problems or things that might not even be problems.
#endregion

namespace Vectoray
{
    public static class Debug
    {
        public static bool ErrorHasOccurred { get; private set; } = false;

        // TODO: Can result in FormatExceptions if message is an interpolated string for which
        // one of the interpolated values is itself a string that contains curly brackets.
        // Delegates might solve this?
        // see https://stackoverflow.com/questions/6088567/creating-an-alias-for-a-function-name-in-c-sharp
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
        /// Log a yellow warning message to the console using `Debug.LogColored`.
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
        /// Log a red error message to the console using `Debug.LogColored`.
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
        /// Log a message to the console using `Debug.Log` and a given foreground color.
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
            Debug.Log(message, arg0, arg1, arg2, arg3);
            Console.ResetColor();
        }

        #region Extensions

        /// <summary>
        /// Logs a red error message using `Debug.LogError` if `value` is a `None&lt;T&gt;`, then
        /// returns said value for further use.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="message">The message to print.</param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        /// <typeparam name="T">The value's type.</typeparam>
        /// <returns>The given value.</returns>
        public static Opt<T> LogErrorIfNone<T>(
            this Opt<T> value,
            string message,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null)
        {
            if (value is None<T>) LogError(message, arg0, arg1, arg2, arg3);
            return value;
        }

        /// <summary>
        /// Logs a provided error message to the console if this OpenGL error code is anything
        /// except `GLErrorCode.NO_ERROR`.
        /// </summary>
        /// <param name="error">The OpenGL error code to check.</param>
        /// <param name="messageProvider">
        /// The function that provides the message to log. The error code will be passed to this so it can be used
        /// in the error message.
        /// </param>
        /// <param name="arg0">First argument.</param>
        /// <param name="arg1">Second argument.</param>
        /// <param name="arg2">Third argument.</param>
        /// <param name="arg3">Fourth argument.</param>
        public static void LogIfError(
            this Rendering.OpenGL.ErrorCode error,
            Func<Rendering.OpenGL.ErrorCode, string> messageProvider,
            object arg0 = null,
            object arg1 = null,
            object arg2 = null,
            object arg3 = null)
        {
            if (error != Rendering.OpenGL.ErrorCode.NO_ERROR)
                LogError(messageProvider(error), arg0, arg1, arg2, arg3);
        }

        #endregion
    }
}