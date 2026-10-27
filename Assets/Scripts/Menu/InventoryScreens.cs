using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Jogar > Inventário (what Freny takes into the next stage) and Jogar > Armeiro (the shop,
// paid with the saved coins). Same upgrades and prices as the arena shop.
public static class InventoryScreens {

    private const float ItemRow = 38f;

    private static readonly FrenyUpgrade[] Equipment = {
        FrenyUpgrade.GreatSword, FrenyUpgrade.Shotgun, FrenyUpgrade.Grenade, FrenyUpgrade.DoubleJump,
    };

    private static readonly FrenyUpgrade[] Attributes = {
        FrenyUpgrade.MaxHealth, FrenyUpgrade.MoveSpeed, FrenyUpgrade.AttackSpeed,
    };

    private static readonly FrenyUpgrade[] Brain = {
        FrenyUpgrade.Reaction, FrenyUpgrade.DashDodge, FrenyUpgrade.AutoPotion, FrenyUpgrade.SmartTarget,
    };

    private static readonly string[] EquipOptions = { "Guardado", "Equipado" };

    // ---- Inventário ----

    public static MenuPage Inventory(MenuStack stack) {
        MenuPage page = new MenuPage(stack.transform, "Inventário", 640f, new Vector2(0.5f, 0.5f), 6f);
        page.Label("O que o Freny leva para a próxima fase");

        foreach (FrenyUpgrade upgrade in Equipment) {
            FrenyUpgrade item = upgrade;
            if (FrenyProgress.GetLevel(item) > 0) {
                page.Choice(ItemName(item), EquipOptions, FrenyProgress.IsEquipped(item) ? 1 : 0,
                    i => FrenyProgress.SetEquipped(item, i == 1));
            } else {
                page.Button(ItemName(item) + ": no Armeiro", null, false, ItemRow);
            }
        }
        if (Loadout.Owns(Loadout.Helmet)) {
            page.Choice(Loadout.DisplayName(Loadout.Helmet), EquipOptions, Loadout.IsEquipped(Loadout.Helmet) ? 1 : 0,
                i => Loadout.SetEquipped(Loadout.Helmet, i == 1));
        } else {
            page.Button(Loadout.DisplayName(Loadout.Helmet) + ": no Armeiro", null, false, ItemRow);
        }

        page.Space(4f);
        page.Label(AttributesText(), MenuKit.SmallTextSize);
        page.Label("O que cai dos inimigos (espadão, capacete, munição) vale só na fase em que caiu.");
        page.Space(4f);
        page.Button("Armeiro", () => stack.Push(Armory(stack)));
        page.Button("Voltar", stack.Back);
        return page;
    }

    private static string AttributesText() {
        List<string> parts = new List<string>();
        if (FrenyProgress.BonusMaxHealth > 0) {
            parts.Add("Vida +" + FrenyProgress.BonusMaxHealth);
        }
        if (FrenyProgress.GetLevel(FrenyUpgrade.MoveSpeed) > 0) {
            parts.Add("Velocidade +" + Mathf.RoundToInt((FrenyProgress.SpeedMultiplier - 1f) * 100f) + "%");
        }
        if (FrenyProgress.GetLevel(FrenyUpgrade.AttackSpeed) > 0) {
            parts.Add("Recarga -" + Mathf.RoundToInt((1f - FrenyProgress.AttackCooldownMultiplier) * 100f) + "%");
        }
        return "Atributos: " + (parts.Count > 0 ? string.Join(" · ", parts) : "nenhum ainda");
    }

    // ---- Armeiro ----

    public static MenuPage Armory(MenuStack stack) {
        MenuPage page = new MenuPage(stack.transform, "Armeiro", 640f, new Vector2(0.5f, 0.5f), 6f);
        Text coins = page.Label("", MenuKit.TextSize);
        Text message = null;
        List<Action> refresh = new List<Action>();
        Action refreshAll = () => {
            coins.text = "Moedas: " + FrenyProgress.Coins;
            foreach (Action action in refresh) {
                action();
            }
        };

        foreach (FrenyUpgrade upgrade in Equipment) {
            UpgradeRow(page, upgrade, refresh, refreshAll, () => message);
        }
        foreach (FrenyUpgrade upgrade in Attributes) {
            UpgradeRow(page, upgrade, refresh, refreshAll, () => message);
        }

        string helmet = Loadout.Helmet;
        Text helmetPrice = ItemButton(page, Loadout.DisplayName(helmet), () => {
            if (Loadout.TryBuy(helmet)) {
                message.text = Loadout.DisplayName(helmet) + " comprado: começa cada fase com armadura cheia.";
                refreshAll();
            } else {
                Refuse(message, Loadout.Owns(helmet) ? "Você já tem o capacete." : "Moedas insuficientes.");
            }
        });
        refresh.Add(() => helmetPrice.text = Loadout.Owns(helmet) ? "comprado" : Loadout.Cost(helmet).ToString());

        page.Space(4f);
        Button brain = page.Button("", () => stack.Push(BrainPage(stack)), true, ItemRow);
        refresh.Add(() => brain.GetComponentInChildren<Text>().text = "Piloto automático (Arena) · " + PointsText());
        message = page.Label("Escolha um item para comprar.");
        page.Button("Voltar", stack.Back);

        refreshAll();
        return page;
    }

    // the autopilot's brain, paid with the points from levelling up
    private static MenuPage BrainPage(MenuStack stack) {
        MenuPage page = new MenuPage(stack.transform, "Piloto automático", 640f, new Vector2(0.5f, 0.5f), 6f);
        Text points = page.Label("", MenuKit.TextSize);
        Text message = null;
        List<Action> refresh = new List<Action>();
        Action refreshAll = () => {
            points.text = "Nível " + FrenyProgress.Level + " · " + PointsText();
            foreach (Action action in refresh) {
                action();
            }
        };
        foreach (FrenyUpgrade upgrade in Brain) {
            UpgradeRow(page, upgrade, refresh, refreshAll, () => message);
        }
        page.Space(4f);
        message = page.Label("Cada nível ganho na Arena vale 1 ponto.");
        page.Button("Voltar", stack.Back);
        refreshAll();
        return page;
    }

    private static string PointsText() {
        int points = FrenyProgress.BrainPoints;
        return points == 1 ? "1 ponto" : points + " pontos";
    }

    private static void UpgradeRow(MenuPage page, FrenyUpgrade upgrade, List<Action> refresh, Action refreshAll, Func<Text> message) {
        Text name = null;
        Text price = ItemButton(page, "", () => {
            if (FrenyProgress.TryBuy(upgrade, null)) {
                message().text = FrenyProgress.DisplayName(upgrade) + " comprado.";
                refreshAll();
            } else {
                Refuse(message(), FrenyProgress.IsMaxed(upgrade) ? "Esse já está no máximo."
                    : FrenyProgress.IsBrain(upgrade) ? "Sem pontos: suba de nível na Arena." : "Moedas insuficientes.");
            }
        }, out name);
        refresh.Add(() => {
            int level = FrenyProgress.GetLevel(upgrade);
            int max = FrenyProgress.MaxLevel(upgrade);
            name.text = FrenyProgress.DisplayName(upgrade) + (max > 1 ? " (" + level + "/" + max + ")" : "");
            price.text = FrenyProgress.IsMaxed(upgrade) ? (max > 1 ? "máximo" : "comprado")
                : FrenyProgress.IsBrain(upgrade) ? "1 ponto" : FrenyProgress.Cost(upgrade).ToString();
        });
    }

    private static void Refuse(Text message, string why) {
        Sfx.Ui("ui_error");
        message.text = why;
    }

    private static Text ItemButton(MenuPage page, string label, Action onClick) {
        return ItemButton(page, label, onClick, out _);
    }

    // a shop row: the item's name on the left, its price on the right
    private static Text ItemButton(MenuPage page, string label, Action onClick, out Text name) {
        Button button = page.Button(label, onClick, true, ItemRow);
        name = button.GetComponentInChildren<Text>();
        name.alignment = TextAnchor.MiddleLeft;
        name.fontSize = MenuKit.SmallTextSize;
        Text price = MenuKit.MakeText(button.transform, "", MenuKit.SmallTextSize, MenuKit.TitleColor, TextAnchor.MiddleRight);
        MenuKit.Stretch(price.rectTransform);
        price.rectTransform.offsetMin = new Vector2(10f, 0f);
        price.rectTransform.offsetMax = new Vector2(-12f, 0f);
        return price;
    }

    private static string ItemName(FrenyUpgrade upgrade) {
        switch (upgrade) {
            case FrenyUpgrade.GreatSword: return "Espadão";
            case FrenyUpgrade.Shotgun: return "Escopeta";
            case FrenyUpgrade.Grenade: return "Granadas";
            case FrenyUpgrade.DoubleJump: return "Pulo duplo";
            default: return FrenyProgress.DisplayName(upgrade);
        }
    }
}
