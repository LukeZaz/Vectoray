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

using System.Collections.Generic;

namespace Vectoray.Entities
{
	public class Entity
	{
		public int id { get; private set; }
		public List<Component> components { get; private set; } = new List<Component>();

		public Entity()
		{
			id = GetFreeId();
			entities.Add(id, this);
		}

		public void Destroy()
		{
			foreach (Component c in components) c.Destroy();
			FreeId(id);
		}

		/// <summary>
		/// Add a component to this entity.
		/// </summary>
		/// <typeparam name="T">Type of the component to add.</typeparam>
		/// <param name="component">Type of the component to add.</param>
		public void AddComponent<T>() where T : Component, new()
		{
			Component c = new T();
			c.Initialize(this);
		}

		internal void UpdateComponents(object caller)
		{
			foreach (Component c in components) c.Update();
		}

		#region Static functions/variables

		/// <summary>
		/// The highest number ID last used. Used when there are no other free IDs, and incremented each time.
		/// </summary>
		private static int lastHighestID = 0;

		/// <summary>
		/// All entities currently existant.
		/// </summary>
		private static Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
		/// <summary>
		/// A list of freed entity IDs. Used automatically when creating new entities.
		/// </summary>
		private static List<int> freeIds = new List<int>();

		/// <summary>
		/// Get an unused entity ID.
		/// </summary>
		/// <returns>An unused entity ID. Will automatically pull from previously-freed IDs if any are available.</returns>
		public static int GetFreeId()
		{
			if (freeIds.Count > 0)
			{
				int id = freeIds[0];
				freeIds.RemoveAt(0);
				return id;
			}
			else return lastHighestID++;
		}

		/// <summary>
		/// Frees an entity and it's ID from the internal entity tracking lists.
		/// </summary>
		/// <param name="id">ID (and respective entity) to free.</param>
		private static void FreeId(int id)
		{
			if (entities.ContainsKey(id)) entities.Remove(id);
			if (!freeIds.Contains(id)) freeIds.Add(id);
		}

		/// <summary>
		/// Retrieve an entity by its ID.
		/// </summary>
		/// <param name="id">The ID of the entity to find.</param>
		/// <returns>The entity with the given ID.</returns>
		public static Entity GetById(int id) => entities[id];

		#endregion
	}

	public class Component
	{
		/// <summary>
		/// Whether or not this component has been initialized yet.
		/// No regular functions (i.e. Update, Start, OnXYZ, etc) will run unless this is true.
		/// </summary>
		public bool initialized { get; private set; } = false; 
		public bool enabled { get; private set; } = true;
		public Entity entity { get; private set; }

		public Component() { }

		internal void Initialize(Entity parent)
		{
			entity = parent;
			initialized = true;
			Start();
		}

		/// <summary>
		/// Called once when this component is first created.
		/// DOES NOT run if the component does not have a parent entity yet. In this case, it will run when one is first set.
		/// </summary>
		protected virtual void Start() { }

		/// <summary>
		/// Called when this component is enabled.
		/// </summary>
		protected virtual void OnEnabled() { }
		
		/// <summary>
		/// Called when this component is disabled.
		/// </summary>
		protected virtual void OnDisabled() { }

		/// <summary>
		/// Called when either this component or it's parent entity is destroyed.
		/// </summary>
		protected virtual void OnDestroyed() { }

		/// <summary>
		/// Called once per frame.
		/// </summary>
		public void Update()
		{
			if (initialized) OnUpdated();
			else Debugging.LogWarning("'Update' called on uninitialized component!");
		}

		protected virtual void OnUpdated() { }

		/// <summary>
		/// Destroy this component.
		/// </summary>
		public void Destroy()
		{
			// TODO: determine if this needs to throw warnings out if uninitialized (probably not?)
			if (initialized) OnDestroyed();
		}

		/// <summary>
		/// Set whether or not this component is enabled.
		/// This will call either OnEnabled or OnDisabled as appropriate.
		/// Will not run if this component is not yet initialized.
		/// </summary>
		/// <param name="value">The new status for this component.</param>
		public void SetEnabled(bool value)
		{
			// TODO: throw exception instead?
			if (!initialized)
			{
				Debugging.LogWarning("'SetEnabled' called on uninitialized component!");
				return;
			}

			enabled = value;
			if (value == true) OnEnabled();
			else OnDisabled();
		}
	}
}