namespace Contigu.Core
{
    /// <summary>
    /// Selectable pre-run challenges (spec extension, explicit request:
    /// "Meta progression avec différents challenge qui offrent différents
    /// boss et état de depart" — see ChallengeCatalog for what each one
    /// actually varies). Classic is always available; the others are
    /// unlocked with Stars (see MetaStats/MetaStatsRecorder).
    /// </summary>
    public enum ChallengeId
    {
        Classic,
        Marathon,
        Chaos
    }
}
