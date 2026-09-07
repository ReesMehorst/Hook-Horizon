using NUnit.Framework;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Tests
{
    public class QteMinigameTests
    {
        private static QteConfig EasyConfig() => new QteConfig(
            barSize: 0.9f, liftAcceleration: 2f, gravity: 1.5f, maxBarSpeed: 1.3f,
            fishSpeed: 0.1f, fillRate: 1f, drainRate: 0f, startProgress: 0.3f);

        private static QteConfig HardConfig() => new QteConfig(
            barSize: 0.1f, liftAcceleration: 2f, gravity: 1.5f, maxBarSpeed: 1.3f,
            fishSpeed: 0.1f, fillRate: 0f, drainRate: 1f, startProgress: 0.3f);

        [Test]
        public void HoldingWithFishInZone_FillsToCaught()
        {
            // Fish parked near centre, huge catch zone: holding should always fill.
            var qte = new QteMinigame(EasyConfig(), new ConstantRng(0.5f));

            QteStatus status = QteStatus.Running;
            for (int i = 0; i < 200 && status == QteStatus.Running; i++)
                status = qte.Tick(0.05f, reelHeld: true);

            Assert.AreEqual(QteStatus.Caught, status);
            Assert.AreEqual(1f, qte.Progress, 0.0001f);
        }

        [Test]
        public void FishOutsideZone_DrainsToEscaped()
        {
            // Tiny zone at the bottom, fish at centre, no reeling: progress must drain out.
            var qte = new QteMinigame(HardConfig(), new ConstantRng(0.5f));

            QteStatus status = QteStatus.Running;
            for (int i = 0; i < 200 && status == QteStatus.Running; i++)
                status = qte.Tick(0.05f, reelHeld: false);

            Assert.AreEqual(QteStatus.Escaped, status);
            Assert.AreEqual(0f, qte.Progress, 0.0001f);
        }

        [Test]
        public void FinishedGame_IgnoresFurtherTicks()
        {
            var qte = new QteMinigame(EasyConfig(), new ConstantRng(0.5f));
            while (qte.Tick(0.05f, true) == QteStatus.Running) { }

            Assert.AreEqual(QteStatus.Caught, qte.Status);
            Assert.AreEqual(QteStatus.Caught, qte.Tick(0.05f, false), "A resolved QTE must not change state.");
        }

        [Test]
        public void BarPosition_StaysWithinTrack()
        {
            var qte = new QteMinigame(EasyConfig(), new ConstantRng(0.5f));
            for (int i = 0; i < 50; i++)
            {
                qte.Tick(0.05f, reelHeld: true);
                Assert.GreaterOrEqual(qte.BarPosition, 0f);
                Assert.LessOrEqual(qte.BarPosition, 1f - qte.Config.BarSize + 0.0001f);
            }
        }
    }
}
