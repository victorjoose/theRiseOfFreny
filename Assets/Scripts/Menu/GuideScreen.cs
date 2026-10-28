using UnityEngine;
using UnityEngine.UI;

// The Guia page (main menu and pause menu): what every weapon, item, buff and shop upgrade does
// and where to get it, one topic at a time. The Ajuda page covers the buttons; this covers the
// rules. Numbers that live in code as public constants are read from there; the rest copy
// Player, ItemPickup, ArenaChest and ArenaDirector, so update both when tuning those.
public static class GuideScreen {

    private const float IconBox = 34f;
    private static readonly string[] Topics = { "Armas", "Fúria e defesa", "Itens", "Onde achar", "Armeiro", "Arena" };
    private static int lastTopic;

    public static MenuPage Guide(MenuStack stack) {
        MenuPage page = new MenuPage(stack.transform, "Guia", 900f, new Vector2(0.5f, 0.5f), 4f);

        RectTransform entries = MenuKit.MakeRect("Entries", page.Content);
        VerticalLayoutGroup column = entries.gameObject.AddComponent<VerticalLayoutGroup>();
        column.spacing = 4f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        page.Choice("Assunto", Topics, lastTopic, i => {
            lastTopic = i;
            Fill(entries, i);
        });
        // the picker is created after the entries' container but goes above it
        entries.SetAsLastSibling();
        Fill(entries, lastTopic);

        page.Space(6f);
        page.Button("Voltar", stack.Back);
        return page;
    }

    private static void Fill(RectTransform entries, int topic) {
        for (int i = entries.childCount - 1; i >= 0; i--) {
            Object.Destroy(entries.GetChild(i).gameObject);
        }
        MenuArt art = MenuArt.Get;
        switch (topic) {
            case 0: Weapons(entries, art); break;
            case 1: Defense(entries, art); break;
            case 2: Items(entries, art); break;
            case 3: Drops(entries, art); break;
            case 4: Shop(entries, art); break;
            default: Arena(entries, art); break;
        }
    }

    private static void Weapons(Transform parent, MenuArt art) {
        Entry(parent, art ? art.sword : null, "Espada",
            "Golpe de perto, 1 de dano, a cada 0,5 s. Funciona no ar e agachado.");
        Entry(parent, art ? art.greatSword : null, "Espadão",
            "Corte e alcance 1,8x maiores (mesmo dano): pega mais inimigos de uma vez. " +
            "Do chão vale 3 golpes; comprado no Armeiro, é para sempre.");
        Entry(parent, art ? art.revolver : null, "Revólver",
            "4 tiros por tambor; vazio, recarrega sozinho em 1,6 s com a reserva de balas " +
            "(começa com 8, máximo 16). Sem reserva, fica sem tiro. Cima atira para cima.");
        Entry(parent, art ? art.shotgun : null, "Escopeta",
            "3 balas em leque, 1 cartucho por tiro. Troque com o botão Escopeta. Vem de baús " +
            "da Arena ou do Armeiro (6 cartuchos por fase). Até 30 cartuchos.");
        Entry(parent, art ? art.grenade : null, "Granada",
            "Segure para carregar a força, solte para lançar em arco. Explode em 1 s ou ao tocar " +
            "um inimigo, acerta todos em volta e quebra baús e vasos. Até 6.");
    }

    private static void Defense(Transform parent, MenuArt art) {
        Entry(parent, art ? art.heart : null, "Vida",
            "4 corações. Golpe comum tira 1; Golem e a bola do Feiticeiro tiram 2. Depois de " +
            "apanhar ela pisca 0,8 s sem levar dano. Queda alta também tira vida.");
        Entry(parent, art ? art.rageHelmet : null, "Fúria",
            "A barra Fúria enche lutando: matar, acertar a espada e apanhar. Parada 2 s, ela " +
            "esvazia. Cheia, dá 20 s de FÚRIA: a espada tira 2 de dano.");
        Entry(parent, art ? art.berserkerHelmet : null, "Armadura (Capacete Berserker)",
            Player.ArmorMax + " corações cinza que levam os golpes antes da vida (queda não). " +
            "Cai de inimigos e vasos; comprado no Armeiro, cada fase começa com ela cheia.");
        Entry(parent, art ? art.armorHeart : null, "Itens que sobram",
            "Com vida, balas ou armadura cheias, o item fica no chão até você precisar. " +
            "Itens duram 25 s e piscam antes de sumir.");
    }

    private static void Items(Transform parent, MenuArt art) {
        Entry(parent, art ? art.potion : null, "Poção",
            "Do chão: cura 1 coração na hora (com vida cheia não pega). Do Mercador " +
            "(150 moedas): fica guardada, beba com o botão Poção.");
        Entry(parent, art ? art.revolver : null, "Balas",
            "+4 na reserva do revólver. Só caem quando a reserva não está cheia.");
        Entry(parent, art ? art.shotgun : null, "Cartuchos",
            "+3 para a escopeta. Só caem se você já tem a escopeta.");
        Entry(parent, art ? art.diamond : null, "Diamante",
            "Vale " + ItemPickup.DiamondValue + " moedas.");
        Entry(parent, art ? art.coin : null, "Moedas",
            "Caem de todo inimigo e ficam salvas. Compram melhorias no Armeiro.");
    }

    private static void Drops(Transform parent, MenuArt art) {
        Entry(parent, art ? art.coin : null, "Inimigos (Arena)",
            "Além da moeda: poção 15%, balas 12%, cartuchos 10%, granada 6%, espadão 4%, " +
            "armadura 4%, diamante 4%. Na campanha só deixam moedas.");
        Entry(parent, art ? art.grenade : null, "Baú (Arena)",
            "Surge a cada 30 s e some em 20 s; quebra com golpe ou tiro. Dá 2 granadas (35%), " +
            "escopeta com 6 cartuchos (35%) ou 10-25 moedas (30%, às vezes com diamante).");
        Entry(parent, art ? art.diamond : null, "Vaso (Arena)",
            "Até 3 por vez, um novo a cada 12 s. Pode ter diamante 12%, poção 18%, balas 10%, " +
            "granada 8% ou armadura 3%.");
    }

    private static void Shop(Transform parent, MenuArt art) {
        Entry(parent, art ? art.jump : null, "Pulo duplo · " + FrenyProgress.Cost(FrenyUpgrade.DoubleJump),
            "Um segundo pulo no ar.");
        Entry(parent, art ? art.grenade : null, "Granada · " + FrenyProgress.Cost(FrenyUpgrade.Grenade),
            "Começa cada fase com 2 granadas.");
        Entry(parent, art ? art.greatSword : null, "Espadão · " + FrenyProgress.Cost(FrenyUpgrade.GreatSword),
            "A espada fica 1,8x maior para sempre.");
        Entry(parent, art ? art.shotgun : null, "Escopeta · " + FrenyProgress.Cost(FrenyUpgrade.Shotgun),
            "Começa cada fase com a escopeta e 6 cartuchos.");
        Entry(parent, art ? art.berserkerHelmet : null, "Capacete Berserker · " + Loadout.HelmetCost,
            "Começa cada fase com " + Player.ArmorMax + " de armadura.");
        Entry(parent, art ? art.heart : null, "Vida, Velocidade e Recarga",
            "+1 coração, +8% de velocidade, -10% de recarga por nível. Custam 100, +50 a cada " +
            "nível. O Inventário deixa armas compradas em casa.");
    }

    private static void Arena(Transform parent, MenuArt art) {
        Sprite face = art != null && art.frenyFace != null && art.frenyFace.Length > 0 ? art.frenyFace[0] : null;
        Entry(parent, face, "Automático ou Manual",
            "No automático o piloto joga por você e dá para assumir. Só corrida toda manual " +
            "vale para o Ranking.");
        Entry(parent, art ? art.rageHelmet : null, "Dificuldade",
            "Fácil, Médio, Difícil ou Freny: quanto maior, mais inimigos, mais rápidos e mais cedo.");
        Entry(parent, art ? art.run : null, "XP e piloto automático",
            "Matar dá XP. Cada nível vale 1 ponto, que compra melhorias do piloto no Armeiro: " +
            "reação, esquiva com dash, poção sozinho e melhor alvo.");
    }

    // icon | name over what it does
    private static void Entry(Transform parent, Sprite icon, string name, string what) {
        Image row = MenuKit.MakeImage("Entry " + name, parent, new Color(1f, 1f, 1f, 0.05f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image picture = MenuKit.MakeImage("Icon", row.transform, Color.white);
        picture.sprite = MenuArt.Trim(icon);
        picture.preserveAspect = true;
        picture.raycastTarget = false;
        picture.enabled = icon != null;
        MenuKit.Size(picture, IconBox, IconBox);

        Text text = MenuKit.MakeText(row.transform, "", MenuKit.SmallTextSize, MenuKit.DimTextColor, TextAnchor.UpperLeft);
        text.supportRichText = true;
        text.text = "<color=#" + ColorUtility.ToHtmlStringRGB(MenuKit.TitleColor) + ">" + name + "</color>\n" + what;
        MenuKit.Size(text, -1f, -1f).flexibleWidth = 1f;
    }
}
