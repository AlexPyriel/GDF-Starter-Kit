#if UNITY_INCLUDE_TESTS
using Assets.Project.Core.Infrastructure.UI.Configuration;
using NUnit.Framework;

namespace Assets.Project.Tests.EditMode
{
    /// <summary>
    /// Verifies that UI repository keys are derived from the component type's full name.
    /// This invariant must hold: the runtime key (UIAssetTypeKey.Get) must match the
    /// _id/Key strings serialized into the UI prefab repositories.
    /// </summary>
    public sealed class UIAssetTypeKeyTests
    {
        private sealed class SampleView { }

        [Test]
        public void Get_ReturnsTypeFullName()
        {
            string key = UIAssetTypeKey.Get(typeof(SampleView));

            Assert.AreEqual(typeof(SampleView).FullName, key);
        }

        [Test]
        public void GetGeneric_MatchesNonGeneric()
        {
            Assert.AreEqual(UIAssetTypeKey.Get(typeof(SampleView)), UIAssetTypeKey.Get<SampleView>());
        }
    }
}
#endif
