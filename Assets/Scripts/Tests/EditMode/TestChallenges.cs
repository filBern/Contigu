using Contigu.Core;

namespace Contigu.Tests
{
    /// <summary>Challenge variants only tests need.</summary>
    public static class TestChallenges
    {
        /// <summary>
        /// Winter's small starting bag, but in the old quota-only form (no enemies, a score target per round, the original budgets). A lot of
        /// tests are about the quota path or want a round with no enemy in the way; every real challenge now fights enemies.
        /// </summary>
        public static readonly ChallengeDefinition QuotaOnlyMarathon = BuildQuotaOnlyMarathon();

        private static ChallengeDefinition BuildQuotaOnlyMarathon()
        {
            var challenge = ChallengeCatalog.Marathon.Clone();
            challenge.UsesEncounters = false;
            challenge.Quotas = new[] { 250, 425, 725, 1225, 2075, 3525, 6000, 10200, 17500, 32000, 58000, 105000, 190000, 340000, 600000 };
            challenge.PieceBudgets = new[] { 20, 20, 22, 22, 24, 24, 24, 18, 24, 24, 24, 26, 26, 28, 30 };
            challenge.PieceBudgetBonus = 0;
            challenge.EnemyHpMultiplier = 1f;
            return challenge;
        }
    }
}
