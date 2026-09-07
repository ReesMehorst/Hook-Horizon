using System.Collections.Generic;
using NUnit.Framework;
using HookHorizon.Domain.Progression;

namespace HookHorizon.Tests
{
    public class QuestSystemTests
    {
        private static QuestSystem NewSystem() => new QuestSystem(SampleQuests.Build());

        [Test]
        public void FirstQuest_CompletesAfterThreeCatches_AndFiresReward()
        {
            var sys = NewSystem();
            Quest completed = null;
            sys.QuestCompleted += q => completed = q;

            sys.RecordCatch("cod");
            sys.RecordCatch("cod");
            Assert.IsNull(completed, "not done at 2/3");
            sys.RecordCatch("cod");

            Assert.IsNotNull(completed);
            Assert.AreEqual("welcome_ashore", completed.Id);
            Assert.AreEqual(50, completed.Reward.Coins);
            Assert.IsTrue(sys.Get("welcome_ashore").Completed);
        }

        [Test]
        public void SecondQuest_IsLockedUntilPrerequisiteDone()
        {
            var sys = NewSystem();

            // Before welcome_ashore, distinct catches must NOT advance tide_survey.
            sys.RecordCatch("cod");
            sys.RecordCatch("perch");
            sys.RecordCatch("pike");
            Assert.AreEqual(0, sys.Get("tide_survey").Objective.Progress);
            Assert.IsFalse(sys.Get("tide_survey").Completed);

            // welcome_ashore just completed on the 3rd catch above; now tide_survey is active.
            Assert.IsTrue(sys.Get("welcome_ashore").Completed);
            var active = new List<string>();
            foreach (Quest q in sys.Active) active.Add(q.Id);
            CollectionAssert.Contains(active, "tide_survey");
        }

        [Test]
        public void DistinctSpecies_CountsUnique_UnlocksBiomeOnComplete()
        {
            var sys = NewSystem();
            string unlocked = null;
            sys.BiomeUnlocked += id => unlocked = id;

            // Finish welcome_ashore first.
            sys.RecordCatch("cod");
            sys.RecordCatch("cod");
            sys.RecordCatch("cod");

            Assert.IsFalse(sys.IsBiomeUnlocked("sunken_ruins"), "gated before the quest");

            sys.RecordCatch("tuna");
            sys.RecordCatch("tuna");   // duplicate: no progress
            Assert.AreEqual(1, sys.Get("tide_survey").Objective.Progress);
            sys.RecordCatch("bass");
            sys.RecordCatch("perch");

            Assert.IsTrue(sys.Get("tide_survey").Completed);
            Assert.AreEqual("sunken_ruins", unlocked);
            Assert.IsTrue(sys.IsBiomeUnlocked("sunken_ruins"));
        }

        [Test]
        public void UnknownOrNullBiome_IsTreatedUnlocked()
        {
            var sys = NewSystem();
            Assert.IsTrue(sys.IsBiomeUnlocked(null));
            Assert.IsTrue(sys.IsBiomeUnlocked("lake"));     // not a gated biome
            Assert.IsFalse(sys.IsBiomeUnlocked("sunken_ruins")); // gated, still locked
        }

        [Test]
        public void ObjectiveProgressed_FiresOnAdvanceOnly()
        {
            var sys = NewSystem();
            int events = 0;
            sys.ObjectiveProgressed += _ => events++;

            sys.RecordCatch("cod"); // 1
            sys.RecordCatch("cod"); // 2
            sys.RecordCatch("cod"); // 3 (also completes)
            // welcome_ashore progressed 3 times.
            Assert.AreEqual(3, events);
        }
    }
}
