# Documento de Requisitos — Evenote Desktop App

## Introdução

O Evenote é uma aplicação web Blazor Server (.NET 10, SQLite) que atualmente é executada via browser ou hospedada em nuvem. Esta feature cria um novo projeto **Evenote.Desktop** — um wrapper desktop para Windows — que empacota o servidor web do Evenote internamente e o expõe por meio de uma janela WebView2 (Microsoft Edge embutido), resultando em um único `.exe` distribuível que não exige instalação de .NET na máquina do usuário.

O projeto web original (`Evenote`) **não será modificado**. O `Evenote.Desktop` o referencia como dependência de projeto e orquestra sua inicialização em processo.

---

## Glossário

- **Aplicativo_Desktop**: O projeto `Evenote.Desktop` — executável WinForms que hospeda o servidor e a janela WebView2.
- **Servidor_Web**: A instância do servidor ASP.NET Core do projeto `Evenote` iniciada em processo pelo Aplicativo_Desktop.
- **WebView2**: Controle embutido do Microsoft Edge (Chromium) usado pelo Aplicativo_Desktop para renderizar a interface do Evenote.
- **Porta_Aleatoria**: Porta TCP disponível selecionada dinamicamente pelo sistema operacional no momento da inicialização.
- **Diretorio_AppData**: Caminho `%AppData%\Evenote\` — diretório de dados persistentes do usuário no Windows.
- **Self-Contained**: Modo de publicação que inclui o runtime .NET no pacote, eliminando dependência de .NET pré-instalado.
- **Solution**: Arquivo `Evenote.sln` que agrupa os projetos `Evenote` e `Evenote.Desktop`.

---

## Requisitos

### Requisito 1: Estrutura de Projeto e Solution

**User Story:** Como desenvolvedor, quero um projeto `Evenote.Desktop` separado dentro da mesma solution, para que o projeto web original não seja modificado e os dois projetos possam ser mantidos de forma independente.

#### Critérios de Aceitação

1. THE **Aplicativo_Desktop** SHALL existir como um projeto WinForms (.NET 10, `net10.0-windows`) dentro do diretório `Evenote.Desktop/` na raiz da solution.
2. THE **Solution** SHALL referenciar tanto o projeto `Evenote` quanto o projeto `Evenote.Desktop`.
3. THE **Aplicativo_Desktop** SHALL referenciar o projeto `Evenote` via `<ProjectReference>`, sem modificar nenhum arquivo do projeto `Evenote`.
4. WHEN o projeto `Evenote` for compilado isoladamente, THE **Servidor_Web** SHALL funcionar normalmente, sem qualquer dependência do `Evenote.Desktop`.

---

### Requisito 2: Inicialização do Servidor Web em Processo

**User Story:** Como usuário, quero que o aplicativo desktop inicie o Evenote automaticamente ao abrir, para que eu não precise iniciar o servidor manualmente ou ter um browser separado.

#### Critérios de Aceitação

1. WHEN o **Aplicativo_Desktop** for iniciado, THE **Servidor_Web** SHALL ser iniciado em processo na mesma instância do executável, antes da janela principal ser exibida.
2. WHEN o **Servidor_Web** for iniciado, THE **Aplicativo_Desktop** SHALL selecionar uma **Porta_Aleatoria** disponível dinamicamente, evitando conflitos com outros processos.
3. WHEN o **Servidor_Web** for iniciado, THE **Aplicativo_Desktop** SHALL aguardar a disponibilidade do endpoint `http://localhost:{Porta_Aleatoria}` antes de exibir a janela principal, com tempo máximo de espera de 30 segundos.
4. IF o **Servidor_Web** não responder dentro de 30 segundos após o início, THEN THE **Aplicativo_Desktop** SHALL exibir uma mensagem de erro ao usuário e encerrar o processo.
5. WHILE o **Servidor_Web** estiver em execução, THE **Aplicativo_Desktop** SHALL manter o processo do servidor ativo e monitorar seu estado.

---

### Requisito 3: Janela Principal com WebView2

**User Story:** Como usuário, quero uma janela desktop com visual limpo que exiba o Evenote como se fosse um aplicativo nativo, para que a experiência seja equivalente a um app local.

#### Critérios de Aceitação

1. WHEN o **Servidor_Web** estiver disponível, THE **Aplicativo_Desktop** SHALL abrir uma janela WinForms com um controle **WebView2** navegando para `http://localhost:{Porta_Aleatoria}`.
2. THE **Aplicativo_Desktop** SHALL exibir o título da janela como "Evenote".
3. THE **Aplicativo_Desktop** SHALL exibir um ícone personalizado na barra de tarefas e na barra de título da janela.
4. THE **Aplicativo_Desktop** SHALL abrir a janela em modo maximizado por padrão.
5. THE **WebView2** SHALL ocupar 100% da área útil da janela, sem barras de endereço, botões de navegação do browser ou qualquer chrome do browser visível.
6. WHEN o usuário redimensionar a janela, THE **WebView2** SHALL acompanhar o redimensionamento e preencher toda a área útil disponível.
7. IF o **WebView2** Runtime não estiver instalado na máquina do usuário, THEN THE **Aplicativo_Desktop** SHALL exibir uma mensagem orientando o usuário a instalar o WebView2 Runtime e encerrar o processo.

---

### Requisito 4: Ciclo de Vida e Encerramento

**User Story:** Como usuário, quero que fechar a janela encerre completamente o aplicativo, para que não haja processos do servidor rodando em segundo plano.

#### Critérios de Aceitação

1. WHEN o usuário fechar a janela principal, THE **Aplicativo_Desktop** SHALL encerrar o **Servidor_Web** de forma limpa (graceful shutdown) antes de finalizar o processo.
2. WHEN o encerramento for solicitado, THE **Servidor_Web** SHALL receber o sinal de shutdown e ter até 10 segundos para concluir requisições em andamento antes de ser terminado forçosamente.
3. IF o **Servidor_Web** não encerrar dentro de 10 segundos após o sinal de shutdown, THEN THE **Aplicativo_Desktop** SHALL terminar o processo forçosamente.
4. WHEN o processo do **Aplicativo_Desktop** for encerrado por qualquer motivo (incluindo kill de processo), THE **Servidor_Web** SHALL ser encerrado junto, não deixando processos órfãos.

---

### Requisito 5: Persistência de Dados em AppData

**User Story:** Como usuário, quero que meus dados (banco SQLite e uploads) fiquem em `%AppData%\Evenote\`, para que o aplicativo desktop não misture dados com o projeto web de desenvolvimento.

#### Critérios de Aceitação

1. WHEN o **Aplicativo_Desktop** iniciar o **Servidor_Web**, THE **Aplicativo_Desktop** SHALL configurar a variável de ambiente `DATABASE_PATH` com o valor `%AppData%\Evenote\evenote.db` antes da inicialização do servidor.
2. WHEN o **Aplicativo_Desktop** iniciar o **Servidor_Web**, THE **Aplicativo_Desktop** SHALL configurar a variável de ambiente `UPLOADS_PATH` com o valor `%AppData%\Evenote\uploads` antes da inicialização do servidor.
3. WHEN o **Servidor_Web** for iniciado pelo **Aplicativo_Desktop** e o **Diretorio_AppData** não existir, THE **Servidor_Web** SHALL criar o diretório `%AppData%\Evenote\` automaticamente.
4. WHEN o **Servidor_Web** for iniciado pelo **Aplicativo_Desktop**, THE **Servidor_Web** SHALL aplicar as migrations pendentes do banco de dados no **Diretorio_AppData** automaticamente.
5. THE **Aplicativo_Desktop** SHALL preservar os dados em **Diretorio_AppData** entre reinicializações do aplicativo, não limpando ou sobrescrevendo dados existentes.

---

### Requisito 6: Publicação Self-Contained

**User Story:** Como desenvolvedor, quero publicar o `Evenote.Desktop` como um executável self-contained para Windows x64, para que o usuário final não precise instalar .NET na máquina.

#### Critérios de Aceitação

1. THE **Aplicativo_Desktop** SHALL ser publicável como executável **self-contained** para a plataforma `win-x64`, com o runtime .NET 10 embutido no pacote.
2. THE **Aplicativo_Desktop** SHALL suportar a opção de publicação `PublishSingleFile=true`, gerando um único arquivo `.exe` distribuível.
3. WHEN publicado com `PublishSingleFile=true`, THE **Aplicativo_Desktop** SHALL funcionar corretamente em máquinas Windows 10 (64-bit) e Windows 11 sem qualquer pré-requisito de runtime .NET instalado.
4. THE **Aplicativo_Desktop** SHALL incluir o projeto `Evenote` e todos os seus assets estáticos no pacote publicado, de forma que a interface web seja completamente funcional offline a partir do executável.
5. WHERE a publicação for para distribuição, THE **Aplicativo_Desktop** SHALL suportar configuração de `ApplicationIcon` no `.csproj` para personalização do ícone do executável.

---

### Requisito 7: Experiência de Carregamento

**User Story:** Como usuário, quero feedback visual enquanto o servidor inicializa, para que eu saiba que o aplicativo está carregando e não travou.

#### Critérios de Aceitação

1. WHEN o **Aplicativo_Desktop** for iniciado e o **Servidor_Web** ainda estiver inicializando, THE **Aplicativo_Desktop** SHALL exibir uma tela de splash ou indicador de carregamento.
2. WHEN o **Servidor_Web** estiver disponível, THE **Aplicativo_Desktop** SHALL substituir o indicador de carregamento pelo controle **WebView2** com a interface do Evenote.
3. THE **Aplicativo_Desktop** SHALL exibir o nome "Evenote" e uma mensagem de status ("Iniciando..." ou equivalente) durante o carregamento.
