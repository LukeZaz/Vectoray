using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vectoray.Entities;

namespace VectorayTests
{
	internal class TestComponent : Component
	{
		public bool startWasRun { get; private set; } = false;
		public bool onUpdateWasRun { get; private set; } = false;
		public bool onEnabledWasRun { get; private set; } = false;
		public bool onDisabledWasRun { get; private set; } = false;
		public bool onDestroyedWasRun { get; private set; } = false;

		protected override void Start() => startWasRun = true;
		protected override void OnUpdate() => onUpdateWasRun = true;
		protected override void OnEnabled() => onEnabledWasRun = true;
		protected override void OnDisabled() => onDisabledWasRun = true;
		protected override void OnDestroyed() => onDestroyedWasRun = true;
	}

	internal class SecondTestComponent : TestComponent { }

	[TestClass]
	public class EntityTests
	{
		[TestCleanup]
		public void PerTestCleanup() => Entity.DestroyAllAndResetStatics();

		#region ID testing
		[TestMethod]
		public void TestEntityGetsIdZero()
		{
			Entity ent = new Entity();
			Assert.AreEqual(0, ent.id);
		}

		[TestMethod]
		public void TestMultipleEntityIds()
		{
			Entity ent = new Entity();
			Entity ent2 = new Entity();
			Assert.AreEqual(0, ent.id);
			Assert.AreEqual(1, ent2.id);
		}

		[TestMethod]
		public void TestIdReuse()
		{
			Entity ent = new Entity();
			Entity ent2 = new Entity();

			ent.Destroy();
			ent = new Entity();

			Assert.AreEqual(0, ent.id);
			Assert.AreEqual(1, ent2.id);
		}

		[TestMethod]
		public void TestEntityGetById()
		{
			Entity ent = new Entity();
			Assert.AreEqual(ent, Entity.GetById(ent.id));
		}
		#endregion

		[TestMethod]
		public void TestAddComponent()
		{
			Entity ent = new Entity();
			Component c = ent.AddComponent<Component>();

			Assert.IsTrue(c.initialized);
			Assert.AreEqual(ent, c.entity);

			Assert.IsTrue(ent.components.Contains(c));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestRepeatedAddComponent()
		{
			// Multiple components of a type are not allowed, and this test ensures that's enforced.
			Entity ent = new Entity();
			ent.AddComponent<Component>();
			ent.AddComponent<Component>();
		}

		[TestMethod]
		public void TestGetComponent()
		{
			Entity ent = new Entity();
			Component c = ent.AddComponent<Component>();
			// Types ARE identification for components; a nice bonus of only allowing one of a kind!
			Assert.AreEqual(c, ent.GetComponent<Component>());
		}

		[TestMethod]
		public void TestDestroyComponent()
		{
			Entity ent = new Entity();
			Component c = ent.AddComponent<Component>();

			ent.DestroyComponent<Component>();

			Assert.IsFalse(ent.components.Contains(c));

			// Also test Component.Destroy here
			Assert.IsFalse(c.initialized);
			Assert.IsFalse(c.enabled);
			Assert.IsNull(c.entity);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void TestInvalidDestroy()
		{
			Entity ent = new Entity();
			ent.DestroyComponent<Component>();
		}

		#region Component class tests
		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void TestComponentSetEnabled()
		{
			Entity ent = new Entity();
			Component c = ent.AddComponent<Component>() as Component;

			c.SetEnabled(false);
			Assert.IsFalse(c.enabled);

			c.SetEnabled(true);
			Assert.IsTrue(c.enabled);

			// Test an invalid use of SetEnabled on uninitialized component
			c = new Component();
			c.SetEnabled(false);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void TestComponentOnXYZ()
		{
			Entity ent = new Entity();
			TestComponent tc = ent.AddComponent<TestComponent>() as TestComponent;

			tc.Update();
			tc.SetEnabled(false);
			tc.SetEnabled(true);
			tc.Destroy();

			Assert.IsTrue(tc.startWasRun);
			Assert.IsTrue(tc.onUpdateWasRun);
			Assert.IsTrue(tc.onDisabledWasRun);
			Assert.IsTrue(tc.onEnabledWasRun);
			Assert.IsTrue(tc.onDestroyedWasRun);

			// Test an invalid use of Update on uninitialized component
			Component c = new Component();
			c.Update();
		}

		[TestMethod]
		public void TestUpdateAllComponentsOnEntity()
		{
			Entity ent = new Entity();
			TestComponent tc = ent.AddComponent<TestComponent>() as TestComponent;
			SecondTestComponent stc = ent.AddComponent<SecondTestComponent>() as SecondTestComponent;

			ent.UpdateComponents();
			
			Assert.IsTrue(tc.onUpdateWasRun);
			Assert.IsTrue(stc.onUpdateWasRun);
		}
		#endregion

		[TestMethod]
		public void TestDestroy()
		{
			Entity ent = new Entity();
			Component c = ent.AddComponent<Component>();
			TestComponent tc = ent.AddComponent<TestComponent>() as TestComponent;

			ent.Destroy();

			// Ensure that Entity.Destroy calls Destroy on all child components
			Assert.IsNull(c.entity);
			Assert.IsNull(tc.entity);

			Assert.AreEqual(Entity.NO_ENTITY_ID, ent.id);
			Assert.AreEqual(0, ent.components.Count);
		}

		[TestMethod]
		public void TestFreeId()
		{
			new Entity();
			Entity.GetById(0).Destroy();
			Assert.AreEqual(0, Entity.GetFreeId());
		}

		[TestMethod]
		public void TestDestroyAll()
		{
			new Entity();
			Entity.DestroyAllAndResetStatics();
			Assert.IsTrue(Entity.GetGlobalEntityCount() == 0);
		}
	}
}
