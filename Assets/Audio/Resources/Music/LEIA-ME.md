# Músicas

Solte aqui os arquivos de música (`.ogg`, `.mp3` ou `.wav`) com estes nomes, e o `MusicPlayer`
toca cada uma no lugar certo, em loop, com o volume de Configurações > Música:

| Arquivo        | Onde toca                                   |
|----------------|---------------------------------------------|
| `menu`         | menu principal                              |
| `arena`        | Arena                                       |
| `masmorra1`    | fases 1-1 a 1-9 (e assim por diante: `masmorra2`...) |
| `campanha`     | qualquer fase sem a música da sua masmorra  |
| `chefe`        | fases de chefe (1-9, 2-9...), se existir    |

Exemplo: `Assets/Audio/Resources/Music/menu.ogg`. Sem arquivo, aquele lugar fica em silêncio.
A troca entre músicas tem um fade curto, e a mesma música continua tocando quando a próxima cena usa o mesmo arquivo.

`arena.ogg` é uma faixa provisória (chiptune em Ré menor, 150 BPM, 51 s em loop), gerada por
`Tools/MusicGen/arena_music.py`. Para trocar, é só substituir o arquivo mantendo o nome.
