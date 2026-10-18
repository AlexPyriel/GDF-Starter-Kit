using System.Reflection;
using Assets.Project.Features.Tutorial.Framework.Presentation.Views;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>
    /// Verifies <see cref="TutorialOverlayView"/> dim-strength behavior: <see cref="TutorialOverlayView.Hide"/>
    /// restores the prefab alpha even if a step changed it before <c>Awake</c> ran (the service creates
    /// the overlay under a disabled root — <c>Awake</c> only arrives on the first show).
    /// </summary>
    public sealed class TutorialOverlayViewTests
    {
        private const float PREFAB_ALPHA = 0.8f;

        private GameObject _root;
        private Image _dimmer;
        private TutorialOverlayView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TutorialOverlay", typeof(RectTransform));
            GameObject dimmerObject = new GameObject("Dimmer", typeof(RectTransform));
            dimmerObject.transform.SetParent(_root.transform, false);
            _dimmer = dimmerObject.AddComponent<Image>();
            _dimmer.color = new Color(0f, 0f, 0f, PREFAB_ALPHA);

            _view = _root.AddComponent<TutorialOverlayView>();

            // Awake is not called in EditMode and fields are serialized: set them via SerializedObject.
            SerializedObject serialized = new SerializedObject(_view);
            serialized.FindProperty("_dimmer").objectReferenceValue = _dimmer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Hide_StepChangedAlphaBeforeLateAwake_RestoresPrefabAlpha()
        {
            // Service order: Hide on creation, step sets a black screen, Awake arrives with the first show.
            _view.Hide();
            _view.SetDimAlpha(1f);
            InvokeAwake();

            _view.Hide();

            Assert.That(_dimmer.color.a, Is.EqualTo(PREFAB_ALPHA).Within(1e-4f));
        }

        [Test]
        public void Hide_FirstHideBeforeAwake_KeepsPrefabAlpha()
        {
            _view.Hide();
            InvokeAwake();
            _view.SetDimAlpha(0f);

            _view.Hide();

            Assert.That(_dimmer.color.a, Is.EqualTo(PREFAB_ALPHA).Within(1e-4f));
        }

        private void InvokeAwake()
        {
            typeof(TutorialOverlayView)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_view, null);
        }
    }
}
