namespace EideeEasyFishing
{
    internal enum AutoRecastContext
    {
        Blocked,
        World,
        FairFishing,
        IceFishing
    }

    internal static class AutoRecastContextPolicy
    {
        // Keep ordinary cutscenes and unrelated minigames blocked. Festival fishing receives a
        // temporary rod and has its own lifecycle, so allow only recognized active contests.
        public static AutoRecastContext Resolve(bool worldReady, bool eventUp, bool hasMinigame,
            bool isFairFishing, bool fairDone, int? fairRemainingMs, bool fairLocation,
            bool temporaryRod, bool iceFishingActive, int iceRemainingMs)
        {
            if (!worldReady) return AutoRecastContext.Blocked;
            if (hasMinigame)
                return isFairFishing && !fairDone && fairRemainingMs > 0 && fairLocation && temporaryRod
                    ? AutoRecastContext.FairFishing : AutoRecastContext.Blocked;
            if (eventUp)
                return iceFishingActive && iceRemainingMs > 0 && temporaryRod
                    ? AutoRecastContext.IceFishing : AutoRecastContext.Blocked;
            return AutoRecastContext.World;
        }
    }
}
