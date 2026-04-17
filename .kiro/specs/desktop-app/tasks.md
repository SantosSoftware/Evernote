# Plano de Implementação: Parch Desktop App

## Visão Geral

Implementação em duas etapas sequenciais:

1. **Renomeação Evenote → Parch** — atualiza todos os identificadores do projeto web (arquivos, namespaces, configurações) sem alterar nenhuma lógica de negócio.
2. **Projeto Parch.Desktop** — novo projeto WinForms que hospeda o servidor Blazor Server em processo e o expõe via WebView2, resultando em um único `.exe` distribuível para Windows.

---

## Tarefas

- [x] 1. Criar branch de feature
  - Criar o branch `feature/parch-desktop` a partir de `main`
  - Verificar que o branch está ativo antes de qualquer modificação
  - _Requisitos: 1.1_

- [x] 2. Renomear arquivos de projeto e solution
  - [x] 2.1 Renomear `Evenote.csproj` → `Parch.csproj`
    - Usar `git mv Evenote.csproj Parch.csproj` para preservar histórico
    - _Requisitos: 1.1, 1.4_
  - [x] 2.2 Renomear `Evenote.sln` → `Parch.sln` e atualizar referências internas
    - Usar `git mv Evenote.sln Parch.sln`
    - Dentro do arquivo `.sln`, substituir `"Evenote"` → `"Parch"` e `"Evenote.csproj"` → `"Parch.csproj"` nas linhas `Project(...)`
    - _Requisitos: 1.2_
  - [x] 2.3 Renomear banco de dados de desenvolvimento
    - Usar `git mv evenote.db parch.db` (e arquivos `-shm`, `-wal` se existirem)
    - _Requisitos: 5.1_

- [x] 3. Atualizar namespaces e referências em arquivos de código
  - [x] 3.1 Substituir todas as ocorrências de `Evenote` por `Parch` em arquivos `.cs` e `.razor`
    - Executar substituição em lote em todos os `.cs` e `.razor` fora de `obj/` e `bin/`
    - Padrões a substituir: `namespace Evenote`, `using Evenote`, `@using Evenote`
    - Arquivos afetados: `Program.cs`, `Data/AppDbContext.cs`, `Components/_Imports.razor`, `Components/App.razor`, todos os `Components/Pages/*.razor`, `Controllers/GravacaoesController.cs`, todos os `Services/*.cs`, todos os `Migrations/*.cs`
    - _Requisitos: 1.1, 1.4_
  - [x] 3.2 Atualizar `Components/App.razor` — referência ao CSS gerado
    - Substituir `Evenote.styles.css` → `Parch.styles.css`
    - _Requisitos: 1.1_

- [x] 4. Atualizar arquivos de infraestrutura e configuração
  - [x] 4.1 Atualizar `Dockerfile`
    - Substituir `Evenote.csproj` → `Parch.csproj` nas linhas `COPY` e `RUN dotnet restore/publish`
    - Substituir `Evenote.dll` → `Parch.dll` na linha `ENTRYPOINT`
    - Substituir `DATABASE_PATH=/data/evenote.db` → `DATABASE_PATH=/data/parch.db`
    - _Requisitos: 1.1_
  - [x] 4.2 Atualizar `fly.toml`
    - Substituir `app = "evenote"` → `app = "parch"`
    - Substituir `DATABASE_PATH = "/data/evenote.db"` → `DATABASE_PATH = "/data/parch.db"`
    - Substituir `source = "evenote_data"` → `source = "parch_data"`
    - _Requisitos: 1.1_
  - [x] 4.3 Atualizar `appsettings.json` e `appsettings.Development.json` se contiverem referências a `Evenote`
    - Verificar e substituir quaisquer ocorrências de `Evenote` por `Parch`
    - _Requisitos: 1.1_

- [x] 5. Expor `CreateHost()` no `Program.cs` do Parch
  - Extrair a configuração do `WebApplication` do `Program.cs` para um método estático público `ParchApp.CreateHost(string[] args)` em um novo arquivo `ParchApp.cs` na raiz do projeto
  - O método deve retornar `IHost` configurado com todos os serviços e middleware já existentes
  - O `Program.cs` deve chamar `ParchApp.CreateHost(args)` e invocar `.Run()` normalmente
  - Garantir que o projeto `Parch` compila e funciona normalmente após a extração: `dotnet build Parch.csproj`
  - _Requisitos: 2.1, 1.4_

- [x] 6. Checkpoint — verificar build e execução do projeto web renomeado
  - Executar `dotnet build Parch.sln` e confirmar zero erros
  - Executar `dotnet run --project Parch.csproj` e confirmar que a aplicação web inicia normalmente
  - Verificar que o banco `parch.db` é criado/usado corretamente
  - Garantir que todos os testes passam, perguntar ao usuário se houver dúvidas.

- [x] 7. Criar projeto `Parch.Desktop`
  - [x] 7.1 Criar diretório `Parch.Desktop/` e o arquivo `Parch.Desktop.csproj`
    - `OutputType=WinExe`, `TargetFramework=net10.0-windows`, `UseWindowsForms=true`
    - `AssemblyName=Parch`, `RootNamespace=Parch.Desktop`
    - `ApplicationIcon=Resources\parch.ico`
    - Configurações de publicação: `RuntimeIdentifier=win-x64`, `SelfContained=true`, `PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`
    - Dependência NuGet: `Microsoft.Web.WebView2` (versão estável mais recente)
    - `ProjectReference` apontando para `..\Parch.csproj`
    - _Requisitos: 1.1, 1.2, 1.3, 6.1, 6.2, 6.5_
  - [x] 7.2 Criar diretório `Parch.Desktop/Resources/` e adicionar `parch.ico`
    - Criar um ícone `.ico` básico (pode ser um placeholder 32x32) para uso no executável e na janela
    - _Requisitos: 3.3, 6.5_

- [x] 8. Implementar `SplashForm.cs`
  - Criar `Parch.Desktop/SplashForm.cs` com `FormBorderStyle.None`, centralizada na tela, tamanho 400×200
  - Fundo escuro (`Color.FromArgb(30, 30, 30)`)
  - Label com texto "Parch" (fonte Segoe UI 28pt Bold, cor branca)
  - Label com texto "Iniciando..." (fonte Segoe UI 11pt, cor cinza claro)
  - _Requisitos: 7.1, 7.3_

- [x] 9. Implementar `MainForm.cs`
  - Criar `Parch.Desktop/MainForm.cs` com controle `WebView2` com `Dock = DockStyle.Fill`
  - Título da janela: "Parch"; ícone: `parch.ico`; estado inicial: `FormWindowState.Maximized`
  - Handler `OnLoad`: chamar `EnsureCoreWebView2Async()` e navegar para `http://localhost:{Program.Port}/`
  - Handler de erro no `OnLoad`: se WebView2 Runtime não estiver instalado, exibir `MessageBox` com link para download e chamar `Application.Exit()`
  - Handler `OnFormClosed`: chamar `Program.WebHost.StopAsync(CancellationToken com timeout 10s)`, descartar o host e chamar `Environment.Exit(0)`
  - _Requisitos: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 4.1, 4.2, 4.3_

- [x] 10. Implementar `Program.cs` do `Parch.Desktop`
  - Criar `Parch.Desktop/Program.cs` com método `Main` anotado com `[STAThread]`
  - Implementar `SelectAvailablePort()`: cria `TcpListener` na porta 0, inicia, lê a porta atribuída pelo SO, para o listener e retorna a porta
  - Implementar `WaitForServer(url, timeout)`: polling com `HttpClient` a cada 200ms até receber resposta HTTP (status < 500) ou atingir o timeout
  - No `Main`: (1) selecionar porta, (2) configurar `DATABASE_PATH` e `UPLOADS_PATH` via `Environment.SetEnvironmentVariable`, (3) criar diretório `%AppData%\Parch\` se não existir, (4) exibir `SplashForm`, (5) iniciar `ParchApp.CreateHost(args)` em `Task.Run`, (6) aguardar servidor com `WaitForServer` (timeout 30s), (7) fechar splash, (8) se timeout → `MessageBox` de erro + `Environment.Exit(1)`, (9) abrir `MainForm` com `Application.Run`
  - Registrar handler `Application.ThreadException` para exibir erros não tratados
  - _Requisitos: 2.1, 2.2, 2.3, 2.4, 2.5, 4.4, 5.1, 5.2, 5.3, 7.1, 7.2_

- [x] 11. Atualizar `Parch.sln` para incluir `Parch.Desktop`
  - Adicionar entrada `Project(...)` para `Parch.Desktop\Parch.Desktop.csproj` com novo GUID
  - Adicionar entradas nas seções `ProjectConfigurationPlatforms` para `Debug|Any CPU` e `Release|Any CPU`
  - _Requisitos: 1.2_

- [ ] 12. Criar projeto de testes de propriedade `Parch.Desktop.Tests`
  - [ ] 12.1 Criar `Parch.Desktop.Tests/Parch.Desktop.Tests.csproj`
    - `TargetFramework=net10.0`, tipo `Library`
    - Dependências: `FsCheck` (ou `CsCheck`), `xunit`, `Microsoft.NET.Test.Sdk`
    - `ProjectReference` para `Parch.Desktop.csproj`
    - _Requisitos: 5.1, 5.2, 2.2_
  - [ ]* 12.2 Escrever teste de propriedade — Propriedade 1: construção determinística dos caminhos de dados
    - **Propriedade 1: Construção determinística dos caminhos de dados**
    - **Valida: Requisitos 5.1, 5.2**
    - Para qualquer string não-vazia `appDataPath`, verificar que `DATABASE_PATH == Path.Combine(appDataPath, "Parch", "parch.db")` e `UPLOADS_PATH == Path.Combine(appDataPath, "Parch", "uploads")`
    - Mínimo 100 iterações com strings variadas (espaços, Unicode, caminhos longos)
  - [ ]* 12.3 Escrever teste de propriedade — Propriedade 2: seleção de porta não conflitante
    - **Propriedade 2: Seleção de porta não conflitante**
    - **Valida: Requisito 2.2**
    - Para qualquer conjunto de portas ocupadas (simuladas por `TcpListener`s ativos), verificar que `SelectAvailablePort()` retorna uma porta fora desse conjunto e no intervalo `[1024, 65535]`
    - Mínimo 100 iterações com diferentes conjuntos de portas ocupadas

- [x] 13. Checkpoint final — build completo e smoke tests
  - Executar `dotnet build Parch.sln` e confirmar zero erros em ambos os projetos
  - Verificar estaticamente que `Parch.Desktop.csproj` contém `TargetFramework=net10.0-windows`, `UseWindowsForms=true`, `ProjectReference` para `Parch.csproj` e configurações de publicação self-contained
  - Verificar que `Parch.csproj` não contém nenhuma referência a `Parch.Desktop`
  - Verificar que `WebView2.Dock = DockStyle.Fill` está presente no `MainForm`
  - Verificar que o arquivo `parch.ico` existe em `Parch.Desktop/Resources/`
  - Garantir que todos os testes passam, perguntar ao usuário se houver dúvidas.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- Os checkpoints garantem validação incremental antes de avançar
- Os testes de propriedade validam as garantias universais de correção definidas no design
- Os testes de unidade validam exemplos específicos e casos de borda
- A renomeação Evenote → Parch (tarefas 2–4) deve ser concluída e validada **antes** de criar o projeto Desktop (tarefas 7–11)
