# Plano de Implementação: Deploy na Nuvem (Fly.io)

## Visão Geral

Implementação incremental das mudanças de infraestrutura para hospedar o Evenote no Fly.io: correção do Dockerfile, criação do `fly.toml`, adaptação do `Program.cs` para caminhos configuráveis via variáveis de ambiente, e pipeline de CI/CD via GitHub Actions.

## Tarefas

- [x] 1. Corrigir o Dockerfile para .NET 10
  - Substituir `sdk:8.0` por `sdk:10.0` na etapa de build
  - Substituir `aspnet:8.0` por `aspnet:10.0` na etapa de runtime
  - Corrigir o `ENTRYPOINT` de `Evernote.dll` para `Evenote.dll`
  - Adicionar `COPY Evenote.csproj ./` e `RUN dotnet restore` antes do `COPY . .` para aproveitar cache de camadas
  - Adicionar `RUN mkdir -p /data/uploads` na etapa de runtime
  - Definir variáveis de ambiente padrão: `DATABASE_PATH=/data/evenote.db`, `UPLOADS_PATH=/data/uploads`, `ASPNETCORE_ENVIRONMENT=Production`
  - _Requisitos: 1.1, 1.2, 1.3, 1.5, 1.6_

  - [ ]* 1.1 Verificar conteúdo do Dockerfile gerado
    - Confirmar que o arquivo contém `sdk:10.0`, `aspnet:10.0` e `Evenote.dll`
    - _Requisitos: 1.1, 1.2, 1.3_

- [x] 2. Criar o arquivo fly.toml
  - Criar o arquivo `fly.toml` na raiz do projeto
  - Definir `app = "evenote"` e `primary_region = "gru"`
  - Configurar `[build]` apontando para o `Dockerfile`
  - Definir variáveis de ambiente em `[env]`: `ASPNETCORE_ENVIRONMENT`, `DATABASE_PATH`, `UPLOADS_PATH`
  - Declarar o mount `[[mounts]]` com `source = "evenote_data"` e `destination = "/data"`
  - Configurar `[[services]]` com `internal_port = 8080` e as portas 80 (com `force_https = true`) e 443
  - Adicionar `[[services.http_checks]]` no endpoint `/` com `grace_period = "30s"`
  - _Requisitos: 2.1, 2.2, 2.7, 5.1, 5.2, 5.3_

  - [ ]* 2.1 Verificar conteúdo do fly.toml gerado
    - Confirmar presença de `[[mounts]]` com `destination = "/data"`, `force_https = true` e health check configurado
    - _Requisitos: 2.2, 5.1, 2.7_

- [x] 3. Adaptar Program.cs para caminhos configuráveis via variáveis de ambiente
  - [x] 3.1 Tornar o caminho do banco de dados configurável
    - Substituir `Path.Combine(builder.Environment.ContentRootPath, "evenote.db")` por leitura de `DATABASE_PATH` com fallback para o caminho atual
    - Adicionar `Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!)` logo após a resolução do caminho
    - _Requisitos: 3.1, 3.2, 3.5_

  - [ ]* 3.2 Escrever teste de propriedade para resolução do caminho do banco de dados
    - **Propriedade 1: Resolução do caminho do banco de dados**
    - Para qualquer caminho absoluto válido em `DATABASE_PATH`, o connection string do `DbContext` deve usar exatamente esse caminho como `Data Source`
    - **Valida: Requisitos 2.3, 2.8, 3.1**

  - [ ]* 3.3 Escrever teste de propriedade para fallback de caminhos padrão
    - **Propriedade 4: Fallback para caminhos padrão**
    - Sem `DATABASE_PATH` definida, o caminho deve ser `{ContentRootPath}/evenote.db`; sem `UPLOADS_PATH`, deve ser `{WebRootPath}/uploads`
    - **Valida: Requisitos 3.2, 3.4**

  - [x] 3.4 Tornar o caminho de uploads configurável
    - Atualizar o endpoint `MapGet("/uploads/{*filename}")` para ler `UPLOADS_PATH` com fallback para `{WebRootPath}/uploads`
    - _Requisitos: 3.3, 3.4_

  - [ ]* 3.5 Escrever teste de propriedade para resolução do caminho de uploads
    - **Propriedade 2: Resolução do caminho de uploads**
    - Para qualquer caminho absoluto válido em `UPLOADS_PATH`, o endpoint `/uploads/{filename}` deve buscar o arquivo concatenando `UPLOADS_PATH` com o nome do arquivo
    - **Valida: Requisitos 2.4, 3.3**

  - [ ]* 3.6 Escrever teste de propriedade para criação de diretórios na inicialização
    - **Propriedade 3: Criação de diretórios na inicialização**
    - Para qualquer caminho de diretório válido em `DATABASE_PATH` ou `UPLOADS_PATH`, após a inicialização o diretório pai deve existir no sistema de arquivos
    - **Valida: Requisito 3.5**

  - [x] 3.7 Remover UseHttpsRedirection
    - Remover (ou comentar com explicação) a chamada `app.UseHttpsRedirection()` do pipeline de middleware
    - Adicionar comentário explicando que o TLS é gerenciado pelo proxy do Fly.io
    - _Requisitos: 5.4_

- [x] 4. Adicionar logs estruturados de migrations
  - Injetar `ILogger<AppDbContext>` no bloco de inicialização do banco de dados
  - Chamar `db.Database.GetPendingMigrations()` antes de `db.Database.Migrate()`
  - Registrar com `logger.LogInformation` o número e os nomes das migrations pendentes
  - _Requisitos: 6.1, 6.2_

  - [ ]* 4.1 Escrever teste de propriedade para logging de migrations
    - **Propriedade 5: Logging de migrations**
    - Para qualquer lista não vazia de migrations pendentes, após `db.Database.Migrate()` cada migration deve ter sido registrada no log com nível `Information`
    - **Valida: Requisito 6.2**

- [x] 5. Checkpoint — Verificar se todas as alterações locais estão corretas
  - Garantir que o projeto compila sem erros com `dotnet build`
  - Verificar que o `Program.cs` não contém `UseHttpsRedirection`
  - Verificar que `DATABASE_PATH` e `UPLOADS_PATH` são lidas corretamente com fallback
  - Garantir que todos os testes passam, tirar dúvidas com o usuário se necessário.

- [x] 6. Criar o pipeline de CI/CD via GitHub Actions
  - Criar o diretório `.github/workflows/` se não existir
  - Criar o arquivo `.github/workflows/deploy.yml`
  - Configurar o trigger `on.push.branches: [main, master]`
  - Adicionar step de checkout com `actions/checkout@v4`
  - Adicionar step de instalação do `flyctl` com `superfly/flyctl-actions/setup-flyctl@master`
  - Adicionar step de deploy com `flyctl deploy --remote-only` usando `FLY_API_TOKEN` do secret
  - Adicionar step final para exibir a URL pública com `flyctl status`
  - Configurar `concurrency` para cancelar deploys anteriores em andamento
  - _Requisitos: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_

  - [ ]* 6.1 Verificar conteúdo do deploy.yml gerado
    - Confirmar presença de `secrets.FLY_API_TOKEN`, `--remote-only` e `cancel-in-progress: true`
    - _Requisitos: 4.6, 4.3_

- [x] 7. Checkpoint final — Garantir que todos os testes passam
  - Executar `dotnet build` e confirmar que não há erros de compilação
  - Garantir que todos os testes passam, tirar dúvidas com o usuário se necessário.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- Os testes de propriedade usam **xUnit** com **FsCheck** ou **CsCheck**
- O volume `evenote_data` deve ser criado manualmente antes do primeiro deploy: `fly volumes create evenote_data --size 1 --region gru`
- O secret `FLY_API_TOKEN` deve ser configurado no repositório GitHub antes de ativar o pipeline
- Checkpoints garantem validação incremental a cada etapa
