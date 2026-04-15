# Requirements Document

## Introduction

Esta funcionalidade adiciona a capacidade de gravar a tela do computador diretamente a partir do editor de notas do Evenote. O usuário pode iniciar uma gravação pelo menu "Inserir" da toolbar, controlar a gravação (iniciar, pausar, retomar e parar), e ao finalizar a gravação ela é salva no servidor e associada à nota atual. O arquivo de vídeo gravado pode ser reproduzido inline na nota e exportado/baixado para a máquina do usuário.

A gravação utiliza a API `getDisplayMedia` do browser (Screen Capture API), que é suportada nos navegadores modernos e não requer plugins. O arquivo resultante é armazenado no servidor em formato WebM e referenciado pela nota no banco de dados SQLite.

## Glossary

- **Gravador**: Componente JavaScript responsável por interagir com a Screen Capture API do browser, controlar o estado da gravação e enviar os dados ao servidor.
- **GravacaoTela**: Entidade de domínio que representa uma gravação de tela associada a uma nota, armazenada no banco de dados.
- **GravacaoTelaService**: Serviço .NET responsável por persistir, recuperar e excluir registros de `GravacaoTela` no banco de dados SQLite.
- **Nota**: Entidade existente no sistema que representa uma nota do usuário, podendo ter zero ou mais gravações de tela associadas.
- **Toolbar**: Barra de ferramentas do editor de notas, contendo o botão "Inserir" e demais controles de formatação.
- **Menu_Inserir**: Dropdown acessado pelo botão "Inserir" na Toolbar, que lista as opções de conteúdo a inserir na nota.
- **Modal_Gravacao**: Painel flutuante exibido durante e após a gravação, que apresenta os controles de gravação e o player de vídeo.
- **WebM**: Formato de vídeo aberto utilizado para armazenar as gravações, compatível com a API MediaRecorder dos browsers modernos.

## Requirements

### Requirement 1: Acesso à gravação pelo Menu Inserir

**User Story:** Como usuário, quero acessar a opção de gravar a tela pelo menu "Inserir" da toolbar do editor, para que eu possa iniciar uma gravação sem sair do contexto da nota.

#### Acceptance Criteria

1. WHEN uma nota está selecionada no editor, THE Menu_Inserir SHALL exibir a opção "Gravar tela" com um ícone representativo.
2. WHEN o usuário clica em "Gravar tela" no Menu_Inserir, THE Gravador SHALL solicitar permissão ao browser para captura de tela via `getDisplayMedia`.
3. IF o browser não suportar a API `getDisplayMedia`, THEN THE Menu_Inserir SHALL ocultar a opção "Gravar tela".
4. IF o usuário negar a permissão de captura de tela, THEN THE Gravador SHALL exibir uma mensagem de erro informando que a permissão foi negada e não iniciar a gravação.

---

### Requirement 2: Controle da gravação (iniciar, pausar, retomar e parar)

**User Story:** Como usuário, quero controlar o ciclo de vida da gravação (iniciar, pausar, retomar e parar), para que eu possa capturar exatamente o conteúdo desejado.

#### Acceptance Criteria

1. WHEN a permissão de captura de tela é concedida, THE Gravador SHALL iniciar a gravação e exibir o Modal_Gravacao com os controles de gravação.
2. WHILE a gravação está em andamento, THE Modal_Gravacao SHALL exibir um indicador visual de gravação ativa e um cronômetro com o tempo decorrido em formato MM:SS.
3. WHILE a gravação está em andamento, THE Modal_Gravacao SHALL exibir um botão "Pausar" que suspende temporariamente a captura.
4. WHILE a gravação está pausada, THE Modal_Gravacao SHALL exibir um botão "Retomar" que reinicia a captura a partir do ponto pausado.
5. WHILE a gravação está em andamento ou pausada, THE Modal_Gravacao SHALL exibir um botão "Parar" que encerra a gravação e inicia o processo de salvamento.
6. WHEN o usuário clica em "Parar", THE Gravador SHALL finalizar a captura e consolidar os dados gravados em um único arquivo WebM.

---

### Requirement 3: Salvamento e associação da gravação à nota

**User Story:** Como usuário, quero que a gravação seja salva automaticamente e associada à nota atual, para que eu possa acessá-la novamente no futuro.

#### Acceptance Criteria

1. WHEN a gravação é finalizada, THE Gravador SHALL enviar o arquivo WebM ao servidor via upload HTTP.
2. WHEN o upload é concluído com sucesso, THE GravacaoTelaService SHALL salvar o arquivo no diretório `wwwroot/uploads/gravacoes/` com um nome único baseado no Id da nota e um timestamp.
3. WHEN o arquivo é salvo no servidor, THE GravacaoTelaService SHALL criar um registro de `GravacaoTela` no banco de dados SQLite contendo: Id, NotaId, CaminhoArquivo, NomeOriginal, TamanhoBytes e CriadoEm.
4. THE GravacaoTela SHALL estar associada a exatamente uma Nota por meio de uma chave estrangeira `NotaId`.
5. IF o upload falhar, THEN THE Modal_Gravacao SHALL exibir uma mensagem de erro descritiva e manter o arquivo temporário disponível para nova tentativa.
6. WHEN o upload é concluído com sucesso, THE Modal_Gravacao SHALL exibir o player de vídeo inline com a gravação recém-salva.

---

### Requirement 4: Exibição das gravações na nota

**User Story:** Como usuário, quero visualizar as gravações associadas a uma nota diretamente no editor, para que eu possa rever o conteúdo gravado sem sair da nota.

#### Acceptance Criteria

1. WHEN uma nota com gravações associadas é selecionada no editor, THE Notas SHALL exibir uma seção "Gravações de tela" abaixo do conteúdo da nota.
2. THE Notas SHALL exibir cada gravação como um player de vídeo HTML5 nativo com controles de reprodução (play, pause, volume, tela cheia).
3. WHEN há mais de uma gravação associada à nota, THE Notas SHALL exibir todas as gravações em sequência na seção de gravações.
4. THE Notas SHALL exibir, para cada gravação, a data e hora de criação e o tamanho do arquivo em formato legível (ex.: "12,4 MB").

---

### Requirement 5: Download da gravação

**User Story:** Como usuário, quero baixar uma gravação para minha máquina, para que eu possa compartilhá-la ou armazená-la fora do Evenote.

#### Acceptance Criteria

1. THE Notas SHALL exibir um botão "Baixar" para cada gravação listada na seção de gravações.
2. WHEN o usuário clica em "Baixar", THE Notas SHALL iniciar o download do arquivo WebM para a máquina do usuário com o nome original da gravação.
3. THE arquivo baixado SHALL estar no formato WebM e ser reproduzível em players de vídeo compatíveis com esse formato.

---

### Requirement 6: Exclusão de gravações

**User Story:** Como usuário, quero excluir gravações que não preciso mais, para que eu possa liberar espaço e manter a nota organizada.

#### Acceptance Criteria

1. THE Notas SHALL exibir um botão "Excluir" para cada gravação listada na seção de gravações.
2. WHEN o usuário clica em "Excluir", THE Notas SHALL solicitar confirmação antes de prosseguir com a exclusão.
3. WHEN a exclusão é confirmada, THE GravacaoTelaService SHALL remover o registro de `GravacaoTela` do banco de dados e excluir o arquivo físico do servidor.
4. IF o arquivo físico não existir no momento da exclusão, THEN THE GravacaoTelaService SHALL remover apenas o registro do banco de dados sem gerar erro.
5. WHEN a exclusão é concluída, THE Notas SHALL atualizar a seção de gravações removendo o item excluído.

---

### Requirement 7: Persistência e integridade dos dados

**User Story:** Como usuário, quero que as gravações sejam preservadas corretamente mesmo quando a nota é movida ou excluída, para que não haja dados órfãos ou perda acidental de arquivos.

#### Acceptance Criteria

1. WHEN uma nota é movida para a lixeira, THE GravacaoTelaService SHALL manter os registros de `GravacaoTela` associados sem excluí-los.
2. WHEN uma nota é excluída definitivamente da lixeira, THE GravacaoTelaService SHALL excluir todos os registros de `GravacaoTela` associados e seus respectivos arquivos físicos.
3. THE GravacaoTela SHALL ter uma restrição de chave estrangeira com `ON DELETE CASCADE` em relação à Nota, garantindo que registros órfãos não sejam criados no banco de dados.
