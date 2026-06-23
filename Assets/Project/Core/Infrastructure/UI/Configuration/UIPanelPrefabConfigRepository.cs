using ConfigRepository;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.UI.Configuration
{
    /// <summary>
    /// Repository asset that stores addressable panel prefab mappings by panel component type key.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/" + nameof(UIPanelPrefabConfigRepository), fileName = nameof(UIPanelPrefabConfigRepository), order = 7)]
    public class UIPanelPrefabConfigRepository : ConfigRepository<string, UIPanelPrefabConfig> { }
}
