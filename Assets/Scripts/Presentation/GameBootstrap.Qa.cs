using System.Collections.Generic;
using Contigu.Core;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>The game's side of the QA hook (see <see cref="QaBridge"/>): what to report about the run, and the debug commands a test may issue.</summary>
    public sealed partial class GameBootstrap
    {
        private void RegisterQaBridge()
        {
            QaBridge.GameState = BuildQaGame;
            QaBridge.GameCommand = RunQaCommand;
        }

        private void UnregisterQaBridge()
        {
            QaBridge.GameState = null;
            QaBridge.GameCommand = null;
        }

        private QaGame BuildQaGame()
        {
            if (_run == null)
            {
                return null;
            }
            var run = _run;
            var game = new QaGame
            {
                state = run.State.ToString(),
                challenge = run.Challenge.Name,
                round = run.CurrentRoundNumber,
                roundScore = run.RoundScore,
                quota = run.CurrentQuota,
                total = run.TotalScore,
                pollen = run.Lueur,
                shuffles = run.ShufflesRemaining,
                seedsLeft = run.PiecesRemainingThisRound,
                weather = run.CurrentWeather.ToString(),
                sequence = _isPlayingPlacementSequence,
                pending = run.PendingUpgrade != null ? run.PendingUpgrade.Name : ""
            };

            var hand = new List<QaToken>();
            var moves = new List<QaMove>();
            for (int i = 0; i < run.Deck.Hand.Count; i++)
            {
                var token = run.Deck.Hand[i];
                if (!token.HasValue)
                {
                    hand.Add(new QaToken { shape = "", color = "", traits = new string[0] });
                    continue;
                }
                var traits = new List<string>();
                foreach (var trait in token.Value.Traits) { traits.Add(trait.Kind.ToString()); }
                bool locked = run.IsHandSlotLocked(i);
                hand.Add(new QaToken { shape = token.Value.Shape.ToString(), color = token.Value.Color.ToString(), traits = traits.ToArray(), rotation = (int)run.Deck.HandRotations[i], locked = locked });
                if (locked || run.State != RunState.InProgress || moves.Count > 60)
                {
                    continue;
                }
                var shape = PieceShapeCatalog.GetRotated(token.Value.Shape, run.Deck.HandRotations[i]);
                for (int x = 0; x < GridManager.Size; x++)
                {
                    for (int y = 0; y < GridManager.Size; y++)
                    {
                        if (run.Grid.CanPlace(shape, x, y) && moves.Count <= 60)
                        {
                            moves.Add(new QaMove { slot = i, x = x, y = y });
                        }
                    }
                }
            }
            game.hand = hand.ToArray();
            game.moves = moves.ToArray();

            var enemies = new List<QaEnemy>();
            if (run.HasActiveEncounter)
            {
                foreach (var enemy in run.CurrentEncounter)
                {
                    enemies.Add(new QaEnemy { id = enemy.Definition.Id.ToString(), hp = enemy.CurrentHp, max = enemy.CurrentMaxHp, dead = enemy.IsDead });
                }
            }
            game.enemies = enemies.ToArray();

            var fertilizers = new List<string>();
            foreach (var id in run.ActiveModifiers) { fertilizers.Add(ModifierCatalog.Get(id).Name); }
            game.fertilizers = fertilizers.ToArray();

            if (run.State == RunState.AwaitingShop)
            {
                game.blister = QaSlots(run.ShopBlisterSlots, i => run.GetBlisterSlotPrice(i));
                game.workshop = QaSlots(run.ShopUpgradeSlots, i => run.GetUpgradeSlotPrice(i));
            }

            var rows = new string[GridManager.Size];
            var cells = new List<QaPoint>();
            for (int y = 0; y < GridManager.Size; y++)
            {
                var chars = new char[GridManager.Size];
                for (int x = 0; x < GridManager.Size; x++)
                {
                    var cell = run.Grid.GetCell(x, y);
                    char c = '.';
                    if (cell.IsFilled && cell.FilledColor.HasValue)
                    {
                        c = cell.FilledColor.Value.ToString()[0];
                        if (cell.IsPoisoned) { c = char.ToLowerInvariant(c); }
                    }
                    else if (cell.IsFrozen) { c = 'F'; }
                    else if (cell.IsLocked) { c = 'X'; }
                    else if (cell.IsPoisoned) { c = '*'; }
                    chars[x] = c;
                    cells.Add(QaBridge.CenterOf(_gridView.GetCellTransform(x, y)));
                }
                rows[y] = new string(chars);
            }
            game.grid = rows;
            game.cells = cells.ToArray();
            return game;
        }

        private static QaShopSlot[] QaSlots(IReadOnlyList<ShopSlot> slots, System.Func<int, int> price)
        {
            var list = new List<QaShopSlot>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null)
                {
                    list.Add(new QaShopSlot { kind = "empty", name = "" });
                    continue;
                }
                bool isModifier = slot.Kind == ShopSlotKind.Modifier;
                list.Add(new QaShopSlot
                {
                    kind = isModifier ? "fertilizer" : "upgrade",
                    name = isModifier ? ModifierCatalog.Get(slot.ModifierId).Name : slot.HiddenUpgrade.Name,
                    price = price(i),
                    sold = slot.Purchased
                });
            }
            return list.ToArray();
        }

        /// <summary>Debug commands for tests: unlockAll, stars, forceRoundWin, grantPollen (arg: amount). The ones that change the run need the CONTIGU_DEBUG scripting define in the build.</summary>
        private string RunQaCommand(string cmd, string arg)
        {
            switch (cmd)
            {
                case "unlockAll":
                    OnUnlockAllChallengesRequested();
                    return "ok";
                case "stars":
                    _metaStats.Stars += 100;
                    _metaStatsStore.Save(_metaStats);
                    _challengeSelectView.Refresh(_metaStats);
                    return "ok";
#if UNITY_EDITOR || CONTIGU_DEBUG
                case "forceRoundWin":
                {
                    if (_isPlayingPlacementSequence || _run.State != RunState.InProgress)
                    {
                        return "not now";
                    }
                    var state = _run.DebugForceRoundComplete();
                    RefreshAll();
                    HandleStateTransition(state);
                    return "ok";
                }
                case "grantPollen":
                {
                    int amount;
                    _run.DebugGrantLueur(int.TryParse(arg, out amount) ? amount : 100);
                    RefreshAll();
                    if (_run.State == RunState.AwaitingShop)
                    {
                        _shopView.Refresh(_run);
                    }
                    return "ok";
                }
#else
                case "forceRoundWin":
                case "grantPollen":
                    return "build without CONTIGU_DEBUG";
#endif
                default:
                    return "unknown command " + cmd;
            }
        }
    }
}
