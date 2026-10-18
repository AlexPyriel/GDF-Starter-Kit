using System.Collections.Generic;
using Assets.Project.Features.Tutorial.Framework.Enums;
using Assets.Project.Features.Tutorial.Framework.Interfaces;
using UnityEngine.UI;
using VContainer.Unity;

namespace Assets.Project.Features.Tutorial.Framework.Services
{
    /// <summary>
    /// Highlight-group registry (root singleton, EAGER via <see cref="IInitializable"/>). The tag
    /// components (<c>TutorialHighlightGroup</c>) are scene MonoBehaviours that DI does not create, so
    /// they reach the registry through the static bridge <see cref="Current"/> (set in the ctor). The
    /// registry is created at app startup (entry point) so <see cref="Current"/> is ready BEFORE scenes
    /// load and before OnEnable on groups fires. A step resolves <see cref="ITutorialHighlightRegistry"/>
    /// via ordinary injection — same instance.
    /// </summary>
    public sealed class TutorialHighlightRegistry : ITutorialHighlightRegistry, IInitializable
    {
        private static readonly IReadOnlyList<Graphic> Empty = new Graphic[0];

        /// <summary>Current registry for access from scene tag components (static MonoBehaviour <-> DI bridge).</summary>
        public static ITutorialHighlightRegistry Current { get; private set; }

        private readonly Dictionary<ETutorialHighlightKey, ITutorialHighlightSource> _sources = new Dictionary<ETutorialHighlightKey, ITutorialHighlightSource>();

        /// <summary>Creates the registry and makes it current for scene tags.</summary>
        public TutorialHighlightRegistry()
        {
            Current = this;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            // Empty: only needed as an entry point to force EAGER construction (the ctor already set Current).
        }

        /// <inheritdoc />
        public void Register(ITutorialHighlightSource source)
        {
            if (source == null || source.HighlightKey == ETutorialHighlightKey.None)
            {
                return;
            }

            _sources[source.HighlightKey] = source;
        }

        /// <inheritdoc />
        public void Unregister(ITutorialHighlightSource source)
        {
            if (source == null || source.HighlightKey == ETutorialHighlightKey.None)
            {
                return;
            }

            if (_sources.TryGetValue(source.HighlightKey, out ITutorialHighlightSource registered) && ReferenceEquals(registered, source))
            {
                _sources.Remove(source.HighlightKey);
            }
        }

        /// <inheritdoc />
        public bool TryGetTargets(ETutorialHighlightKey key, out IReadOnlyList<Graphic> targets)
        {
            if (key != ETutorialHighlightKey.None && _sources.TryGetValue(key, out ITutorialHighlightSource source))
            {
                targets = source.HighlightTargets ?? Empty;
                return true;
            }

            targets = Empty;
            return false;
        }
    }
}
