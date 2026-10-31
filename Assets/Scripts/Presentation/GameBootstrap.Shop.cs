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
            _purchaseAnimating = true; // Skip/Esc stay quiet until the choice screen is up

            // Lets the bought card shrink/fade away in place before the row rebuilds without it (see
            // ShopView.PlayBlisterRemoval) — the follow-up dispatch (reveal overlay or plain refresh) only
            // runs once that's done, so it never pops in mid-animation.
            _shopView.PlayBlisterRemoval(index, () =>
            {
                _purchaseAnimating = false;
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
            if (pending.Id == UpgradeId.SproutPack || pending.Id == UpgradeId.Forge)
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
                ShowTypeShelf(pending, _run.PendingUpgradeTypeCandidates, TypeChoiceTitle(pending), null);
            }
        }

        /// <summary>Seed-type choice (Propagate a cutting, Replace, Recolor) on the shop's shelves, like every other choice; Replace and Recolor chain a second shelf screen.</summary>
        private void ShowTypeShelf(UpgradeDefinition def, IReadOnlyList<(ShapeId Shape, PieceColor Color)> candidateTypes, string title, (ShapeId Shape, PieceColor Color)? replacing)
        {
            var candidates = new List<(ShapeId Shape, PieceColor Color)>(candidateTypes);
            var entries = new List<ChoiceEntry>();
            foreach (var candidate in candidates)
            {
                var shape = PieceShapeCatalog.Get(candidate.Shape);
                var color = candidate.Color;
                var traits = RepresentativeTraits(candidate.Shape, color);
                int level = _run.GetColorMasteryLevel(color) + _run.GetShapeMasteryLevel(candidate.Shape) - 1;
                entries.Add(new ChoiceEntry
                {
                    Columns = ShapePreviewFactory.ColumnsOf(shape),
                    Rows = ShapePreviewFactory.RowsOf(shape),
                    BuildVisual = (host, size) => BuildSeedVisual(host, shape, color, traits, _tooltipView),
                    Name = VisualDefaults.GetColorName(color) + " " + VisualDefaults.GetShapeName(candidate.Shape),
                    Body = "Mastery level " + level
                });
            }
            ShowShelfChoice(ShelfStage.ForUpgrade(def, null), title, entries, true, index => OnTypeChosen(def, candidates[index], replacing));
        }

        private static string TypeChoiceTitle(UpgradeDefinition def)
        {
            switch (def.Id)
            {
                case UpgradeId.DuplicatePiece: return "Choose a seed to duplicate";
                case UpgradeId.ReplacePiece: return "Choose the seed to remove";
                case UpgradeId.RecolorPiece: return "Choose a seed to recolor";
                default: return "Choose a seed type";
            }
        }

        /// <summary>The copy the upgrade will really act on (the plainest one, see DeckManager.IndexOfPlainestCopy) is the one shown.</summary>
        private IReadOnlyList<PieceTrait> RepresentativeTraits(ShapeId shape, PieceColor color)
        {
            int idx = _run.Deck.IndexOfPlainestCopy(shape, color);
            return idx >= 0 ? _run.Deck.Deck[idx].Traits : null;
        }

        private void OnTypeChosen(UpgradeDefinition def, (ShapeId Shape, PieceColor Color) chosen, (ShapeId Shape, PieceColor Color)? replacing)
        {
            if (replacing.HasValue)
            {
                OnSubChoiceConfirmed(new UpgradeSubChoice(replacing.Value.Shape, replacing.Value.Color, addShape: chosen.Shape, addColor: chosen.Color));
                return;
            }
            if (def.Id == UpgradeId.RecolorPiece)
            {
                ShowRecolorShelf(def, chosen.Shape, chosen.Color);
                return;
            }
            if (def.Id == UpgradeId.ReplacePiece)
            {
                var next = _run.Upgrades.GetReplacementCandidateTypesFor(_run.Deck, chosen.Shape, chosen.Color);
                ShowTypeShelf(def, next, "Choose the seed to add instead", chosen);
                return;
            }
            OnSubChoiceConfirmed(new UpgradeSubChoice(chosen.Shape, chosen.Color));
        }

        private void ShowRecolorShelf(UpgradeDefinition def, ShapeId shapeId, PieceColor fromColor)
        {
            var shape = PieceShapeCatalog.Get(shapeId);
            var targets = new List<PieceColor>();
            var entries = new List<ChoiceEntry>();
            var baseColors = PieceColorUtility.BaseColors;
            for (int i = 0; i < baseColors.Count; i++)
            {
                var target = baseColors[i];
                if (target == fromColor)
                {
                    continue;
                }
                targets.Add(target);
                entries.Add(new ChoiceEntry
                {
                    Columns = ShapePreviewFactory.ColumnsOf(shape),
                    Rows = ShapePreviewFactory.RowsOf(shape),
                    BuildVisual = (host, size) => BuildSeedVisual(host, shape, target, null, _tooltipView),
                    Name = VisualDefaults.GetColorName(target)
                });
            }
            ShowShelfChoice(ShelfStage.ForUpgrade(def, null), "Choose the target color", entries, true, index => OnSubChoiceConfirmed(new UpgradeSubChoice(shapeId, fromColor, targets[index])));
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

        private static void BuildSeedVisual(RectTransform host, PieceShape shape, PieceColor color, IReadOnlyList<PieceTrait> traits, TooltipView tooltip)
        {
            float tile = ShapePreviewFactory.UniformTile(host.sizeDelta.x);
            ShapePreviewFactory.BuildWithTraits(host, shape, color, traits, tooltip, host.parent.gameObject, tile);
        }

        private void ShowPackOfUpgrades(UpgradeDefinition pack)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var upgrade in _run.PendingPackUpgrades)
            {
                var sprite = UpgradeVisualDefaults.GetGlyph(upgrade.Id);
                string body = TileUpgradePreview.Append(DescriptionTextFormatter.Colorize(upgrade.Description, 14), upgrade.Id);
                entries.Add(new ChoiceEntry
                {
                    BuildVisual = (host, size) => BuildSpriteVisual(host, sprite),
                    Name = upgrade.Name,
                    Body = body,
                    Subtitle = Loc.T("choice.sproutTag", UpgradeVisualDefaults.GetRarityLabel(upgrade.Rarity)),
                    FillCell = true
                });
            }
            ShowShelfChoice(ShelfStage.ForWorkshop(pack), Loc.T("choice.sproutPackHint"), entries, true, OnPackOptionChosen);
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
            ShowShelfChoice(ShelfStage.ForWorkshop(pack), Loc.T("choice.fertilizerPackHint"), entries, true, OnPackOptionChosen);
        }

        private static void BuildSpriteVisual(RectTransform host, Sprite sprite)
        {
            var image = UIFactory.CreatePanel(host, "Icon", Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0.5f - ShopView.UpgradeGlyphShopScale * 0.5f, 0.5f - ShopView.UpgradeGlyphShopScale * 0.5f);
            image.rectTransform.anchorMax = new Vector2(0.5f + ShopView.UpgradeGlyphShopScale * 0.5f, 0.5f + ShopView.UpgradeGlyphShopScale * 0.5f);
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        }

        private void ShowSeedPack(ChoiceSource source)
        {
            var entries = new List<ChoiceEntry>();
            foreach (var token in _run.PendingUpgradePieceCandidates)
            {
                var t = token;
                entries.Add(new ChoiceEntry { Columns = ShapePreviewFactory.ColumnsOf(PieceShapeCatalog.Get(t.Shape)), Rows = ShapePreviewFactory.RowsOf(PieceShapeCatalog.Get(t.Shape)), BuildVisual = (host, size) => BuildSeedVisual(host, PieceShapeCatalog.Get(t.Shape), t.Color, t.Traits, _tooltipView) });
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
                var masteryShape = PieceShapeCatalog.Get(o.Shape ?? ShapeId.Single);
                entries.Add(new ChoiceEntry
                {
                    // Like seeds, mastery pieces stand on the shelf with the shop's common tile size.
                    Columns = ShapePreviewFactory.ColumnsOf(masteryShape),
                    Rows = ShapePreviewFactory.RowsOf(masteryShape),
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
            ShowShelfChoice(source, Loc.T("choice.pickMastery"), entries, true, OnMasteryOptionChosen);
        }

        private void ShowCompost(UpgradeDefinition pending)
        {
            var deckIndices = new List<int>(_run.PendingUpgradeTileCandidates);
            var entries = new List<ChoiceEntry>();
            foreach (var deckIndex in deckIndices)
            {
                var t = _run.Deck.Deck[deckIndex];
                entries.Add(new ChoiceEntry { Columns = ShapePreviewFactory.ColumnsOf(PieceShapeCatalog.Get(t.Shape)), Rows = ShapePreviewFactory.RowsOf(PieceShapeCatalog.Get(t.Shape)), BuildVisual = (host, size) => BuildSeedVisual(host, PieceShapeCatalog.Get(t.Shape), t.Color, t.Traits, _tooltipView) });
            }
            ShowShelfChoice(ShelfStage.ForWorkshop(pending), Loc.T("choice.pickCompost"), entries, true, index =>
            {
                _run.ResolveCompostChoice(deckIndices[index]);
                _shopView.SetSelectionMode(false);
                RefreshAll();
                _shopView.Refresh(_run);
            });
        }

        private void OnPackOptionChosen(int index)
        {
            var packId = _run.PendingUpgrade != null ? _run.PendingUpgrade.Id : UpgradeId.SproutPack;
            if (packId == UpgradeId.SproutPack || packId == UpgradeId.Forge)
            {
                if (_run.ResolvePackUpgradeChoice(index))
                {
                    var chosen = _run.PendingUpgrade;
                    ShowTileChoiceInShop(chosen, packId);
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
        /// Skips the choice in progress: nothing is taken and the Pollen is not refunded, but the purchase counts as made.
        /// </summary>
        /// <summary>True from the click that buys a Blister item until its card has finished leaving the shelf.</summary>
        private bool _purchaseAnimating;

        private void RequestAbandonPurchase()
        {
            if (_run == null || _run.State != RunState.AwaitingShop || _run.PendingUpgrade == null || _purchaseAnimating)
            {
                return;
            }
            // Skipping counts as having taken the purchase: the Pollen is spent and the item's next price rises, one click.
            _abandonArmedAt = -10f;
            _run.AbandonPending();
            _shelfChoiceView.Hide();
            _tileChoiceView.Hide();
            _modifierUpgradeChoiceView.Hide();
            _shopView.SetSelectionMode(false);
            RefreshAll();
            _shopView.Refresh(_run);
        }

        /// <summary>Shows the "Skip" button above every overlay while a choice is pending in the shop.</summary>
        private void UpdateAbandonButton()
        {
            bool show = _run != null && _run.State == RunState.AwaitingShop && _run.PendingUpgrade != null && !_purchaseAnimating;
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
            SaveCheckpoint();
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
