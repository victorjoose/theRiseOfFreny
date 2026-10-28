# Recursos online com Firebase

O jogo tem três recursos online, todos em `Assets/Scripts/Online/`:

- **Login automático**: na primeira vez que o jogo abre, o jogador ganha uma conta de convidado (Firebase Auth anônimo). Depois ele pode ligar essa conta a um email e senha para levar o progresso para outro computador.
- **Leaderboard global**: quando o jogador morre, a pontuação da run é enviada. Cada jogador tem uma entrada com o melhor score. A tecla **Tab** abre o top 10, que também aparece sozinho depois da morte.
- **Save na nuvem**: nome, melhor score, número de runs e total de moedas ficam em `saves/{uid}`. Runs jogadas offline ficam guardadas no PlayerPrefs e sobem na próxima vez que o jogo conectar.

## Por que REST e não o Firebase Unity SDK

O Firebase Unity SDK só tem suporte oficial para Android e iOS. No Windows/Mac/Linux ele roda apenas como "beta para desenvolvimento" e não funciona em WebGL. Como o The Rise of Freny é um jogo de PC, a integração usa as APIs REST do Firebase com `UnityWebRequest`. Assim não tem pacote nativo para importar, nada muda no `Packages/manifest.json`, e o mesmo código funciona em PC, WebGL e mobile.

Se um dia o jogo for para celular e precisar de Crashlytics ou push, dá para adicionar o SDK nessa hora.

## Projeto atual

O projeto já foi criado: **The Rise of Freny** (`the-rise-of-freny`), com login anônimo e email/senha ativos, Firestore em `southamerica-east1` (São Paulo) e as regras de `firebase/firestore.rules` publicadas. Para usar, basta ter o `Assets/Resources/FirebaseConfig.json` (passo 4). O `apiKey` está em Configurações do projeto > Seus apps > Unity game.

Os passos abaixo servem para recriar tudo em outro projeto.

## Passo a passo (uns 10 minutos)

### 1. Criar o projeto

1. Abra https://console.firebase.google.com e clique em **Adicionar projeto**. Pode chamar de `the-rise-of-freny`. O Google Analytics é opcional.
2. Na tela do projeto, clique no ícone **Web (`</>`)** para registrar um app. Qualquer apelido serve, e não precisa ativar o Hosting.
3. O console mostra um bloco `firebaseConfig`. Anote o `apiKey` e o `projectId`.

### 2. Ativar o login

1. Vá em **Build > Authentication > Começar**.
2. Na aba **Método de login**, ative **Anônimo**.
3. Ative também **Email/senha** (sem o "link por email"). É o que permite levar o progresso para outro computador.

### 3. Criar o banco

1. Vá em **Build > Firestore Database > Criar banco de dados**.
2. Escolha uma região perto dos jogadores (por exemplo `southamerica-east1`, São Paulo).
3. Comece em **modo de produção**.
4. Gere a chave de assinatura do ranking (passo 5) e, na aba **Regras**, apague o que estiver lá, cole o conteúdo de `Tools/ScoreKey/firestore.rules.publish` e clique em **Publicar**. Não publique o `firebase/firestore.rules` direto: ele tem `__SCORE_KEY__` no lugar da chave, e aí nenhuma pontuação seria aceita.

### 4. Configurar o jogo

1. Crie a pasta `Assets/Resources/` se ela não existir.
2. Copie [`FirebaseConfig.example.json`](FirebaseConfig.example.json) para `Assets/Resources/FirebaseConfig.json`.
3. Preencha com o `apiKey` e o `projectId` do passo 1.

Esse arquivo está no `.gitignore`. A `apiKey` do Firebase não é um segredo de verdade (ela vai dentro de qualquer build do jogo), quem protege os dados são as regras do Firestore. Mesmo assim, fora do repositório fica mais fácil trocar de projeto.

### 5. Chave de assinatura do ranking

O jogo assina cada pontuação (`ScoreSignature.cs`) e as regras só aceitam pontuações com a assinatura certa. A chave não pode ir para o repositório, que é público:

```bash
python Tools/ScoreKey/make_score_key.py caminho/da/score.key          # chave que já existe
python Tools/ScoreKey/make_score_key.py --new caminho/da/score.key    # chave nova
```

Isso escreve `Assets/Scripts/Online/ScoreKey.cs` (o jogo) e `Tools/ScoreKey/firestore.rules.publish` (as regras com a chave), os dois no `.gitignore`. A chave atual fica nos arquivos do projeto no Claude (`firebase/score.key`). Trocar a chave exige publicar as regras novas e soltar um build novo juntos. Um build sem `ScoreKey.cs` roda normal, mas não manda pontuação para o ranking.

As regras também exigem o "ticket" da corrida (`runs/{uid}`, gravado com a hora do servidor quando uma corrida da Arena começa) e só aceitam uma pontuação que caiba no tempo real da corrida (`maxScoreAfter`).

Sem o `FirebaseConfig.json` o jogo funciona normal, só que offline, e o painel do Tab avisa que os recursos online estão desligados.

### 5. Testar

1. Dê Play na `JoviScene` ou na `MaxScene`.
2. No Console deve aparecer qualquer aviso com `[Online]` se algo der errado. Se não aparecer nada, o login funcionou.
3. Aperte **Tab**: o status deve mostrar **Online**.
4. Jogue a Arena no modo Manual e morra. A pontuação aparece no topo da tela e o leaderboard abre.
5. No console do Firebase, em **Firestore Database > Dados**, deve aparecer a coleção da dificuldade jogada (`leaderboard_hard`, `leaderboard_freny`, `leaderboard_medium` ou `leaderboard_easy`) e a `saves`.

## Como funciona

| Arquivo | O que faz |
| --- | --- |
| `OnlineServices.cs` | Se cria sozinho quando o jogo inicia (`RuntimeInitializeOnLoadMethod`), então não precisa mexer em nenhuma cena. Faz o login, sincroniza o save e detecta a morte do jogador. |
| `FirebaseAuthService.cs` | Login anônimo, email/senha e renovação do token (o token dura 1 hora). O refresh token fica no PlayerPrefs. |
| `FirestoreClient.cs` | Cliente mínimo do Firestore via REST: ler, gravar campos e fazer query. |
| `LeaderboardService.cs` | Um ranking por dificuldade, nesta ordem: Difícil (o oficial), Freny, Médio e Fácil (`ArenaDifficulty.Ranked`). Grava o melhor score em `leaderboard_<dificuldade>/{uid}` e busca o top 10 de cada um. Só entram corridas da Arena jogadas no Manual do começo ao fim. A coleção antiga `leaderboard` misturava todas as dificuldades e o jogo não grava mais nela. |
| `CloudSaveService.cs` | Lê e grava `saves/{uid}`. |
| `OnlineHUD.cs` | Painel provisório em IMGUI (Tab). A ideia é trocar por uma tela de verdade quando os menus forem refeitos. |

A pontuação é lida de `UIManager.scoreCountHUD` e as moedas de `Player.coins`. Na cena do Runner ele usa `ScoreManager.score`. Se a pontuação passar a ficar em outro lugar (por exemplo um `int` no `GameManager`), basta ajustar `OnlineServices.CurrentScore()`.

## Limites conhecidos

- **Dá para trapacear.** O score é enviado pelo próprio jogo, então alguém que edite as requisições consegue mandar um número alto. As regras só garantem que cada um mexe na própria entrada, que o score não diminui e que fica abaixo de 10.000.000. Para um leaderboard confiável, o próximo passo é validar a run numa Cloud Function (exige o plano Blaze).
- **Conta de convidado é do computador.** Se o jogador limpar o PlayerPrefs ou trocar de PC sem ligar um email, ele começa do zero.
- **Custo**: o plano grátis (Spark) cobre 50 mil leituras e 20 mil gravações por dia. Cada run grava 2 documentos e o leaderboard lê 10, então dá para milhares de runs por dia sem pagar nada.

## Próximos passos possíveis

- Validar scores com Cloud Functions.
- Login com Google ou Steam.
- Leaderboard semanal (uma coleção por semana).
- Desafio diário com seed compartilhada.
- Remote Config para ajustar dificuldade e preços da loja sem gerar build nova.
