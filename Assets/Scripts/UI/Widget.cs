using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Puzzle.UI
{
    /// <summary>
    /// Base class for every piece of HUD (like Unreal's UUserWidget). A widget wraps one element from a
    /// UXML layout, finds its named children, and hooks itself up to game events.
    /// Register every event subscription with <see cref="OnDispose"/> so it is always cleaned up.
    ///
    /// Typical shape:
    /// <code>
    /// public MyWidget(VisualElement root, Game game) : base(root)
    /// {
    ///     _label = Q&lt;Label&gt;("value");
    ///     game.Resources.Changed += OnChanged;
    ///     OnDispose(() => game.Resources.Changed -= OnChanged);
    /// }
    /// </code>
    /// </summary>
    public abstract class Widget : IDisposable
    {
        /// <summary>The element this widget controls.</summary>
        public VisualElement Root { get; }

        public bool IsVisible => Root.style.display != DisplayStyle.None;

        private readonly List<Action> _cleanup = new List<Action>();

        protected Widget(VisualElement root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root), $"{GetType().Name} was given no root element. Check the element name in the UXML.");
        }

        public void Show() => Root.style.display = DisplayStyle.Flex;

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>Finds a named child, with a clear error if the layout is missing it.</summary>
        protected T Q<T>(string name) where T : VisualElement
        {
            T element = Root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"{GetType().Name}: its layout has no <{typeof(T).Name}> named '{name}'.");
            return element;
        }

        /// <summary>Runs <paramref name="cleanup"/> when this widget is disposed (e.g. to unsubscribe an event).</summary>
        protected void OnDispose(Action cleanup) => _cleanup.Add(cleanup);

        /// <summary>Instantiates a UXML template as a child of <paramref name="parent"/> and returns its root.</summary>
        protected static VisualElement Spawn(VisualTreeAsset template, VisualElement parent)
        {
            if (template == null) throw new ArgumentNullException(nameof(template), "A UI template is not assigned on the GameHUD.");

            TemplateContainer instance = template.Instantiate();
            parent.Add(instance);
            return instance;
        }

        public virtual void Dispose()
        {
            for (int i = _cleanup.Count - 1; i >= 0; i--)
            {
                _cleanup[i]();
            }
            _cleanup.Clear();
        }
    }
}
