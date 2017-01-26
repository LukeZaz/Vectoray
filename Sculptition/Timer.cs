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
