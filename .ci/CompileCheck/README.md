# Checagem de compilação (sem Unity)

Compila os scripts do jogo contra as DLLs do `UnityEngine` (pacote NuGet `UnityEngine.Modules`)
para pegar erros de C# sem precisar abrir a Unity nem ter licença. Roda no GitHub Actions a cada push/PR.

Rodar localmente (precisa do .NET SDK 8):

```
dotnet build .ci/CompileCheck
```

Limites: não compila scripts de Editor nem o `2d-extras-master`, e os tipos do uGUI (`Text`, `Image`)
são substituídos por stubs mínimos em `UiStubs.cs`. Se um script novo usar outro tipo do uGUI/TMP,
adicione o stub ali. Isso não substitui testar na Unity: só garante que o código compila.
