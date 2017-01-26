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

using static SDL2.SDL;

namespace Sculptition
{
	public class Timer
	{
		public uint startTime { get; private set; }
		public uint pausedTime { get; private set; }

		public bool hasStarted { get; private set; }
		public bool paused { get; private set; }

		/// <summary>
		/// Create a new timer.
		/// </summary>
		/// <param name="startNow">If true, timer will begin immediately after creation.</param>
		public Timer(bool startNow = false)
		{
			startTime = 0;
			pausedTime = 0;

			hasStarted = false;
			paused = false;

			if (startNow) Start();
		}

		/// <summary>
		/// Begin the timer.
		/// </summary>
		public void Start()
		{
			hasStarted = true;
			paused = false;

			startTime = SDL_GetTicks();
			pausedTime = 0;
		}

		/// <summary>
		/// Stop and reset the timer.
		/// </summary>
		public void Stop()
		{
			hasStarted = false;
			paused = false;

			startTime = 0;
			pausedTime = 0;
		}

		/// <summary>
		/// Pause this timer if it has been started and is not already paused.
		/// </summary>
		public void Pause()
		{
			if (hasStarted && !paused)
			{
				paused = true;

				pausedTime = SDL_GetTicks() - startTime;
				startTime = 0;
			}
		}

		/// <summary>
		/// Unpause this timer if it has been started and paused.
		/// </summary>
		public void Unpause()
		{
			if (hasStarted && paused)
			{
				paused = false;

				startTime = SDL_GetTicks() - pausedTime;
				pausedTime = 0;
			}
		}

		/// <summary>
		/// Toggle whether or not this timer is paused.
		/// </summary>
		public void TogglePause()
		{
			if (paused) Unpause();
			else Pause();
		}

		/// <summary>
		/// Get the timers current time.
		/// </summary>
		/// <param name="inSeconds">If true, output will be in seconds instead of miliseconds.</param>
		/// <returns>The current counted time.</returns>
		public uint GetTime(bool inSeconds = false)
		{
			uint time = 0;

			if (hasStarted)
			{
				if (paused)
				{
					time = pausedTime;
				}
				else
				{
					time = SDL_GetTicks() - startTime;
				}
			}

			if (!inSeconds) return time;
			else return time / 1000;
		}
	}
}
