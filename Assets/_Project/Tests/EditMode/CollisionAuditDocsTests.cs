using GasSafari.Editor;
using NUnit.Framework;

namespace GasSafari.Tests
{
    // docs/11, checklist "nada se traspasa": T-0, W1, W2 y W3 no tienen colliders de menos de 0.3 m ni props con los que la
    // balsa pueda chocar sin collider. Si falla, corre `Gas Safari/QA/Corregir props sin collider (T-0, W1, W2, W3)`.
    public class CollisionAuditDocsTests
    {
        [Test]
        public void T0W1W2W3_HaveNoThinCollidersAndNoUnprotectedProps()
        {
            var report = CollisionAudit.RunAll();
            Assert.IsEmpty(report, string.Join("\n", report));
        }
    }
}
