using NUnit.Framework;
using Assets.Project.Core.Infrastructure.UI.Enums;

namespace Assets.Project.Tests.EditMode
{
    /// <summary>
    /// Smoke test asserting that the Unity Test Runner is wired up and that project runtime types
    /// are visible to the test assembly through its asmdef reference.
    /// </summary>
    public sealed class TestInfrastructureSmokeTest
    {
        /// <summary>Confirms the test runner can execute a trivial assertion.</summary>
        [Test]
        public void Sanity_BasicArithmetic()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
        }

        /// <summary>Confirms project runtime types are visible to EditMode tests via the asmdef reference.</summary>
        [Test]
        public void RuntimeAssembly_IsAccessibleFromEditModeTests()
        {
            EUIPanelMode mode = EUIPanelMode.Overlay;
            Assert.That(mode, Is.EqualTo(EUIPanelMode.Overlay));
        }
    }
}
