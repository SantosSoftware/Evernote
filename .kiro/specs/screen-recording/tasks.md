# Plano de Implementação: Gravação de Tela

## Visão Geral

Implementação incremental da funcionalidade de gravação de tela no editor de notas do Evenote. Cada tarefa constrói sobre a anterior, terminando com a integração completa de todos os componentes.

## Tarefas

- [x] 1. Criar entidade `GravacaoTela`, migração EF Core e atualizar `AppDbContext`
  - Adicionar a classe `GravacaoTela` em `Services/NotaService.cs` (junto às demais entidades do projeto) com os campos: `Id`, `NotaId`, `CaminhoArquivo`, `NomeOriginal`, `TamanhoBytes`, `CriadoEm`
  - Adicionar `DbSet<GravacaoTela>` em `Data/AppDbContext.cs`
  - Configurar a relação `GravacaoTela → Nota` com `OnDelete(DeleteBehavior.Cascade)` no `OnModelCreating`
  - Gerar a migration `AddGravacaoTela` via `dotnet ef migrations add AddGravacaoTela`
  - _Requirements: 3.3, 3.4, 7.3_

- [x] 2. Implementar `GravacaoTelaService`
  - [x] 2.1 Criar `Services/GravacaoTelaService.cs` com os métodos:
    - `SalvarGravacaoAsync(Guid notaId, IFormFile arquivo, string nomeOriginal)` — salva o arquivo em `wwwroot/uploads/gravacoes/` usando a convenção `{notaId}_{yyyyMMddHHmmss}_{guid-curto}.webm` e persiste o registro no banco
    - `ObterPorNotaAsync(Guid notaId)` — retorna lista de `GravacaoTela` ordenada por `CriadoEm` ascendente
    - `ExcluirGravacaoAsync(Guid id)` — remove o arquivo físico (sem lançar exceção se ausente) e o registro do banco
    - `ExcluirPorNotaAsync(Guid notaId)` — remove todos os arquivos físicos e registros associados à nota (usado na exclusão definitiva)
    - Método auxiliar estático `FormatarTamanho(long bytes)` — retorna string legível com unidade (B, KB, MB, GB)
    - Método auxiliar estático `FormatarCronometro(int segundos)` — retorna string no formato `MM:SS`
    - Garantir que o diretório `wwwroot/uploads/gravacoes/` seja criado se não existir
    - _Requirements: 3.2, 3.3, 6.3, 6.4, 7.2_

  - [ ]* 2.2 Escrever teste de propriedade P1 — Formatação do cronômetro sempre produz MM:SS válido
    - **Property 1: Para qualquer inteiro não-negativo de segundos (0–5999), `FormatarCronometro` deve retornar string no formato `MM:SS` onde SS ∈ [0, 59]**
    - **Validates: Requirements 2.2**
    - Usar FsCheck com gerador de inteiros no intervalo [0, 5999]; incluir casos de borda: 0, 59, 60, 3599, 3600

  - [ ]* 2.3 Escrever teste de propriedade P2 — Nome de arquivo gerado é único e contém o NotaId
    - **Property 2: Para qualquer `Guid` de nota e dois timestamps distintos, os nomes gerados devem ser diferentes e ambos devem conter a representação string do `NotaId`**
    - **Validates: Requirements 3.2**
    - Extrair a lógica de geração de nome para método testável; usar FsCheck com pares de `DateTime` distintos

  - [ ]* 2.4 Escrever teste de propriedade P4 — Formatação de tamanho produz string legível com unidade
    - **Property 4: Para qualquer `TamanhoBytes` não-negativo, `FormatarTamanho` deve retornar string não vazia contendo exatamente uma das unidades: "B", "KB", "MB" ou "GB"**
    - **Validates: Requirements 4.4**
    - Usar FsCheck com gerador de `long` não-negativo; incluir casos de borda: 0, 1023, 1024, `long.MaxValue`

- [x] 3. Registrar `GravacaoTelaService` no DI e habilitar controllers
  - Em `Program.cs`, adicionar `builder.Services.AddScoped<GravacaoTelaService>()` e `builder.Services.AddControllers()`
  - Adicionar `app.MapControllers()` no pipeline de requisições
  - Garantir que o diretório `wwwroot/uploads/gravacoes/` seja criado na inicialização (pode ser feito no `Program.cs` ou no próprio serviço)
  - _Requirements: 3.1, 3.2_

- [x] 4. Implementar `GravacaoesController`
  - [x] 4.1 Criar `Controllers/GravacaoesController.cs` com:
    - `POST /api/gravacoes/upload` — recebe `multipart/form-data` com campos `notaId` (Guid) e `arquivo` (IFormFile); valida MIME type (`video/webm`), delega ao `GravacaoTelaService.SalvarGravacaoAsync` e retorna `GravacaoTelaDto` (200) ou `400` com mensagem de erro
    - `DELETE /api/gravacoes/{id}` — delega ao `GravacaoTelaService.ExcluirGravacaoAsync` e retorna `204`
    - Definir o record `GravacaoTelaDto` no mesmo arquivo ou em `Services/NotaService.cs`
    - _Requirements: 3.1, 3.2, 3.3, 6.3_

  - [ ]* 4.2 Escrever teste de propriedade P3 — `GravacaoTela` criada possui todos os campos obrigatórios válidos
    - **Property 3: Para qualquer gravação salva com sucesso, o objeto resultante deve ter `Id ≠ Guid.Empty`, `NotaId` igual ao fornecido, `CaminhoArquivo` não vazio, `NomeOriginal` não vazio, `TamanhoBytes > 0` e `CriadoEm` definido**
    - **Validates: Requirements 3.3, 3.4**
    - Usar FsCheck com banco SQLite em memória; gerar `Guid` de nota e tamanho de arquivo aleatórios

- [x] 5. Checkpoint — Verificar camada de dados e API
  - Garantir que todos os testes passem, que a migration seja aplicada com sucesso e que o endpoint de upload responda corretamente. Perguntar ao usuário se houver dúvidas antes de prosseguir.

- [x] 6. Criar `wwwroot/js/screenRecorder.js`
  - [x] 6.1 Implementar o módulo `window.screenRecorder` com as funções:
    - `verificarSuporteAsync()` — retorna `Promise<bool>` verificando `navigator.mediaDevices?.getDisplayMedia`
    - `iniciarAsync(dotNetRef)` — solicita permissão via `getDisplayMedia({ video: true, audio: true })`; se concedida, cria `MediaRecorder`, inicia gravação, inicia cronômetro com `setInterval` chamando `dotNetRef.invokeMethodAsync('TickCronometro', segundos)` a cada segundo, e chama `dotNetRef.invokeMethodAsync('GravacaoIniciada')`; trata `stream.oninactive` para finalizar automaticamente
    - `pausar()` — chama `mediaRecorder.pause()` e `dotNetRef.invokeMethodAsync('GravacaoPausada')`
    - `retomar()` — chama `mediaRecorder.resume()` e `dotNetRef.invokeMethodAsync('GravacaoRetomada')`
    - `pararAsync()` — para o `MediaRecorder`, consolida os chunks em `Blob` WebM, faz `fetch POST /api/gravacoes/upload` com `FormData` contendo `notaId` e `arquivo`, e chama `dotNetRef.invokeMethodAsync('GravacaoParada', gravacaoTelaDto)` ou `GravacaoErro` em caso de falha
    - `cancelar()` — para o stream sem fazer upload
    - Todos os métodos async devem ter `try/catch` que invocam `GravacaoErro` via `dotNetRef`
    - _Requirements: 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 3.1, 3.5_

  - [x] 6.2 Referenciar `screenRecorder.js` em `Components/App.razor` com tag `<script src="/js/screenRecorder.js"></script>`
    - _Requirements: 1.2_

- [x] 7. Estender `Notas.razor` — estado, callbacks JS e modal de gravação
  - [x] 7.1 Adicionar campos de estado em `@code`:
    - `_gravacoes` (`List<GravacaoTela>`) — lista de gravações da nota selecionada
    - `_estadoGravacao` (enum `EstadoGravacao { Inativo, Gravando, Pausado }`)
    - `_segundosGravacao` (`int`) — segundos decorridos
    - `_erroGravacao` (`string?`) — mensagem de erro da gravação
    - `_suportaGravacao` (`bool`) — resultado de `verificarSuporteAsync`
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 7.2 Adicionar métodos `[JSInvokable]` em `Notas.razor`:
    - `GravacaoIniciada()` — define `_estadoGravacao = Gravando`, chama `StateHasChanged`
    - `GravacaoPausada()` — define `_estadoGravacao = Pausado`, chama `StateHasChanged`
    - `GravacaoRetomada()` — define `_estadoGravacao = Gravando`, chama `StateHasChanged`
    - `GravacaoParada(GravacaoTelaDto dto)` — adiciona a gravação à lista `_gravacoes`, reseta `_estadoGravacao = Inativo` e `_segundosGravacao = 0`, chama `StateHasChanged`
    - `GravacaoErro(string mensagem)` — define `_erroGravacao = mensagem`, reseta estado, chama `StateHasChanged`
    - `TickCronometro(int segundos)` — define `_segundosGravacao = segundos`, chama `StateHasChanged`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.5, 3.6_

  - [x] 7.3 Adicionar item "Gravar tela" no `menu-inserir-dropdown` em `Notas.razor`:
    - Exibir o item somente quando `_suportaGravacao == true`
    - Ao clicar, fechar o menu e invocar `screenRecorder.iniciarAsync(dotNetRef)` via JS Interop
    - Ícone SVG representativo (câmera ou círculo de gravação)
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 7.4 Adicionar bloco `Modal_Gravacao` em `Notas.razor`:
    - Renderizar condicionalmente quando `_estadoGravacao != Inativo`
    - Exibir indicador visual de gravação ativa (ponto vermelho pulsante) quando `_estadoGravacao == Gravando`
    - Exibir cronômetro formatado via `GravacaoTelaService.FormatarCronometro(_segundosGravacao)`
    - Botão "Pausar" visível quando `_estadoGravacao == Gravando` → invoca `screenRecorder.pausar()`
    - Botão "Retomar" visível quando `_estadoGravacao == Pausado` → invoca `screenRecorder.retomar()`
    - Botão "Parar" sempre visível → invoca `screenRecorder.pararAsync()`
    - Exibir `_erroGravacao` quando não nulo
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.5, 3.6_

  - [x] 7.5 Carregar gravações ao selecionar nota:
    - No método `SelecionarNota` e em `OnAfterRenderAsync` (quando `_idCarregado` muda), chamar `GravacaoTelaService.ObterPorNotaAsync(notaSelecionada.Id)` e atribuir a `_gravacoes`
    - Verificar suporte a gravação via `screenRecorder.verificarSuporteAsync()` na inicialização
    - _Requirements: 4.1_

- [x] 8. Estender `Notas.razor` — seção de exibição de gravações
  - [x] 8.1 Adicionar seção "Gravações de tela" abaixo da seção de PDF em `Notas.razor`:
    - Renderizar somente quando `_gravacoes.Count > 0`
    - Para cada gravação, exibir:
      - Player `<video>` HTML5 com atributo `controls` e `src` apontando para `CaminhoArquivo`
      - Data e hora de criação formatada
      - Tamanho do arquivo via `GravacaoTelaService.FormatarTamanho(gravacao.TamanhoBytes)`
      - Botão "Baixar" com `href` para `CaminhoArquivo` e atributo `download="@gravacao.NomeOriginal"`
      - Botão "Excluir" que abre diálogo de confirmação antes de prosseguir
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 5.3, 6.1_

  - [x] 8.2 Implementar exclusão de gravação em `Notas.razor`:
    - Método `ExcluirGravacao(Guid id)` — exibe confirmação via `JS.InvokeAsync<bool>("confirm", "Excluir esta gravação?")`, se confirmado chama `DELETE /api/gravacoes/{id}` via `HttpClient` ou diretamente `GravacaoTelaService.ExcluirGravacaoAsync`, remove o item de `_gravacoes` e chama `StateHasChanged`
    - _Requirements: 6.2, 6.3, 6.4, 6.5_

  - [ ]* 8.3 Escrever teste de propriedade P5 — Cada gravação é renderizada com player, botão de download e botão de exclusão
    - **Property 5: Para qualquer lista não-vazia de `GravacaoTela` (1–20 itens), a seção deve renderizar exatamente N players `<video controls>`, N links de download com `NomeOriginal` e N botões de exclusão**
    - **Validates: Requirements 4.2, 4.3, 5.1, 6.1**
    - Usar bUnit para renderização do componente com lista gerada pelo FsCheck

- [x] 9. Integrar exclusão definitiva da nota com `GravacaoTelaService`
  - [x] 9.1 Modificar `NotaService.ExcluirDefinitivamente` (ou o método correspondente em `Notas.razor`) para chamar `GravacaoTelaService.ExcluirPorNotaAsync(nota.Id)` antes de remover a nota do banco
    - Garantir que os arquivos físicos sejam removidos antes da exclusão do registro (o cascade delete do EF cuida do banco)
    - _Requirements: 7.2, 7.3_

  - [ ]* 9.2 Escrever teste de propriedade P6 — Exclusão de gravação remove o registro do banco
    - **Property 6: Para qualquer `GravacaoTela` existente, após `ExcluirGravacaoAsync(id)`, nenhuma gravação com aquele `Id` deve existir no banco**
    - **Validates: Requirements 6.3**
    - Usar banco SQLite em memória; gerar `GravacaoTela` aleatória via FsCheck

  - [ ]* 9.3 Escrever teste de propriedade P7 — Mover nota para lixeira preserva todas as gravações
    - **Property 7: Para qualquer nota com N gravações (0–10), após `MoverParaLixeira`, o número de registros `GravacaoTela` com aquele `NotaId` deve permanecer N**
    - **Validates: Requirements 7.1**
    - Usar banco SQLite em memória; gerar nota com lista de gravações via FsCheck

  - [ ]* 9.4 Escrever teste de propriedade P8 — Exclusão definitiva remove todas as gravações em cascata
    - **Property 8: Para qualquer nota com N gravações (N ≥ 0), após `ExcluirDefinitivamente`, nenhum registro `GravacaoTela` com aquele `NotaId` deve existir no banco**
    - **Validates: Requirements 7.2, 7.3**
    - Usar banco SQLite em memória; verificar tanto o cascade delete do EF quanto a remoção dos arquivos físicos

- [x] 10. Adicionar estilos CSS em `Notas.razor.css`
  - Estilos para o modal de gravação: `.modal-gravacao`, `.gravacao-indicador` (ponto vermelho pulsante com animação `@keyframes`), `.gravacao-cronometro`, `.gravacao-controles`, `.gravacao-erro`
  - Estilos para a seção de gravações: `.gravacoes-secao`, `.gravacao-item`, `.gravacao-video`, `.gravacao-meta`, `.gravacao-acoes`, `.btn-baixar`, `.btn-excluir-gravacao`
  - Seguir o padrão visual existente (cores, border-radius, transições) do arquivo CSS atual
  - _Requirements: 2.1, 2.2, 4.1, 4.2_

- [x] 11. Checkpoint final — Garantir que todos os testes passem
  - Executar `dotnet build` para verificar erros de compilação
  - Executar `dotnet test` para verificar que todos os testes (unitários e de propriedade) passam
  - Verificar que a migration foi aplicada e que o diretório `wwwroot/uploads/gravacoes/` existe
  - Perguntar ao usuário se houver dúvidas antes de encerrar.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia os requisitos específicos para rastreabilidade
- Os testes de propriedade usam **FsCheck** (via `FsCheck.Xunit`) — adicionar o pacote ao projeto de testes antes de implementá-los
- O `GravacaoTelaService` deve ser registrado como `Scoped` para alinhar com o ciclo de vida do `AppDbContext`
- O `screenRecorder.js` deve ser carregado de forma não-bloqueante; verificar suporte (`verificarSuporteAsync`) na inicialização do componente para ocultar o item do menu quando necessário
- A convenção de nome de arquivo `{notaId}_{yyyyMMddHHmmss}_{guid-curto}.webm` garante unicidade mesmo com uploads simultâneos
