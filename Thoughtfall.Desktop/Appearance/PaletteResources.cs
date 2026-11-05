namespace Thoughtfall.Desktop.Appearance;

using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

/// <summary>Creates independent Fluent brushes from the bundled definitions for each palette.</summary>
internal sealed class PaletteResources : ResourceDictionary
{
    /// <summary>The palette's base colours, before Fluent's derived brushes are resolved.</summary>
    public ColorPaletteResources FluentColours { get; set; } = new();

    internal void Initialise(ThemeVariant baseVariant)
    {
        // Use Fluent as a brush factory. Its control styles are never installed here.
        // A fresh resource dictionary prevents static brushes leaking between palettes.
        var fluent = new FluentTheme();
        fluent.Palettes[baseVariant] = FluentColours;
        MergedDictionaries.Add(new FluentPaletteResources(fluent.Resources, baseVariant));
    }

    private sealed class FluentPaletteResources : ResourceProvider
    {
        private readonly ThemeVariantScope _scope;
        private readonly ThemeVariant _baseVariant;

        internal FluentPaletteResources(IResourceDictionary resources, ThemeVariant baseVariant)
        {
            _baseVariant = baseVariant;
            // Fluent's dynamic brush bindings resolve against their owner's resources.
            // Keep their Light/Dark lookups local to this palette, too.
            _scope = new ThemeVariantScope { RequestedThemeVariant = baseVariant, Resources = resources };
        }

        public override bool HasResources => _scope.Resources.HasResources;

        public override bool TryGetResource(object key, ThemeVariant? theme, out object? value) =>
            _scope.Resources.TryGetResource(key, _baseVariant, out value);

        protected override void OnAddOwner(IResourceHost owner)
        {
            _scope.ResourcesChanged += OnResourcesChanged;
            base.OnAddOwner(owner);
        }

        protected override void OnRemoveOwner(IResourceHost owner)
        {
            _scope.ResourcesChanged -= OnResourcesChanged;
            base.OnRemoveOwner(owner);
        }

        private void OnResourcesChanged(object? sender, ResourcesChangedEventArgs e) => RaiseResourcesChanged();
    }
}
