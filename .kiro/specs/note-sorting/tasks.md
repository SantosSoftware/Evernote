# Plano de Implementação: Ordenação de Notas

## Visão Geral

Implementação incremental da feature de ordenação configurável da lista de notas em `Notas.razor`. As tarefas seguem a ordem: modelo de dados → migration → lógica de ordenação → UI → testes.

## Tarefas

- [x] 1. Adicionar campo `CriadoEm` à entidade `Nota` e criar migration
  - Abrir o arquivo do modelo `Nota` (em `Services/NotaService.cs` ou arquivo dedicado) e adicionar a propriedade `public DateTime? CriadoEm { get; set; }` como campo nullable
  - Executar `dotnet ef migrations add AddCriadoEmNota` para gerar a migration que adiciona a coluna `CriadoEm TEXT NULL` à tabela `Notas`
  - Verificar o arquivo de migration gerado para confirmar que apenas a coluna `CriadoEm` é adicionada, sem alterar outros campos
  - _Requisitos: 7.1, 7.2, 7.3_

- [x] 2. Atualizar `NotaService.Adicionar` para preencher `CriadoEm`
  - [x] 2.1 Modificar o método `Adicionar` em `NotaService` para atribuir `nota.CriadoEm ??= DateTime.Now` antes de adicionar ao contexto
    - Garantir que `SalvarAlteracoes` não toque em `CriadoEm` (o EF Core rastreia apenas campos modificados)
    - _Requisitos: 7.1, 7.3_

  - [ ]* 2.2 Escrever teste de propriedade para `CriadoEm` preenchido e preservado
    - **Propriedade 5: CriadoEm é preenchido na criação e preservado em saves**
    - Usar FsCheck.Xunit; gerar nota com campos aleatórios; verificar que `CriadoEm != null` após `Adicionar` e que o valor não muda após N chamadas a `SalvarAlteracoes`
    - **Valida: Requisitos 7.1, 7.3**

- [x] 3. Adicionar enums e estado de ordenação ao componente `Notas.razor`
  - Declarar `public enum CriterioOrdenacao { DataCriacao, Titulo }` e `public enum DirecaoOrdenacao { Crescente, Decrescente }` (em `Services/NotaService.cs` ou arquivo de enums dedicado)
  - Adicionar ao bloco `@code` de `Notas.razor` os campos de estado:
    - `private CriterioOrdenacao _criterioOrdenacao = CriterioOrdenacao.DataCriacao;`
    - `private DirecaoOrdenacao _direcaoOrdenacao = DirecaoOrdenacao.Decrescente;`
    - `private bool _mostrarMenuOrdenacao = false;`
  - Adicionar a propriedade auxiliar `private bool OrdenacaoPersonalizada => _criterioOrdenacao != CriterioOrdenacao.DataCriacao || _direcaoOrdenacao != DirecaoOrdenacao.Decrescente;`
  - _Requisitos: 1.3, 1.4, 6.2_

- [x] 4. Implementar o método `AplicarOrdenacao` e integrá-lo ao carregamento da lista
  - [x] 4.1 Criar o método `AplicarOrdenacao()` em `Notas.razor` conforme o design:
    - Critério `Titulo`: `OrderBy`/`OrderByDescending` com `StringComparer.CurrentCultureIgnoreCase`
    - Critério `DataCriacao` (default): `OrderBy`/`OrderByDescending` por `n.CriadoEm ?? n.AtualizadoEm`
    - Atribuir o resultado a `_notasOrdenadas = query.ToList()`
    - _Requisitos: 3.3, 3.4, 4.2, 4.3, 4.4, 4.5, 5.1_

  - [ ]* 4.2 Escrever teste de propriedade P1 — Ordenação por DataCriacao
    - **Propriedade 1: Ordenação por DataCriacao preserva todos os elementos e respeita a direção**
    - Gerar lista arbitrária de notas com `CriadoEm` variados (incluindo nulls); verificar que a lista resultante tem os mesmos elementos e está ordenada corretamente por `CriadoEm ?? AtualizadoEm`
    - **Valida: Requisitos 3.3, 4.2, 4.3, 5.1**

  - [ ]* 4.3 Escrever teste de propriedade P2 — Ordenação por Titulo
    - **Propriedade 2: Ordenação por Titulo preserva todos os elementos e respeita a direção (case-insensitive)**
    - Gerar lista arbitrária de notas com títulos variados (maiúsculas/minúsculas misturadas); verificar que a lista resultante tem os mesmos elementos e está ordenada alfabeticamente sem distinção de caso
    - **Valida: Requisitos 3.4, 4.4, 4.5, 5.1**

  - [x] 4.4 Substituir o `OrderByDescending(n => n.AtualizadoEm)` existente em `OnParametersSetAsync` pela chamada a `AplicarOrdenacao()`, garantindo que a ordenação padrão seja aplicada no carregamento inicial e ao trocar de caderno
    - _Requisitos: 5.1, 5.3, 6.1, 6.2_

  - [ ]* 4.5 Escrever teste de propriedade P3 — Reordenação preserva a nota selecionada
    - **Propriedade 3: Reordenação preserva a nota selecionada**
    - Gerar lista de notas + índice aleatório para `notaSelecionada` + par `(CriterioOrdenacao, DirecaoOrdenacao)` aleatório; verificar que a referência `notaSelecionada` aponta para o mesmo objeto após `AplicarOrdenacao`
    - **Valida: Requisito 5.2**

  - [ ]* 4.6 Escrever teste de propriedade P4 — Preferência mantida ao trocar de caderno
    - **Propriedade 4: Preferência de ordenação é mantida ao trocar de caderno**
    - Simular sequência de trocas de `CadernoId` (GUIDs aleatórios) após definir `_criterioOrdenacao` e `_direcaoOrdenacao`; verificar que os valores permanecem inalterados após cada troca
    - **Valida: Requisito 6.1**

- [x] 5. Implementar os métodos de controle do menu de ordenação
  - [x] 5.1 Criar `ToggleMenuOrdenacao()` que alterna `_mostrarMenuOrdenacao` e fecha todos os outros menus (`_mostrarInserir`, `_mostrarCorFonte`, `_mostrarCorFundo`, `_mostrarMenuAcoes`)
    - _Requisitos: 2.1, 2.3, 2.4_

  - [ ]* 5.2 Escrever teste de propriedade P7 — Abrir menu fecha outros
    - **Propriedade 7: Abrir Menu_Ordenação fecha todos os outros menus**
    - Gerar combinação booleana arbitrária dos 4 outros menus; chamar `ToggleMenuOrdenacao` para abrir; verificar que todos os outros campos de visibilidade são `false`
    - **Valida: Requisito 2.4**

  - [x] 5.3 Criar `SelecionarCriterio(CriterioOrdenacao criterio)` que atualiza `_criterioOrdenacao`, chama `AplicarOrdenacao()` e define `_mostrarMenuOrdenacao = false`
    - _Requisitos: 3.3, 3.4, 2.5, 5.1_

  - [x] 5.4 Criar `SelecionarDirecao(DirecaoOrdenacao direcao)` que atualiza `_direcaoOrdenacao`, chama `AplicarOrdenacao()` e define `_mostrarMenuOrdenacao = false`
    - _Requisitos: 4.2, 4.3, 4.4, 4.5, 2.5, 5.1_

  - [ ]* 5.5 Escrever teste de propriedade P8 — Selecionar opção fecha menu
    - **Propriedade 8: Selecionar qualquer opção fecha o Menu_Ordenação**
    - Gerar `CriterioOrdenacao` ou `DirecaoOrdenacao` aleatório; chamar `SelecionarCriterio` ou `SelecionarDirecao`; verificar que `_mostrarMenuOrdenacao == false`
    - **Valida: Requisito 2.5**

  - [x] 5.6 Atualizar `FecharMenus()` para incluir `_mostrarMenuOrdenacao = false`
    - _Requisitos: 2.2_

- [x] 6. Checkpoint — Verificar lógica antes de construir a UI
  - Garantir que todos os testes passam, ask the user if questions arise.

- [x] 7. Adicionar o `Botão_Ordenação` e o `Menu_Ordenação` ao markup de `Notas.razor`
  - [x] 7.1 Inserir o `Botão_Ordenação` no `Cabeçalho_Lista` (`.lista-header`), posicionado entre o `Badge_Contagem` e o `btn-nova-nota`:
    - Botão com ícone SVG de setas para cima/baixo
    - Aplicar classe CSS de destaque quando `OrdenacaoPersonalizada == true` (Requisito 1.4)
    - Evento `@onclick` chama `ToggleMenuOrdenacao` com `@onclick:stopPropagation="true"`
    - _Requisitos: 1.1, 1.2, 1.3, 1.4_

  - [x] 7.2 Adicionar o `Menu_Ordenação` como painel flutuante dentro de um `color-picker-wrapper` (padrão já usado no projeto):
    - Seção de critério com botões "Data de criação" e "Título", cada um chamando `SelecionarCriterio`
    - Seção de direção com botões "Crescente" e "Decrescente", cada um chamando `SelecionarDirecao`
    - Indicador visual (marca de seleção ou classe CSS de destaque) na opção atualmente ativa para critério e direção
    - Renderizado condicionalmente com `@if (_mostrarMenuOrdenacao)`
    - _Requisitos: 2.1, 3.1, 3.2, 3.5, 4.1, 4.6_

  - [ ]* 7.3 Escrever teste de propriedade P6 — Indicador visual de ordenação personalizada
    - **Propriedade 6: Indicador visual de ordenação personalizada é consistente com o estado**
    - Gerar par `(CriterioOrdenacao, DirecaoOrdenacao)` aleatório; verificar que `OrdenacaoPersonalizada` retorna `true` se e somente se o par for diferente de `(DataCriacao, Decrescente)`
    - **Valida: Requisito 1.4**

- [x] 8. Adicionar estilos CSS para o `Botão_Ordenação` e o `Menu_Ordenação` em `Notas.razor.css`
  - Estilo para `.btn-ordenacao` (estado normal e hover), seguindo o padrão visual de `.btn-nova-nota`
  - Estilo para `.btn-ordenacao-ativo` (destaque de cor quando `OrdenacaoPersonalizada == true`)
  - Estilo para `.menu-ordenacao-dropdown` (painel flutuante), seguindo o padrão de `.menu-acoes-dropdown`
  - Estilos para itens do menu (`.menu-ordenacao-item`, `.menu-ordenacao-item-ativo`) com indicador visual de seleção
  - _Requisitos: 1.2, 1.4, 3.5, 4.6_

- [x] 9. Checkpoint final — Garantir que todos os testes passam
  - Garantir que todos os testes passam, ask the user if questions arise.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- Os testes de propriedade usam **FsCheck.Xunit** (integrado com xUnit), com mínimo de 100 iterações por propriedade
- A ordenação é puramente em memória — nenhuma consulta adicional ao banco é necessária
- O campo `CriadoEm` é nullable para compatibilidade com notas legadas; o fallback para `AtualizadoEm` é tratado em código
