# Headless tools (no Unity needed)

Builds the game's `Core` logic as a plain .NET 8 library (with tiny `UnityEngine` stand-ins in `Stubs/`), so the rules can be tested and simulated anywhere `dotnet` is available.

```
# EditMode tests (the same NUnit files Unity runs, Assets/Scripts/Tests/EditMode)
cd Tools/Headless/Tests && dotnet test

# Balance simulator: a bot plays full Classic runs against the real rules
cd Tools/Headless/RunSim
dotnet run -- --runs 1000 --policy greedy          # greedy | random | both
dotnet run -- --runs 500 --seed 42 --csv runs.csv  # per-run CSV for further analysis
```

The simulator reports win rate, the round runs end in (with that round's roster), defeat causes (out of seeds vs blocked board),
the per-round clear rate and which fertilizers correlate with deeper runs. The bot is intentionally simple (greedy placement,
buys fertilizers first, no fertilizer ordering, no reading of enemy abilities): treat the numbers as a *relative* signal between
balance changes, not as how a good player would fare.
