# Documento de Requisitos — Deploy na Nuvem (Fly.io)

## Introdução

Este documento descreve os requisitos para hospedar o aplicativo Evenote (Blazor Server, .NET 10, SQLite) na plataforma Fly.io, permitindo acesso remoto de qualquer dispositivo com internet. O escopo inclui a correção do Dockerfile, a configuração de volumes persistentes para banco de dados e uploads, e a criação de um pipeline de CI/CD via GitHub Actions para deploy automático a cada push na branch principal.

## Glossário

- **Evenote**: Aplicativo Blazor Server (.NET 10) com banco de dados SQLite e armazenamento de arquivos locais.
- **Dockerfile**: Arquivo de definição da imagem Docker usada para empacotar e executar o Evenote.
- **Fly.io**: Plataforma de hospedagem em nuvem baseada em containers que executa a imagem Docker do Evenote.
- **fly.toml**: Arquivo de configuração declarativa do Fly.io que define a aplicação, portas, volumes e variáveis de ambiente.
- **Volume_Persistente**: Disco gerenciado pelo Fly.io montado no container para preservar dados entre deploys e reinicializações.
- **GitHub_Actions**: Serviço de CI/CD do GitHub que executa workflows automatizados a cada evento no repositório.
- **Pipeline_CI_CD**: Workflow do GitHub Actions responsável por construir a imagem Docker e fazer deploy no Fly.io.
- **FLY_API_TOKEN**: Token de autenticação da API do Fly.io armazenado como secret no repositório GitHub.
- **Migration**: Processo automático do EF Core que aplica alterações de esquema ao banco de dados SQLite na inicialização.
- **wwwroot/uploads**: Diretório dentro do container onde PDFs e gravações WebM enviados pelos usuários são armazenados.

---

## Requisitos

### Requisito 1: Correção do Dockerfile para .NET 10

**User Story:** Como desenvolvedor, quero que o Dockerfile use .NET 10 e o nome correto do binário, para que a imagem Docker construída execute o Evenote corretamente.

#### Critérios de Aceitação

1. THE Dockerfile SHALL usar a imagem base `mcr.microsoft.com/dotnet/sdk:10.0` na etapa de build.
2. THE Dockerfile SHALL usar a imagem base `mcr.microsoft.com/dotnet/aspnet:10.0` na etapa de runtime.
3. THE Dockerfile SHALL referenciar o binário `Evenote.dll` no `ENTRYPOINT`.
4. WHEN a imagem Docker é construída a partir do Dockerfile corrigido, THE Dockerfile SHALL produzir uma imagem que inicializa o Evenote sem erros de runtime.
5. THE Dockerfile SHALL expor a porta `8080` e configurar `ASPNETCORE_URLS=http://+:8080`.
6. THE Dockerfile SHALL usar build multi-estágio para minimizar o tamanho da imagem final.

---

### Requisito 2: Configuração do Fly.io

**User Story:** Como usuário, quero que o Evenote esteja hospedado no Fly.io com dados persistentes, para que minhas notas, tarefas e arquivos não sejam perdidos entre deploys ou reinicializações.

#### Critérios de Aceitação

1. THE fly.toml SHALL definir o nome da aplicação, região e configuração de porta HTTP interna `8080`.
2. THE fly.toml SHALL declarar um Volume_Persistente montado em `/data` dentro do container.
3. WHEN o container é iniciado, THE Evenote SHALL armazenar o arquivo `evenote.db` no caminho `/data/evenote.db`.
4. WHEN o container é iniciado, THE Evenote SHALL armazenar os arquivos de upload no caminho `/data/uploads/`.
5. WHEN um deploy é realizado, THE Volume_Persistente SHALL preservar os dados existentes do banco de dados e dos uploads.
6. IF o Volume_Persistente não estiver montado, THEN THE Evenote SHALL registrar um erro de inicialização e encerrar o processo com código de saída diferente de zero.
7. THE fly.toml SHALL configurar verificação de saúde (health check) no endpoint `/` com intervalo máximo de 30 segundos.
8. WHERE a variável de ambiente `DATABASE_PATH` estiver definida, THE Evenote SHALL usar seu valor como caminho do arquivo `evenote.db` em vez do caminho padrão.

---

### Requisito 3: Adaptação do Program.cs para caminhos configuráveis

**User Story:** Como operador, quero que os caminhos do banco de dados e dos uploads sejam configuráveis via variável de ambiente, para que o Evenote funcione corretamente tanto em desenvolvimento local quanto no container do Fly.io.

#### Critérios de Aceitação

1. WHEN a variável de ambiente `DATABASE_PATH` estiver definida, THE Evenote SHALL usar seu valor como caminho absoluto do arquivo `evenote.db`.
2. WHEN a variável de ambiente `DATABASE_PATH` não estiver definida, THE Evenote SHALL usar o caminho padrão `{ContentRootPath}/evenote.db` (comportamento atual preservado).
3. WHEN a variável de ambiente `UPLOADS_PATH` estiver definida, THE Evenote SHALL usar seu valor como diretório base para armazenamento e leitura de arquivos de upload.
4. WHEN a variável de ambiente `UPLOADS_PATH` não estiver definida, THE Evenote SHALL usar o caminho padrão `{WebRootPath}/uploads/` (comportamento atual preservado).
5. WHEN o Evenote é inicializado, THE Evenote SHALL criar os diretórios necessários para banco de dados e uploads caso não existam.

---

### Requisito 4: Pipeline de CI/CD via GitHub Actions

**User Story:** Como desenvolvedor, quero que cada push na branch principal dispare automaticamente o build e o deploy no Fly.io, para que novas versões do Evenote sejam publicadas sem intervenção manual.

#### Critérios de Aceitação

1. WHEN um push é feito na branch `main` ou `master`, THE Pipeline_CI_CD SHALL iniciar automaticamente o processo de build e deploy.
2. THE Pipeline_CI_CD SHALL construir a imagem Docker usando o Dockerfile do repositório.
3. THE Pipeline_CI_CD SHALL fazer deploy da imagem construída no Fly.io usando o `FLY_API_TOKEN`.
4. IF o build Docker falhar, THEN THE Pipeline_CI_CD SHALL interromper o workflow e registrar o erro sem tentar o deploy.
5. IF o deploy no Fly.io falhar, THEN THE Pipeline_CI_CD SHALL registrar o erro e retornar código de saída diferente de zero para sinalizar falha no workflow.
6. THE Pipeline_CI_CD SHALL usar o secret `FLY_API_TOKEN` armazenado no repositório GitHub para autenticação, sem expor o valor em logs.
7. THE Pipeline_CI_CD SHALL executar em ambiente `ubuntu-latest`.
8. WHEN o deploy é concluído com sucesso, THE Pipeline_CI_CD SHALL exibir a URL pública da aplicação no log do workflow.

---

### Requisito 5: Segurança e acesso

**User Story:** Como usuário, quero que o Evenote hospedado na nuvem seja acessível apenas por HTTPS, para que meus dados trafeguem de forma segura.

#### Critérios de Aceitação

1. THE fly.toml SHALL configurar o handler HTTPS na porta `443` com redirecionamento automático de HTTP para HTTPS.
2. WHEN uma requisição HTTP é recebida na porta `80`, THE Fly.io SHALL redirecionar automaticamente para HTTPS.
3. THE fly.toml SHALL desabilitar `force_https` apenas em ambiente de desenvolvimento local.
4. THE Evenote SHALL desabilitar `UseHttpsRedirection` dentro do container, delegando o TLS ao proxy do Fly.io.

---

### Requisito 6: Observabilidade e logs

**User Story:** Como operador, quero ter acesso aos logs da aplicação em produção, para que eu possa diagnosticar erros e monitorar o comportamento do Evenote na nuvem.

#### Critérios de Aceitação

1. WHEN o Evenote é executado no Fly.io, THE Evenote SHALL emitir logs estruturados para a saída padrão (`stdout`) e erro padrão (`stderr`).
2. WHEN uma Migration é aplicada na inicialização, THE Evenote SHALL registrar no log o nome de cada migration aplicada.
3. WHEN um erro não tratado ocorre, THE Evenote SHALL registrar o stack trace completo no log antes de encerrar.
4. THE fly.toml SHALL configurar o nível mínimo de log como `Information` em produção.
