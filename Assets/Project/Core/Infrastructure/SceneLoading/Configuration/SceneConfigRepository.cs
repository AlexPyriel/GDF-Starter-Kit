using ConfigRepository;
using UnityEngine;

namespace Assets.Project.Core.Infrastructure.SceneLoading.Configuration
{
    /// <summary>
    /// Repository asset for scene configs keyed by scene type.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/" + nameof(SceneConfigRepository), fileName = nameof(SceneConfigRepository), order = 4)]
    public class SceneConfigRepository : ConfigRepository<ESceneType, SceneConfig> { }
}
