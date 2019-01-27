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
using System.Collections.Generic;
using System.Linq;

namespace Vectoray.Entities
{
	public class Entity
	{
		public const int NO_ENTITY_ID = -1;

		/// <summary>
		/// Unique identifier for this entity.
		/// </summary>
		public int id { get; private set; } = NO_ENTITY_ID;

		/// <summary>
		/// Components currently on this entity.
		/// An entity cannot have more than one of a specific type of component.
		/// </summary>
		public List<Component> components { get; private set; } = new List<Component>();

		// TODO: Constructor that accepts generic component types to add
		/// <summary>
		/// Create a new entity and automatically assign an ID to it.
		/// </summary>
		public Entity()
		{
			id = GetFreeId();
			entities.Add(id, this);
		}

		/// <summary>
		/// Destroy this entity and all of it's components.
		/// </summary>
		public void Destroy()
		{
			foreach (Component c in components) c.Destroy();
			components.Clear();

			FreeId(id);
			id = NO_ENTITY_ID;
		}

		/// <summary>
		/// Add a component of a given type to this entity.
		/// Only one of a specific type of component can be on an entity at once.
		/// </summary>
		/// <typeparam name="T">Type of the component to add.</typeparam>
		/// <returns>The component that was added.</returns>
		/// <exception cref="ArgumentException">Thrown if the entity already has a component of this type.</exception>
		public Component AddComponent<T>() where T : Component, new()
		{
			Component c = components.OfType<T>().FirstOrDefault();
			if (c == null)
			{
				c = new T();
				c.Initialize(this);
				components.Add(c);
				return c;
			}
			else throw new ArgumentException($"Entities cannot have multiple of a type of component. [ID: {id}]");
		}

		/// <summary>
		/// Get the component of this type from this entity.
		/// </summary>
		/// <typeparam name="T">The type of the component to get.</typeparam>
		/// <returns>The component of the given type, or null if there is none.</returns>
		public Component GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();

		/// <summary>
		/// Remove the component of this type from the entity, if present.
		/// </summary>
		/// <typeparam name="T">The type of the component to remove.</typeparam>
		/// <exception cref="ArgumentException">Thrown if no component of the given type exists on this entity.</exception>
		public void DestroyComponent<T>() where T : Component
		{
			Component c = components.OfType<T>().FirstOrDefault();
			if (c != null)
			{
				c.Destroy();
				components.Remove(c);
			}
			else throw new ArgumentException($"Entity {id} does not have a component of type {typeof(T).Name} to destroy.");
		}

		/// <summary>
		/// Update all components on this entity.
		/// </summary>
		public void UpdateComponents()
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
		/// Completely reset all static variables, destroying ALL entities in the process and moving back to a clean slate.
		/// This is meant for unit testing; use with extreme caution.
		/// </summary>
		public static void DestroyAllAndResetStatics()
		{
			List<Entity> allEntities = entities.Values.ToList();
			foreach (Entity ent in allEntities)
			{
				ent.Destroy();
			}

			entities.Clear();
			freeIds.Clear();
			lastHighestID = 0;
		}

		/// <summary>
		/// Get a count of all existant entities.
		/// </summary>
		/// <returns>An integer count of how many entities currently exist.</returns>
		public static int GetGlobalEntityCount() => entities.Count();

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
		/// <returns>The entity with the given ID, or null if there was none.</returns>
		public static Entity GetById(int id) => entities.TryGetValue(id, out Entity ent) ? ent : null;

		#endregion
	}

	public class Component
	{
		/// <summary>
		/// Whether or not this component has been initialized yet.
		/// This will be true if the internal Initialization function has been called (typically when an entity first adds this component to itself).
		/// Will also become false when this components' Destroy function is called.
		/// No regular functions (i.e. Update, Start, OnXYZ, etc) will run unless this is true.
		/// </summary>
		public bool initialized { get; private set; } = false; 

		/// <summary>
		/// Whether or not this entity is enabled.
		/// Disabled entities do not run Update calls.
		/// </summary>
		public bool enabled { get; private set; } = true;

		/// <summary>
		/// The parent entity of this component.
		/// </summary>
		public Entity entity { get; private set; }

		public Component() { }

		/// <summary>
		/// Initialize this component, setting it's parent and running it's Start function.
		/// </summary>
		/// <param name="parent">Parent entity for this component.</param>
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
		/// Called once per frame if this component is enabled and initialized.
		/// </summary>
		protected virtual void OnUpdate() { }

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
		/// Checks if this component is initialized and enabled and calls OnUpdate if so.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if component is not initialized.</exception>
		public void Update()
		{
			if (initialized && enabled) OnUpdate();
			else if (initialized) Debugging.LogWarning("'Update' called on disabled component!");
			else throw new InvalidOperationException($"Cannot call Update on uninitialized components. [{entity?.id} -> {GetType().Name}]");
		}

		/// <summary>
		/// Destroy this component.
		/// </summary>
		public void Destroy()
		{
			// TODO: determine if this needs to throw warnings out if uninitialized (probably not?)
			if (initialized)
			{
				OnDestroyed();
				// Fully disable and reset this component to help ensure it gets handled by GC
				entity = null;
				enabled = false;
				initialized = false;
			}
		}

		/// <summary>
		/// Set whether or not this component is enabled.
		/// This will call either OnEnabled or OnDisabled as appropriate.
		/// </summary>
		/// <param name="value">The new status for this component.</param>
		/// <exception cref="InvalidOperationException">Thrown if component is not initialized.</exception>
		public void SetEnabled(bool value)
		{
			if (!initialized) throw new InvalidOperationException($"Cannot use SetEnabled on uninitialized components. [{entity?.id} -> {GetType().Name}]");

			if (enabled == value) return;

			enabled = value;
			if (value == true) OnEnabled();
			else OnDisabled();
		}
	}
}