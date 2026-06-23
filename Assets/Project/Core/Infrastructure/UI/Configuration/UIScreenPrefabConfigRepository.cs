using ConfigRepository;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Configuration
{
    /// <summary>
    /// Repository asset that stores addressable screen prefab mappings by screen component type key.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/" + nameof(UIScreenPrefabConfigRepository), fileName = nameof(UIScreenPrefabConfigRepository), order = 6)]
    public class UIScreenPrefabConfigRepository : ConfigRepository<string, UIScreenPrefabConfig> { }
}
