<h1>THE RISE OF FRENY</h1>
<body style="display: flex">
  <div>
   <marquee direction="up" height="100" width="200" bgcolor="white">Rise</marquee>
  </div>
  <div>
    <marquee direction="down" height="100" width="200" bgcolor="white">Of</marquee>
  </div>
   <div>
    <marquee direction="up" height="100" width="200" bgcolor="white">Freny</marquee>
  </div>
</body>

Jogo 2D de ação / RPG / plataforma feito na Unity.

## Abrindo o projeto

- Unity **6000.6.7f1**, exatamente essa versão (veja `ProjectSettings/ProjectVersion.txt`).
- Antes de começar, leia o [CONTRIBUTING.md](CONTRIBUTING.md): ele explica o setup do Smart Merge e do Git LFS, o
  fluxo de branches e as regras de `.meta`.
- URP 2D, Cinemachine, Tilemap e 2D Animation vêm pelo Package Manager.
- O fluxo de branches é `feature → dev → master`. O `dev` é o branch mais atualizado.

## Cenas

| Cena | O que é |
| --- | --- |
| `Scenes/JoviScene` | Hub / taverna com o mercador, para onde as portas das fases voltam |
| `Scenes/MaxScene`, `Scenes/MaxLevels/1-1` … `1-9` | Fases do mundo 1 (não existe 1-6) |
| `Scenes/ZE Arena` | Arena com spawners, pontuação e combo (onde o trabalho mais recente estava) |
| `Scenes/UI - *` | Telas de menu, pausa, loading e morte (ainda sem scripts ligados) |
| `Runner/` | Protótipo antigo de endless runner, separado do jogo principal |

## Controles

| Ação | Tecla |
| --- | --- |
| Andar | A / D ou setas |
| Pular (segure para pular mais alto) | Espaço |
| Ataque corpo a corpo | Botão esquerdo do mouse |
| Tiro de revólver | R |
| Dash | Shift + C |
| Falar com o mercador / fechar loja | E / Esc |
| Entrar numa porta | W |
| Reiniciar depois de morrer | Espaço |
| Beber poção comprada | Q |
| Ligar/desligar o autopiloto (Arena) | T |
| Mostrar/esconder melhorias (Arena) | U |

## Arena idle

Na `ZE Arena` um autopiloto (`ArenaAutopilot`) joga por Freny usando as mesmas ações de uma pessoa: o `Player`
lê tudo de `FrenyInput`, que escolhe entre o teclado e o autopiloto. Qualquer tecla assume o controle na hora, e
depois de 3 s sem tocar em nada o autopiloto volta. Matar inimigos dá XP; cada nível dá 1 ponto para o "cérebro"
do autopiloto (reação, esquiva com dash, poção, escolha de alvo), e moedas compram atributos da Freny (vida,
velocidade, recarga), que valem também na campanha. O progresso fica no `PlayerPrefs` (`FrenyProgress`).

Por enquanto a `ArenaBootstrap` adiciona esses componentes ao player quando a `ZE Arena` carrega, e o painel é IMGUI
provisório (`ArenaHud`). Quando a cena for montada no Editor, os componentes colocados à mão são respeitados.

## Checagem de compilação

O GitHub Actions compila os scripts a cada push/PR sem precisar da Unity. Detalhes em [`.ci/CompileCheck`](.ci/CompileCheck/README.md).
