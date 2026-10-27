# Trabalhando em equipe no The Rise of Freny

Guia curto para quem vai mexer no projeto junto. Se algo aqui não fizer sentido, pergunte antes de forçar um merge.

## 1. Preparar a máquina (uma vez)

1. Instale a **Unity 6000.6.7f1** pelo Unity Hub. Tem que ser exatamente essa versão: abrir com outra versão
   reescreve dezenas de arquivos (`ProjectSettings`, `.meta`, cenas) e gera conflito para todo mundo. O CI recusa
   PR com outra versão em `ProjectSettings/ProjectVersion.txt`.
2. Instale o **Git for Windows** (já vem com Git LFS). No macOS/Linux, instale o `git-lfs` separado.
3. Clone e rode o script de configuração dentro do clone:

   ```powershell
   git clone https://github.com/victorjoose/theRiseOfFreny.git
   cd theRiseOfFreny
   git checkout dev
   powershell -ExecutionPolicy Bypass -File Tools\git\setup.ps1
   ```

   (macOS/Linux: `bash Tools/git/setup.sh`.) O script liga o **Smart Merge** da Unity
   (`UnityYAMLMerge`), que junta mudanças em cenas e prefabs sozinho, e os hooks do Git LFS. Rode de novo se reinstalar a Unity.

## 2. Fluxo do dia a dia

```
dev ──┬──────────────► (PR) ──► dev
      └── sua-branch ──┘
```

1. Atualize: `git checkout dev` e `git pull`.
2. Crie uma branch curta: `git checkout -b nome/assunto` (ex.: `joao/boss-masmorra2`).
3. Faça commits pequenos. Antes de abrir o PR, traga o `dev` de novo (`git pull origin dev`) e resolva conflitos
   na sua branch.
4. Abra o PR **para `dev`** (nunca para `master`). O GitHub roda a checagem de compilação e de `.meta`.
5. Quando o CI ficar verde e a cena abrir sem erro no Console, faça o merge. Apague a branch depois.

`master` só recebe o `dev` quando for fechar uma versão.

## 3. Regras de `.meta` (a fonte dos conflitos)

Cada arquivo e pasta dentro de `Assets/` tem um `.meta` com um **GUID**. Cenas e prefabs apontam para os assets
por esse GUID. Se o `.meta` some ou é recriado, o GUID muda e as referências quebram ("Missing" no Inspector).

- **Sempre commite o `.meta` junto com o arquivo**, no mesmo commit. Arquivo novo sem `.meta` ou `.meta` sem
  arquivo faz o CI falhar.
- **Renomeie, mova e apague só pela janela Project da Unity**, nunca pelo Explorer. Assim o `.meta` vai junto e
  o GUID é mantido.
- **Não copie arquivos com `.meta`** de outro lugar do projeto: dois `.meta` com o mesmo GUID dão problema. Use
  Ctrl+D na Unity, que gera um GUID novo.
- **Pasta vazia não existe no Git.** Se você criar uma pasta e não colocar nada nela, não commite o `.meta` dela.
- Se aparecer um `.meta` que você não mexeu, quase sempre é a Unity reimportando; confira o diff antes de
  commitar ou descartar.
- Num conflito de `.meta`, mantenha o GUID que já está no `dev` (normalmente "theirs" quando você está trazendo
  o `dev` para a sua branch).

## 4. Cenas e prefabs

O Smart Merge resolve a maioria dos conflitos em `.unity`, `.prefab`, `.asset`, `.anim` e `.controller`, mas não
faz milagre:

- **Combine quem mexe em qual cena.** Duas pessoas editando a mesma cena ao mesmo tempo é o caso que mais dá dor.
  Avise no grupo antes de abrir uma cena grande (`ZE Arena`, fases da campanha).
- Prefira **prefabs**: mudar um inimigo no prefab não toca na cena.
- Salve a cena só quando quiser mesmo mudá-la. Abrir e dar Ctrl+S "por garantia" gera diff à toa.
- Se o merge de uma cena der errado, não tente consertar o YAML na mão: escolha uma versão
  (`git checkout --ours` ou `--theirs` no arquivo) e refaça a sua mudança na Unity.

## 5. Arte, áudio e Git LFS

- **PNG, OGG, MP3 e fontes ficam no Git normal.** São pequenos (pixel art, áudio curto) e o repo inteiro tem
  poucas dezenas de MB. O `.gitattributes` marca esses arquivos como binários: o Git não tenta mesclar nem mexer
  no fim de linha deles.
- **Arquivos-fonte e grandes vão para o Git LFS**: `.psd`, `.aseprite`, `.kra`, `.blend`, `.fbx`, `.wav`,
  vídeos etc. Você só faz `git add` como sempre; o `.gitattributes` cuida do resto.

Binário não tem merge: se duas pessoas editarem o mesmo arquivo, uma das edições se perde. Então:

- **PNG/OGG:** avise no grupo antes de editar um sprite ou som que o outro pode estar mexendo. Num conflito,
  escolha uma versão inteira (`git checkout --ours` ou `--theirs` no arquivo) e refaça a outra edição.
- **Arquivos do LFS** chegam como **só leitura**. Antes de editar, trave; ninguém consegue dar push num arquivo
  travado por outra pessoa:

  ```bash
  git lfs lock "Art/freny.aseprite"     # trava para você
  git lfs locks                          # vê o que está travado e por quem
  git lfs unlock "Art/freny.aseprite"   # depois do merge do seu PR
  ```

## 6. O que não vai para o Git

`Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds e arquivos da IDE já estão no `.gitignore`. O
`Assets/Resources/FirebaseConfig.json` é local de cada um (veja `docs/firebase/SETUP.md`); nunca commite.

## 7. Clones sem LFS

Os containers na nuvem (CI, sessões do Claude) pulam o download do LFS e não conseguem enviar arquivos LFS.
Hoje isso não afeta nada, porque PNG e OGG não estão no LFS. Arquivo novo do LFS (ex.: `.aseprite`) tem que ser
commitado e enviado de uma máquina local.
