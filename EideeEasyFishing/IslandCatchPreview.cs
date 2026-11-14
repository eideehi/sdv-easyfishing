namespace EideeEasyFishing
{
    internal enum IslandCatchOutcome
    {
        Normal,
        Walnut,
        Trash,
        Unavailable,
        NoOrdinaryCatch,
        HostWalnutDrop
    }

    internal static class IslandCatchPreview
    {
        public static IslandCatchOutcome GetOutcomeWithoutWalnuts(bool isStardropPool)
            => isStardropPool ? IslandCatchOutcome.NoOrdinaryCatch : IslandCatchOutcome.Normal;

        // Mirrors the vanilla island getFish overrides without awarding or requesting any nuts.
        public static IslandCatchOutcome GetOutcome(bool isStardropPool, bool poolWalnutCollected,
            bool multiplayer, bool isHost, int limitedNutsCollected, double seededRoll)
        {
            if (isStardropPool)
            {
                if (poolWalnutCollected) return IslandCatchOutcome.Trash;
                return multiplayer
                    ? (isHost ? IslandCatchOutcome.HostWalnutDrop : IslandCatchOutcome.Unavailable)
                    : IslandCatchOutcome.Walnut;
            }

            if (limitedNutsCollected < 5 && seededRoll < 0.15)
                return multiplayer
                    ? (isHost ? IslandCatchOutcome.HostWalnutDrop : IslandCatchOutcome.Unavailable)
                    : IslandCatchOutcome.Walnut;

            return IslandCatchOutcome.Normal;
        }
    }
}
