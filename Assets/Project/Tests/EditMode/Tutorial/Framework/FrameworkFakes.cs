using System;
using System.Collections.Generic;
using System.Threading;
using Assets.Project.Core.Infrastructure.SceneLoading;
using Assets.Project.Core.Infrastructure.SceneLoading.Interfaces;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using Assets.Project.Features.Tutorial.Progress;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Assets.Project.Tests.EditMode.Tutorial.Framework
{
    /// <summary>Fake step: records its lifecycle in a shared log, optionally blocks on a gate (for cancellation).</summary>
    internal sealed class FakeStep : ITutorialStep
    {
        private readonly List<string> _log;
        private readonly string _name;
        private readonly UniTaskCompletionSource _gate;

        public FakeStep(
            List<string> log,
            string name,
            ESceneType requiredScene = ESceneType.Home,
            UniTaskCompletionSource gate = null)
        {
            _log = log;
            _name = name;
            RequiredScene = requiredScene;
            _gate = gate;
        }

        public ESceneType RequiredScene { get; }
        public int ExecuteCount { get; private set; }
        public int DisposeCount { get; private set; }

        // Transient fake does not clone — the runner executes the same instance (counters stay observable).
        public ITutorialStep CreateRuntimeInstance() => this;

        public async UniTask Execute(CancellationToken token)
        {
            ExecuteCount++;
            _log.Add("exec:" + _name);

            if (_gate != null)
            {
                await _gate.Task.AttachExternalCancellation(token);
            }
        }

        public void Dispose()
        {
            DisposeCount++;
            _log.Add("dispose:" + _name);
        }
    }

    /// <summary>Fake tutorial definition: id/skip/steps in memory (no ScriptableObject).</summary>
    internal sealed class FakeTutorialDefinition : ITutorialDefinition
    {
        public FakeTutorialDefinition(string tutorialId, bool skipTutorial, IReadOnlyList<ITutorialStepEntry> steps)
        {
            TutorialId = tutorialId;
            SkipTutorial = skipTutorial;
            Steps = steps;
        }

        public string TutorialId { get; }
        public IReadOnlyList<ITutorialStepEntry> Steps { get; }
        public bool SkipTutorial { get; }
    }

    /// <summary>Tutorial list entry for tests: wraps a step and carries positional skip/checkpoint flags.</summary>
    internal sealed class FakeStepEntry : ITutorialStepEntry
    {
        public FakeStepEntry(ITutorialStep step, bool isDisabled = false, bool shouldSaveProgress = false)
        {
            Step = step;
            IsDisabled = isDisabled;
            ShouldSaveProgress = shouldSaveProgress;
        }

        public ITutorialStep Step { get; }
        public bool IsDisabled { get; }
        public bool ShouldSaveProgress { get; }
    }

    /// <summary>Fake waiter: completes immediately and logs the awaited scene.</summary>
    internal sealed class FakeSceneWaiter : ITutorialSceneWaiter
    {
        private readonly List<string> _log;

        public FakeSceneWaiter(List<string> log)
        {
            _log = log;
        }

        public bool IsActive(ESceneType scene)
        {
            return true;
        }

        public UniTask WaitForScene(ESceneType scene, CancellationToken token)
        {
            _log.Add("wait:" + scene);
            return UniTask.CompletedTask;
        }
    }

    /// <summary>Fake holder: returns the given resolver, logging when it is accessed (used to verify inject order). Publishes nothing.</summary>
    internal sealed class FakeSceneScopeHolder : IActiveSceneScopeHolder
    {
        private readonly List<string> _log;
        private readonly IObjectResolver _resolver;

        public FakeSceneScopeHolder(List<string> log, IObjectResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public ESceneType? ActiveScene => ESceneType.Home;

        public IObjectResolver ActiveResolver
        {
            get
            {
                _log.Add("inject-resolver");
                return _resolver;
            }
        }

        public void SetActive(ESceneType scene, IObjectResolver resolver)
        {
        }
    }

    /// <summary>In-memory progress: cursor/completion in memory; the default starting cursor is set in the constructor.</summary>
    internal sealed class FakeProgress : ITutorialProgressService
    {
        private readonly List<string> _log;
        private readonly int _startCursor;
        private readonly Dictionary<string, int> _cursors = new();
        private readonly HashSet<string> _completed = new();

        public FakeProgress(List<string> log, int startCursor = 0)
        {
            _log = log;
            _startCursor = startCursor;
        }

        public bool IsCompleted(string tutorialKey)
        {
            return _completed.Contains(tutorialKey);
        }

        public void MarkCompleted(string tutorialKey)
        {
            _log.Add("complete:" + tutorialKey);
            _completed.Add(tutorialKey);
        }

        public int GetCursor(string tutorialKey)
        {
            return _cursors.TryGetValue(tutorialKey, out int value) ? value : _startCursor;
        }

        public void SetCursor(string tutorialKey, int stepIndex)
        {
            _log.Add("cursor:" + stepIndex);
            _cursors[tutorialKey] = stepIndex;
        }

        public void Reset(string tutorialKey)
        {
            _completed.Remove(tutorialKey);
            _cursors.Remove(tutorialKey);
        }

        public void ResetAll()
        {
            _completed.Clear();
            _cursors.Clear();
        }
    }

    /// <summary>Fake overlay: logs calls; the "advance" click is triggered via <see cref="RaiseAdvance"/>.</summary>
    internal sealed class FakeOverlay : ITutorialOverlay
    {
        private readonly List<string> _log;
        private UniTaskCompletionSource _advance;
        private RectTransform _contentRoot;

        public FakeOverlay(List<string> log)
        {
            _log = log;
        }

        public RectTransform ContentRoot
        {
            get
            {
                if (_contentRoot == null)
                {
                    _contentRoot = new GameObject("FakeContentRoot", typeof(RectTransform)).GetComponent<RectTransform>();
                }

                return _contentRoot;
            }
        }

        public void Show() => _log.Add("overlay:show");
        public void Hide() => _log.Add("overlay:hide");
        public void Highlight(IReadOnlyList<Graphic> targets) => _log.Add("overlay:highlight:" + (targets?.Count ?? 0));
        public void Highlight(IReadOnlyList<RectTransform> regions) => _log.Add("overlay:highlight-rects:" + (regions?.Count ?? 0));
        public void RaiseAbove(IReadOnlyList<RectTransform> elements) => _log.Add("overlay:raise:" + (elements?.Count ?? 0));
        public void ClearHighlight() => _log.Add("overlay:clear");
        public void SetDimAlpha(float alpha) => _log.Add("overlay:dim:" + alpha);

        public UniTask WaitForAdvanceClick(CancellationToken token)
        {
            _advance = new UniTaskCompletionSource();
            return _advance.Task.AttachExternalCancellation(token);
        }

        /// <summary>Simulates an "advance" click on the dim.</summary>
        public void RaiseAdvance() => _advance?.TrySetResult();

        /// <summary>Destroys the created ContentRoot (call in TearDown).</summary>
        public void Cleanup()
        {
            if (_contentRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_contentRoot.gameObject);
                _contentRoot = null;
            }
        }
    }

    /// <summary>In-memory highlight registry: key -> targets.</summary>
    internal sealed class FakeHighlightRegistry : ITutorialHighlightRegistry
    {
        private readonly Dictionary<ETutorialHighlightKey, IReadOnlyList<Graphic>> _sources = new();

        public void Register(ITutorialHighlightSource source)
        {
            if (source != null && source.HighlightKey != ETutorialHighlightKey.None)
            {
                _sources[source.HighlightKey] = source.HighlightTargets;
            }
        }

        public void Unregister(ITutorialHighlightSource source)
        {
            if (source != null && source.HighlightKey != ETutorialHighlightKey.None)
            {
                _sources.Remove(source.HighlightKey);
            }
        }

        public void Set(ETutorialHighlightKey key, IReadOnlyList<Graphic> targets)
        {
            _sources[key] = targets;
        }

        public bool TryGetTargets(ETutorialHighlightKey key, out IReadOnlyList<Graphic> targets)
        {
            if (_sources.TryGetValue(key, out targets))
            {
                return true;
            }

            targets = new Graphic[0];
            return false;
        }
    }
}
