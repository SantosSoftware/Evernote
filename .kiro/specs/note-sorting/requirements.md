# Documento de Requisitos

## Introdução

Esta funcionalidade adiciona ordenação de notas à página de Notas do aplicativo Evenote. Atualmente, as notas são sempre exibidas em ordem decrescente de data de atualização. Com esta feature, o usuário poderá escolher o critério de ordenação (data de criação ou título) e a direção (crescente ou decrescente), por meio de um botão de ordenação posicionado ao lado do badge de contagem de notas no cabeçalho do painel esquerdo.

## Glossário

- **Painel_Lista**: O painel esquerdo da página de Notas que exibe a lista de cartões de notas.
- **Cabeçalho_Lista**: A área superior do Painel_Lista que contém o título, o badge de contagem e os botões de ação.
- **Badge_Contagem**: O elemento visual que exibe o número total de notas visíveis na lista.
- **Botão_Ordenação**: O botão de ícone posicionado ao lado do Badge_Contagem que abre o menu de opções de ordenação.
- **Menu_Ordenação**: O painel flutuante exibido ao clicar no Botão_Ordenação, contendo as opções de critério e direção de ordenação.
- **Critério_Ordenação**: O campo pelo qual as notas são ordenadas — `DataCriacao` (data de criação) ou `Titulo` (título alfabético).
- **Direção_Ordenação**: A direção da ordenação — `Crescente` ou `Decrescente`.
- **Nota**: Entidade persistida no banco de dados com campos `Titulo`, `CriadoEm` e `AtualizadoEm`.
- **NotaService**: Serviço C# responsável por fornecer e persistir as notas.
- **Notas_Page**: O componente Blazor `Notas.razor` que renderiza a página de Notas.

---

## Requisitos

### Requisito 1: Exibição do Botão de Ordenação

**User Story:** Como usuário, quero ver um botão de ordenação ao lado do badge de contagem de notas, para que eu possa acessar as opções de ordenação de forma rápida e intuitiva.

#### Critérios de Aceitação

1. THE Notas_Page SHALL exibir o Botão_Ordenação no Cabeçalho_Lista, posicionado entre o Badge_Contagem e o botão de nova nota.
2. THE Botão_Ordenação SHALL exibir um ícone de ordenação (setas para cima e para baixo) que comunica visualmente sua função.
3. WHILE o Menu_Ordenação estiver fechado, THE Botão_Ordenação SHALL exibir o estado padrão sem indicação visual de seleção ativa.
4. WHILE uma ordenação diferente da padrão estiver ativa, THE Botão_Ordenação SHALL exibir um indicador visual (destaque de cor) para sinalizar que a ordenação foi personalizada.

---

### Requisito 2: Abertura e Fechamento do Menu de Ordenação

**User Story:** Como usuário, quero abrir e fechar o menu de ordenação de forma fluida, para que eu possa visualizar e selecionar as opções sem interromper meu fluxo de trabalho.

#### Critérios de Aceitação

1. WHEN o usuário clica no Botão_Ordenação, THE Menu_Ordenação SHALL ser exibido como um painel flutuante abaixo do botão.
2. WHEN o usuário clica fora do Menu_Ordenação, THE Notas_Page SHALL fechar o Menu_Ordenação.
3. WHEN o usuário clica novamente no Botão_Ordenação enquanto o Menu_Ordenação está aberto, THE Notas_Page SHALL fechar o Menu_Ordenação.
4. WHEN o Menu_Ordenação é aberto, THE Notas_Page SHALL fechar todos os outros menus abertos (Inserir, Cor da Fonte, Cor de Fundo, Menu de Ações).
5. WHEN o usuário seleciona uma opção no Menu_Ordenação, THE Notas_Page SHALL fechar o Menu_Ordenação automaticamente.

---

### Requisito 3: Opções de Critério de Ordenação

**User Story:** Como usuário, quero escolher entre ordenar por data de criação ou por título, para que eu possa encontrar minhas notas da forma que melhor se adapta ao meu contexto.

#### Critérios de Aceitação

1. THE Menu_Ordenação SHALL exibir a opção "Data de criação" como critério de ordenação.
2. THE Menu_Ordenação SHALL exibir a opção "Título" como critério de ordenação.
3. WHEN o usuário seleciona "Data de criação", THE Notas_Page SHALL ordenar a lista de notas pelo campo `CriadoEm` da Nota.
4. WHEN o usuário seleciona "Título", THE Notas_Page SHALL ordenar a lista de notas pelo campo `Titulo` da Nota, utilizando comparação sem distinção de maiúsculas e minúsculas.
5. THE Menu_Ordenação SHALL exibir um indicador visual (marca de seleção ou destaque) na opção de critério atualmente ativa.

---

### Requisito 4: Opções de Direção de Ordenação

**User Story:** Como usuário, quero escolher entre ordenação crescente e decrescente para cada critério, para que eu possa visualizar as notas na ordem que preferir.

#### Critérios de Aceitação

1. THE Menu_Ordenação SHALL exibir as opções "Crescente" e "Decrescente" para a direção de ordenação.
2. WHEN o usuário seleciona "Crescente" com critério "Data de criação", THE Notas_Page SHALL ordenar as notas da mais antiga para a mais recente pelo campo `CriadoEm`.
3. WHEN o usuário seleciona "Decrescente" com critério "Data de criação", THE Notas_Page SHALL ordenar as notas da mais recente para a mais antiga pelo campo `CriadoEm`.
4. WHEN o usuário seleciona "Crescente" com critério "Título", THE Notas_Page SHALL ordenar as notas em ordem alfabética de A a Z pelo campo `Titulo`.
5. WHEN o usuário seleciona "Decrescente" com critério "Título", THE Notas_Page SHALL ordenar as notas em ordem alfabética de Z a A pelo campo `Titulo`.
6. THE Menu_Ordenação SHALL exibir um indicador visual (marca de seleção ou destaque) na opção de direção atualmente ativa.

---

### Requisito 5: Aplicação Imediata da Ordenação

**User Story:** Como usuário, quero que a lista de notas seja reordenada imediatamente ao selecionar uma opção, para que eu veja o resultado da minha escolha sem precisar confirmar ou recarregar a página.

#### Critérios de Aceitação

1. WHEN o usuário altera o Critério_Ordenação ou a Direção_Ordenação, THE Notas_Page SHALL reordenar a lista `_notasOrdenadas` imediatamente, sem recarregar dados do banco de dados.
2. WHEN a lista é reordenada, THE Notas_Page SHALL manter a nota atualmente selecionada como selecionada, sem alterar o conteúdo exibido no editor.
3. WHEN a lista é reordenada, THE Notas_Page SHALL atualizar a renderização do Painel_Lista para refletir a nova ordem.

---

### Requisito 6: Persistência da Preferência de Ordenação por Sessão

**User Story:** Como usuário, quero que minha preferência de ordenação seja mantida enquanto navego entre cadernos na mesma sessão, para que eu não precise reconfigurar a ordenação a cada troca de caderno.

#### Critérios de Aceitação

1. WHILE o usuário navega entre cadernos diferentes na mesma sessão, THE Notas_Page SHALL manter o Critério_Ordenação e a Direção_Ordenação selecionados pelo usuário.
2. WHEN a página de Notas é carregada pela primeira vez em uma sessão, THE Notas_Page SHALL aplicar a ordenação padrão de Critério_Ordenação `DataCriacao` com Direção_Ordenação `Decrescente`.

---

### Requisito 7: Campo CriadoEm na Entidade Nota

**User Story:** Como desenvolvedor, quero que a entidade Nota possua um campo de data de criação, para que a ordenação por data de criação seja possível com dados precisos.

#### Critérios de Aceitação

1. THE NotaService SHALL garantir que toda Nota criada tenha o campo `CriadoEm` preenchido com a data e hora do momento da criação.
2. IF uma Nota existente no banco de dados não possuir valor no campo `CriadoEm`, THEN THE Notas_Page SHALL utilizar o valor de `AtualizadoEm` como substituto para fins de ordenação.
3. THE NotaService SHALL preservar o valor original de `CriadoEm` ao salvar alterações em uma Nota existente.
