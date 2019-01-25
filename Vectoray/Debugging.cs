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

using System;
using System.Text;

namespace Vectoray
{
	using static OpenGL.Definitions;
	using static OpenGL.Definitions.GLenum;
	using static OpenGL.Definitions.GLboolean;

	public static class Debugging
	{
		/// <summary>
		/// Write a message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void Log(string message, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
			=> Console.WriteLine(message, arg0, arg1, arg2, arg3);

		/// <summary>
		/// Write an error message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void LogError(string message, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
			=> LogColored(message, ConsoleColor.Red, arg0, arg1, arg2, arg3);

		/// <summary>
		/// Write a warning message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void LogWarning(string message, object arg0 = null, object arg1 = null, object arg2 = null, object arg3 = null)
			=> LogColored(message, ConsoleColor.Yellow, arg0, arg1, arg2, arg3);

		/// <summary>
		/// Write a colored message to the console via WriteLine.
		/// </summary>
		/// <param name="message">The message to display.</param>
		/// <param name="foregroundColor">The foreground color to use.</param>
		/// <param name="arg0">First argument.</param>
		/// <param name="arg1">Second argument.</param>
		/// <param name="arg2">Third argument.</param>
		/// <param name="arg3">Fourth argument.</param>
		public static void LogColored(string message, ConsoleColor foregroundColor, object arg0 = null, object arg1 = null, object arg2 = null,
			object arg3 = null)
		{
			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message, arg0, arg1, arg2, arg3);
			Console.ResetColor();
		}

		/// <summary>
		/// Writes given program info log to the console.
		/// </summary>
		/// <param name="program">The program to write the info log of.</param>
		public static void WriteProgramLog(GLuint program)
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
			else LogError($"GLuint {(uint)program} is not a program!");
		}

		/// <summary>
		/// Writes given shader info log to the console.
		/// </summary>
		/// <param name="shader">Shader to write the info log of.</param>
		public static void WriteShaderLog(GLuint shader)
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
			else LogError($"GLuint {(uint)shader} is not a shader!");
		}
	}
}
