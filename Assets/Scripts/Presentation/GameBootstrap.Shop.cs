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
            if (pending.Id == UpgradeId.SproutPack)
            {
                ShowPackOfUpgrades(revealedUpgrade);
            }
            else if (pending.Id == UpgradeId.FertilizerPack)
            {
                ShowPackOfModifiers(revealedUpgrade);
            }
            else if (pending.Id == UpgradeId.SeedPack)
            {
                ShowSeedPack(ShelfStage.ForWorkshop(pending));
            }
            else if (pending.Id == UpgradeId.MasteryPack)
            {
                ShowMasteries(ShelfStage.ForWorkshop(pending));
            }
            else if (pending.Id == UpgradeId.Compost)
            {
                ShowCompost(pending);
            }
            else if (pending.Pool == UpgradePool.Grid)
            {
                ShowTileChoiceInShop(pending, revealedUpgrade != null && revealedUpgrade.Pool == UpgradePool.Workshop ? revealedUpgrade.Id : (UpgradeId?)null);
            }
            else if (pending.Id == UpgradeId.RandomPiece)
            {
                ShowSeedPack(ShelfStage.ForUpgrade(pending, null));
            }
            else if (pending.Id == UpgradeId.PieceMastery || pending.Id == UpgradeId.ColorMastery)
            {
                ShowMasteries(ShelfStage.ForUpgrade(pending, null));
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

        /// <summary>The seed choice replaces the shop's shelves on the same screen (same backdrop, same tablets) instead of opening a separate overlay.</summary>
        private void ShowTileChoiceInShop(UpgradeDefinition upgrade, UpgradeId? source = null)
        {
            _shopView.SetSelectionMode(true);
            _tileChoiceView.SetShelfLayout(_shopView.GetShelfLayout());
            _tileChoiceView.Show(_run, _run.Deck, _run.PendingUpgradeTileCandidates, EconomyConstants.ShopTileChoiceCount, upgrade, source);
        }

        /// <summary>Gives the shelf choice the shop's own shelf positions and puts the shop's shelves away while it is shown.</summary>
        private void ShowShelfChoice(ChoiceSource source, string title, List<ChoiceEntry> entries, bool needsConfirm, System.Action<int> onChosen)
        {
            _shopView.SetSelectionMode(true);
            _shelfChoiceView.SetShelfLayout(_shopView.GetShelfLayout());
            _shelfChoiceView.Show(source, title, entries, needsConfirm, onChosen);
        }

        private static void BuildSeedVisual(RectTransform host, PieceShape shape, PieceColor color, PieceTrait? trait, TooltipView tooltip)
        {
            ShapePreviewFactory.Build(host, shape, color, trait, tooltip, host.parent.gameObject);
        }

        private void ShowPackOfUpgrades(UpgradeDefinition pack)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var upgrade in _run.PendingPackUpgrades)
            {
                var sprite = UpgradeVisualDefaults.GetGlyph(upgrade.Id);
                entries.Add(new ChoiceEntry
                {
                    BuildVisual = (host, size) => BuildSpriteVisual(host, sprite),
                    Name = upgrade.Name,
                    Body = DescriptionTextFormatter.Colorize(upgrade.Description, 14),
                    Subtitle = Loc.T("choice.sproutTag", UpgradeVisualDefaults.GetRarityLabel(upgrade.Rarity)),
                    FillCell = true
                });
            }
            ShowShelfChoice(ShelfStage.ForWorkshop(pack), Loc.T("choice.sproutPackHint"), entries, false, OnPackOptionChosen);
        }

        private void ShowPackOfModifiers(UpgradeDefinition pack)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var id in _run.PendingPackModifiers)
            {
                var def = ModifierCatalog.Get(id);
                var captured = def;
                entries.Add(new ChoiceEntry
                {
                    BuildVisual = (host, size) =>
                    {
                        var badge = ModifierBadgeFactory.Create(host, captured, size, _tooltipView, showBackground: false, attachTooltip: false);
                        badge.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                        badge.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                        badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                        badge.rectTransform.anchoredPosition = Vector2.zero;
                    },
                    Name = def.Name,
                    Body = DescriptionTextFormatter.Colorize(def.Description, 14),
                    Subtitle = Loc.T("choice.fertilizerTag", ModifierVisualDefaults.GetCategoryLabel(def.Category)),
                    FillCell = true
                });
            }
            ShowShelfChoice(ShelfStage.ForWorkshop(pack), Loc.T("choice.fertilizerPackHint"), entries, false, OnPackOptionChosen);
        }

        private static void BuildSpriteVisual(RectTransform host, Sprite sprite)
        {
            var image = UIFactory.CreatePanel(host, "Icon", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            UIFactory.StretchFull(image.rectTransform);
        }

        private void ShowSeedPack(ChoiceSource source)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var token in _run.PendingUpgradePieceCandidates)
            {
                var t = token;
                entries.Add(new ChoiceEntry { BuildVisual = (host, size) => BuildSeedVisual(host, PieceShapeCatalog.Get(t.Shape), t.Color, t.Trait, _tooltipView) });
            }
            ShowShelfChoice(source, Loc.T("choice.pickSeed"), entries, true, OnPieceChoiceConfirmed);
        }

        private void ShowMasteries(ChoiceSource source)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var option in _run.PendingMasteryOptions)
            {
                var o = option;
                string name;
                int levelNow;
                if (o.Shape.HasValue)
                {
                    name = VisualDefaults.GetShapeName(o.Shape.Value);
                    levelNow = _run.GetShapeMasteryLevel(o.Shape.Value);
                }
                else
                {
                    name = VisualDefaults.GetColorName(o.Color.Value);
                    levelNow = _run.GetColorMasteryLevel(o.Color.Value);
                }
                entries.Add(new ChoiceEntry
                {
                    BuildVisual = (host, size) =>
                    {
                        if (o.Shape.HasValue)
                        {
                            ShapePreviewFactory.BuildMono(host, PieceShapeCatalog.Get(o.Shape.Value), new Color(0.55f, 0.55f, 0.55f));
                        }
                        else
                        {
                            ShapePreviewFactory.Build(host, PieceShapeCatalog.Get(ShapeId.Single), o.Color.Value, null, null, null);
                        }
                    },
                    Name = name,
                    Body = Loc.T("choice.masteryBody", levelNow, levelNow + o.Gain, o.Gain, levelNow + o.Gain - 1),
                    Subtitle = Loc.T("choice.masteryTag")
                });
            }
            ShowShelfChoice(source, Loc.T("choice.pickMastery"), entries, false, OnMasteryOptionChosen);
        }

        private void ShowCompost(UpgradeDefinition pending)
        {
            var types = new List<(ShapeId Shape, PieceColor Color)>(_run.PendingUpgradeTypeCandidates);
            var entries = new List<ChoiceEntry>();
            foreach (var type in types)
            {
                var t = type;
                entries.Add(new ChoiceEntry { BuildVisual = (host, size) => BuildSeedVisual(host, PieceShapeCatalog.Get(t.Shape), t.Color, null, _tooltipView) });
            }
            ShowShelfChoice(ShelfStage.ForWorkshop(pending), Loc.T("choice.pickCompost"), entries, true,
                index => OnSubChoiceConfirmed(new UpgradeSubChoice(types[index].Shape, types[index].Color)));
        }

        private void OnPackOptionChosen(int index)
        {
            var packId = _run.PendingUpgrade != null ? _run.PendingUpgrade.Id : UpgradeId.SproutPack;
            if (packId == UpgradeId.SproutPack)
            {
                if (_run.ResolvePackUpgradeChoice(index))
                {
                    var chosen = _run.PendingUpgrade;
                    ShowTileChoiceInShop(chosen, UpgradeId.SproutPack);
                }
                return;
            }
            _run.ResolvePackModifierChoice(index);
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        // ------------------------------------------------------------ abandoning a purchase (Esc / the "Cancel purchase" button)

        private float _abandonArmedAt = -10f;

        /// <summary>
        /// Gives up the choice in progress (no refund). The first request only arms it (a purchase is a point of no return), the second one
        /// within a few seconds confirms.
        /// </summary>
        private void RequestAbandonPurchase()
        {
            if (_run == null || _run.State != RunState.AwaitingShop || _run.PendingUpgrade == null)
            {
                return;
            }
            if (Time.unscaledTime - _abandonArmedAt > 4f)
            {
                _abandonArmedAt = Time.unscaledTime;
                _abandonLabel.text = Loc.T("choice.abandonConfirm");
                return;
            }
            _abandonArmedAt = -10f;
            _run.AbandonPending();
            _shelfChoiceView.Hide();
            _tileChoiceView.Hide();
            _modifierUpgradeChoiceView.Hide();
            _draftView.Hide();
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        /// <summary>Shows the "Cancel purchase" button above every overlay while a choice is pending in the shop.</summary>
        private void UpdateAbandonButton()
        {
            bool show = _run != null && _run.State == RunState.AwaitingShop && _run.PendingUpgrade != null;
            if (_abandonButton.gameObject.activeSelf != show)
            {
                _abandonButton.gameObject.SetActive(show);
                _abandonLabel.text = Loc.T("choice.abandon");
                _abandonArmedAt = -10f;
            }
            if (show)
            {
                if (Time.unscaledTime - _abandonArmedAt > 4f && _abandonLabel.text != Loc.T("choice.abandon"))
                {
                    _abandonLabel.text = Loc.T("choice.abandon");
                }
                var rect = _abandonButton.transform;
                if (rect.GetSiblingIndex() != rect.parent.childCount - 1)
                {
                    rect.SetAsLastSibling();
                }
            }
        }

        private void OnSubChoiceConfirmed(UpgradeSubChoice subChoice)
        {
            _run.ResolveUpgradeSubChoice(subChoice);
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnTileChoiceConfirmed(IReadOnlyList<int> chosenDeckIndices)
        {
            _run.ResolveUpgradeTileChoice(chosenDeckIndices);
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnPieceChoiceConfirmed(int candidateIndex)
        {
            _run.ResolveUpgradePieceChoice(candidateIndex);
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        private void OnMasteryOptionChosen(int optionIndex)
        {
            _run.ResolveMasteryChoice(optionIndex);
            _shopView.SetSelectionMode(false);
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
            if (_run.CurrentWeather != WeatherId.Clear)
            {
                _hudView.PulseWeather();
            }
        }
    }
}
