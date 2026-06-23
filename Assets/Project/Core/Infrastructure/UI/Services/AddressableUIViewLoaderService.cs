using AddressableAssetLoader;
using Assets.Project.Core.Infrastructure.UI.Views;

namespace Assets.Project.Core.Infrastructure.UI.Services
{
    /// <summary>
    /// Loads authored UI prefabs and exposes their shared root view component.
    /// </summary>
    public sealed class AddressableUIViewLoaderService : AddressablePrefabsLoaderService<View> { }
}