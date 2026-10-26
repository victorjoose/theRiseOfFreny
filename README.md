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

- Unity **2022.3.3f1** (veja `ProjectSettings/ProjectVersion.txt`). Qualquer 2022.3 LTS mais nova deve abrir com upgrade automático.
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

## Checagem de compilação

O GitHub Actions compila os scripts a cada push/PR sem precisar da Unity. Detalhes em [`.ci/CompileCheck`](.ci/CompileCheck/README.md).
