using System;
using EchoCave.Core;
using EchoCave.Game;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EchoCave.Tests
{
    public class BatFactoryTests
    {
        private static readonly Vector3 PrefabScale = new Vector3(0.5f, 0.5f, 0.5f);

        private GameObject _prefabRoot;
        private GameObject _containerRoot;
        private BatView _prefab;
        private Transform _container;

        [SetUp]
        public void SetUp()
        {
            _prefab = CreateBatView("BatPrefab");
            _prefab.transform.localScale = PrefabScale;
            _prefabRoot = _prefab.gameObject;

            _containerRoot = new GameObject("BatContainer");
            _container = _containerRoot.transform;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_prefabRoot);
            UnityEngine.Object.DestroyImmediate(_containerRoot);
        }

        // A bare BatView with the three children it needs; the serialized fields are wired the way the prefab does.
        private static BatView CreateBatView(string name)
        {
            var root = new GameObject(name);
            var view = root.AddComponent<BatView>();

            GameObject Child(string childName)
            {
                var child = new GameObject(childName);
                child.transform.SetParent(root.transform, false);
                return child;
            }

            var so = new SerializedObject(view);
            so.FindProperty("body").objectReferenceValue = Child("Body");
            so.FindProperty("arrow").objectReferenceValue = Child("Arrow").transform;
            so.FindProperty("silhouette").objectReferenceValue = Child("Silhouette");
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private BatFactory NewFactory(int prewarm = 0) => new BatFactory(_prefab, _container, prewarm);

        private static Bat MakeBat(int id, Direction direction, bool hidden = false) => new Bat(id, new Cell(1, 1), direction, hidden);

        private static bool IsActive(BatView view, string child) => view.transform.Find(child).gameObject.activeSelf;

        private static void AssertArrowAngle(BatView view, float degrees)
        {
            Quaternion expected = Quaternion.Euler(0f, 0f, degrees);

            Assert.AreEqual(0f, Quaternion.Angle(expected, view.transform.Find("Arrow").localRotation), 0.01f);
        }

        [Test]
        public void Get_ReturnsAnActiveViewBoundToTheBat()
        {
            BatView view = NewFactory().Get(MakeBat(7, Direction.Left));

            Assert.IsTrue(view.gameObject.activeSelf);
            Assert.AreEqual(7, view.BatId);
            AssertArrowAngle(view, 90f);
            Assert.IsTrue(IsActive(view, "Body"));
            Assert.IsFalse(IsActive(view, "Silhouette"));
        }

        [Test]
        public void Get_HiddenBat_ShowsTheSilhouetteOnly()
        {
            BatView view = NewFactory().Get(MakeBat(1, Direction.Up, hidden: true));

            Assert.IsFalse(IsActive(view, "Body"));
            Assert.IsFalse(IsActive(view, "Arrow"));
            Assert.IsTrue(IsActive(view, "Silhouette"));
        }

        [Test]
        public void Get_ParentsTheViewUnderTheContainer()
        {
            BatView view = NewFactory().Get(MakeBat(1, Direction.Up));

            Assert.AreSame(_container, view.transform.parent);
        }

        [Test]
        public void Release_DeactivatesTheView_AndGetReusesIt()
        {
            BatFactory factory = NewFactory();
            BatView first = factory.Get(MakeBat(1, Direction.Up));

            factory.Release(first);

            Assert.IsFalse(first.gameObject.activeSelf);
            Assert.AreEqual(1, factory.FreeCount);
            Assert.AreEqual(0, factory.ActiveCount);

            BatView second = factory.Get(MakeBat(9, Direction.Down, hidden: true));

            Assert.AreSame(first, second);
            Assert.AreEqual(9, second.BatId);
            Assert.IsTrue(IsActive(second, "Silhouette"));
            AssertArrowAngle(second, 180f);
            Assert.AreEqual(1, _container.childCount);
        }

        [Test]
        public void Release_RestoresThePrefabPose_NotUnitScale()
        {
            BatFactory factory = NewFactory();
            BatView view = factory.Get(MakeBat(1, Direction.Up));
            view.transform.localScale = Vector3.one * 3f;
            view.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            factory.Release(view);
            BatView reused = factory.Get(MakeBat(2, Direction.Up));

            Assert.AreSame(view, reused);
            Assert.AreEqual(PrefabScale, reused.transform.localScale);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, reused.transform.localRotation), 0.01f);
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveView()
        {
            BatFactory factory = NewFactory();
            factory.Get(MakeBat(1, Direction.Up));
            factory.Get(MakeBat(2, Direction.Down));
            factory.Get(MakeBat(3, Direction.Left));

            factory.ReleaseAll();

            Assert.AreEqual(0, factory.ActiveCount);
            Assert.AreEqual(3, factory.FreeCount);
            for (int i = 0; i < _container.childCount; i++)
                Assert.IsFalse(_container.GetChild(i).gameObject.activeSelf);
        }

        [Test]
        public void Release_Twice_Throws()
        {
            BatFactory factory = NewFactory();
            BatView view = factory.Get(MakeBat(1, Direction.Up));
            factory.Release(view);

            Assert.Throws<InvalidOperationException>(() => factory.Release(view));
            Assert.AreEqual(1, factory.FreeCount, "A double release must not put the view in the pool twice.");
        }

        [Test]
        public void Release_ViewFromAnotherSource_Throws()
        {
            BatView stranger = CreateBatView("Stranger");

            try
            {
                Assert.Throws<InvalidOperationException>(() => NewFactory().Release(stranger));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stranger.gameObject);
            }
        }

        [Test]
        public void Prewarm_CreatesInactiveViewsUnderTheContainer()
        {
            BatFactory factory = NewFactory(prewarm: 4);

            Assert.AreEqual(4, _container.childCount);
            Assert.AreEqual(4, factory.FreeCount);
            Assert.AreEqual(0, factory.ActiveCount);
            for (int i = 0; i < _container.childCount; i++)
                Assert.IsFalse(_container.GetChild(i).gameObject.activeSelf);

            factory.Get(MakeBat(1, Direction.Up));

            Assert.AreEqual(4, _container.childCount, "Get must take a prewarmed view instead of instantiating.");
            Assert.AreEqual(3, factory.FreeCount);
        }

        [Test]
        public void Get_BeyondThePrewarm_InstantiatesMore()
        {
            BatFactory factory = NewFactory(prewarm: 1);

            factory.Get(MakeBat(1, Direction.Up));
            factory.Get(MakeBat(2, Direction.Up));

            Assert.AreEqual(2, _container.childCount);
            Assert.AreEqual(2, factory.ActiveCount);
        }

        [Test]
        public void Constructor_RejectsNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new BatFactory(null, _container));
            Assert.Throws<ArgumentNullException>(() => new BatFactory(_prefab, null));
        }

        [Test]
        public void Get_NullBat_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => NewFactory().Get(null));
        }
    }
}
