using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Shop purchases and the upgrade sub-choices that follow them.</summary>
    public sealed partial class GameBootstrap
    {
        private void OnBlisterBuyRequested(int index)
        {
            if (index < 0 || index >= _run.ShopBlisterSlots.Count || _run.ShopBlisterSlots[index] == null)
            {
                return;
            }
            var slot = _run.ShopBlisterSlots[index];
            // A Modifier-kind slot applies outright, no follow-up screen
            // needed. An Upgrade-kind slot needs the same reveal/sub-choice
            // dispatch a Casino purchase does (see
            // HandleUpgradePurchaseResult) — its identity was already
            // visible before buying, but what happens next is unchanged.
            bool isUpgrade = slot.Kind == ShopSlotKind.Upgrade;
            var revealedUpgrade = isUpgrade ? slot.HiddenUpgrade : null;

            if (!_run.BuyBlisterSlot(index))
            {
                return;
            }
            _shopView.PlayPurchasePop();

            // Lets the bought card shrink/fade away in place before the row rebuilds without it (see
            // ShopView.PlayBlisterRemoval) — the follow-up dispatch (reveal overlay or plain refresh) only
            // runs once that's done, so it never pops in mid-animation.
            _shopView.PlayBlisterRemoval(index, () =>
            {
                if (isUpgrade)
                {
                    HandleUpgradePurchaseResult(revealedUpgrade);
                }
                else
                {
                    RefreshAll();
                    _shopView.Refresh(_run);
                }
            });
        }

        private void OnUpgradeBuyRequested(int index)
        {
            if (index < 0 || index >= _run.ShopUpgradeSlots.Count || _run.ShopUpgradeSlots[index] == null)
            {
                return;
            }
            // HiddenUpgrade stays readable on the ShopSlot object itself
            // after purchase (RunManager only flips Purchased, never clears
            // it) — grabbed here so the Joker case (applies immediately,
            // leaves PendingUpgrade null) still has something to reveal.
            var revealedUpgrade = _run.ShopUpgradeSlots[index].HiddenUpgrade;

            if (!_run.BuyUpgradeSlot(index))
            {
                return;
            }
            _shopView.PlayPurchasePop();
            HandleUpgradePurchaseResult(revealedUpgrade);
        }

        /// <summary>
        /// Shared post-purchase reveal/follow-up dispatch for an upgrade
        /// just bought, whichever shop section it came from — the
        /// resolution logic only depends on RunManager.PendingUpgrade/
        /// LastRandomModifierGranted, never on which section triggered it.
        /// </summary>
        private void HandleUpgradePurchaseResult(UpgradeDefinition revealedUpgrade)
        {
            var pending = _run.PendingUpgrade;
            // Deferred until the carousel is dismissed (see
            // OnModifierCarouselDismissed) so the panel doesn't spoil the
            // reveal. Core has already granted it — only the panel's own
            // refresh needs to wait; everything else updates immediately.
            bool willShowModifierCarousel = pending == null && revealedUpgrade.Id == UpgradeId.RandomModifier && _run.LastRandomModifierGranted.HasValue;
            RefreshAll(refreshModifierPanel: !willShowModifierCarousel);
            _shopView.Refresh(_run);

            if (pending == null)
            {
                // A Bank upgrade with no sub-choice (Joker or Random
                // Modifier) — already applied; still shows the reveal card.
                // No reveal at all if the gamble didn't pay off
                // (LastRandomModifierGranted null — already at the cap).
                if (revealedUpgrade.Id == UpgradeId.JokerPiece)
                {
                    _upgradeRevealView.Show(revealedUpgrade, _run.LastJokerShapeAdded, PieceColor.Joker, new PieceTrait(_run.LastJokerCombatKindAdded, 0));
                }
                else if (revealedUpgrade.Id == UpgradeId.PermanentBonusSquare)
                {
                    // Applies instantly with nothing else to show — so say what happened and where.
                    if (_run.LastPermanentBonusPosition.HasValue)
                    {
                        _upgradeRevealView.ShowBoard(revealedUpgrade, _run.LastPermanentBonusPosition.Value);
                    }
                }
                else if (revealedUpgrade.Id == UpgradeId.PieceMastery && _run.LastShapeMasteryGranted.HasValue)
                {
                    var grantedShape = _run.LastShapeMasteryGranted.Value;
                    _shapeCarouselView.Show(grantedShape, _run.GetShapeMasteryLevel(grantedShape));
                }
                else if (revealedUpgrade.Id == UpgradeId.ColorMastery && _run.LastColorMasteryGranted.HasValue)
                {
                    var grantedColor = _run.LastColorMasteryGranted.Value;
                    _colorCarouselView.Show(grantedColor, _run.GetColorMasteryLevel(grantedColor));
                }
                else if (_run.LastRandomModifierGranted.HasValue)
                {
                    _modifierCarouselView.Show(_run.LastRandomModifierGranted.Value, "Random fertilizer");
                }
                return;
            }
            if (pending.Pool == UpgradePool.Grid)
            {
                _tileChoiceView.Show(_run, _run.Deck, _run.PendingUpgradeTileCandidates, EconomyConstants.ShopTileChoiceCount, pending);
            }
            else if (pending.Id == UpgradeId.RandomPiece)
            {
                _pieceChoiceView.Show(_run.PendingUpgradePieceCandidates, pending, _run);
            }
            else if (pending.Id == UpgradeId.PieceMastery || pending.Id == UpgradeId.ColorMastery)
            {
                _masteryChoiceView.Show(_run.PendingMasteryOptions, _run);
            }
            else if (pending.Id == UpgradeId.ModifierUpgrade)
            {
                _modifierUpgradeChoiceView.Show(_run.ActiveModifiers, index => _run.GetModifierLevel(index), pending);
            }
            else
            {
                _draftView.ShowForPendingUpgrade(pending, _run.PendingUpgradeTypeCandidates, _run);
            }
        }

        private void OnSubChoiceConfirmed(UpgradeSubChoice subChoice)
        {
            _run.ResolveUpgradeSubChoice(subChoice);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnTileChoiceConfirmed(IReadOnlyList<int> chosenDeckIndices)
        {
            _run.ResolveUpgradeTileChoice(chosenDeckIndices);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnPieceChoiceConfirmed(int candidateIndex)
        {
            _run.ResolveUpgradePieceChoice(candidateIndex);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnMasteryOptionChosen(int optionIndex)
        {
            _run.ResolveMasteryChoice(optionIndex);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnModifierUpgradeChoiceConfirmed(int slotIndex)
        {
            _run.ResolveModifierUpgradeChoice(slotIndex);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnRerollRequested()
        {
            _run.RerollShop();
            _shopView.Refresh(_run);
            _shopView.PlayRerollFlip();
            _hudView.Refresh(_run);
        }

        private void OnLeaveShopRequested()
        {
            if (!_run.LeaveShop())
            {
                return;
            }
            _shopView.Hide();
            RefreshAll();
            SetStatusText(GetCurrentRoundStatus());
            _roundBannerView.Play(_run);
        }
    }
}
