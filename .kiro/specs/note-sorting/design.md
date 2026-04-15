# Documento de Design — Ordenação de Notas

## Visão Geral

Esta feature adiciona ordenação configurável à lista de notas da página `Notas.razor`. O usuário poderá escolher o critério (`DataCriacao` ou `Titulo`) e a direção (`Crescente` ou `Decrescente`) por meio de um `Menu_Ordenação` flutuante acionado por um `Botão_Ordenação` posicionado no `Cabeçalho_Lista`, entre o `Badge_Contagem` e o botão de nova nota.

A ordenação é aplicada **em memória** sobre a lista `_notasOrdenadas` já carregada — sem nova consulta ao banco. A preferência é mantida por toda a sessão (instância do componente Blazor Server), mas não é persistida entre sessões.

Para suportar ordenação por data de criação, a entidade `Nota` precisará de um novo campo `CriadoEm`, adicionado via migration do EF Core.

---

## Arquitetura

O projeto é um **Blazor Server (.NET 10)** com SQLite e Entity Framework Core. A arquitetura relevante para esta feature é:

```
┌─────────────────────────────────────────────────────────┐
│  Notas.razor  (componente Blazor Server, Scoped)        │
│                                                         │
│  Estado de ordenação:                                   │
│    _criterioOrdenacao : CriterioOrdenacao               │
│    _direcaoOrdenacao  : DirecaoOrdenacao                │
│    _mostrarMenuOrdenacao : bool                         │
│                                                         │
│  Lista em memória:                                      │
│    _notasOrdenadas : List<Nota>   ←── reordenada        │
│                                       in-place          │
└────────────────┬────────────────────────────────────────┘
                 │ lê / salva
                 ▼
┌─────────────────────────────────────────────────────────┐
│  NotaService  (Scoped)                                  │
│    NotasAtivas → List<Nota>                             │
│    Adicionar / SalvarAlteracoes                         │
└────────────────┬────────────────────────────────────────┘
                 │ EF Core
                 ▼
┌─────────────────────────────────────────────────────────┐
│  AppDbContext → SQLite (evenote.db)                     │
│    DbSet<Nota>  — novo campo: CriadoEm                  │
└─────────────────────────────────────────────────────────┘
```

Não há camada de serviço nova: a lógica de ordenação vive inteiramente no componente `Notas.razor`, pois é estado de apresentação puro.

---

## Componentes e Interfaces

### Enums (novos, em `Services/NotaService.cs` ou arquivo dedicado)

```csharp
public enum CriterioOrdenacao { DataCriacao, Titulo }
public enum DirecaoOrdenacao  { Crescente, Decrescente }
```

### Alterações em `Nota` (modelo)

Adição do campo `CriadoEm`:

```csharp
public class Nota
{
    // ... campos existentes ...
    public DateTime? CriadoEm { get; set; }   // nullable para compatibilidade com dados legados
}
```

O campo é `nullable` para que notas existentes no banco (sem valor) não quebrem. A lógica de ordenação usa `AtualizadoEm` como fallback quando `CriadoEm` é `null` (Requisito 7.2).

### Alterações em `NotaService.Adicionar`

```csharp
public void Adicionar(Nota nota)
{
    nota.CriadoEm ??= DateTime.Now;   // garante preenchimento na criação
    _db.Notas.Add(nota);
    _db.SaveChanges();
}
```

`SalvarAlteracoes` não toca em `CriadoEm` — o EF Core só persiste campos que foram modificados no objeto rastreado, então o valor original é preservado automaticamente (Requisito 7.3).

### Alterações em `Notas.razor` — estado

```csharp
// Ordenação
private CriterioOrdenacao _criterioOrdenacao = CriterioOrdenacao.DataCriacao;
private DirecaoOrdenacao  _direcaoOrdenacao  = DirecaoOrdenacao.Decrescente;
private bool _mostrarMenuOrdenacao = false;

// Propriedade auxiliar
private bool OrdenacaoPersonalizada =>
    _criterioOrdenacao != CriterioOrdenacao.DataCriacao ||
    _direcaoOrdenacao  != DirecaoOrdenacao.Decrescente;
```

### Método `AplicarOrdenacao` (novo, em `Notas.razor`)

```csharp
private void AplicarOrdenacao()
{
    IEnumerable<Nota> query = _criterioOrdenacao switch
    {
        CriterioOrdenacao.Titulo =>
            _direcaoOrdenacao == DirecaoOrdenacao.Crescente
                ? _notasOrdenadas.OrderBy(n => n.Titulo,
                      StringComparer.CurrentCultureIgnoreCase)
                : _notasOrdenadas.OrderByDescending(n => n.Titulo,
                      StringComparer.CurrentCultureIgnoreCase),

        _ => // DataCriacao (default)
            _direcaoOrdenacao == DirecaoOrdenacao.Crescente
                ? _notasOrdenadas.OrderBy(n => n.CriadoEm ?? n.AtualizadoEm)
                : _notasOrdenadas.OrderByDescending(n => n.CriadoEm ?? n.AtualizadoEm),
    };

    _notasOrdenadas = query.ToList();
}
```

Este método é chamado sempre que `_criterioOrdenacao` ou `_direcaoOrdenacao` mudam, e também após o carregamento inicial da lista (substituindo o `OrderByDescending(n => n.AtualizadoEm)` atual).

### Método `ToggleMenuOrdenacao` (novo)

```csharp
private void ToggleMenuOrdenacao()
{
    _mostrarMenuOrdenacao = !_mostrarMenuOrdenacao;
    // Fecha todos os outros menus (Requisito 2.4)
    _mostrarInserir   = false;
    _mostrarCorFonte  = false;
    _mostrarCorFundo  = false;
    _mostrarMenuAcoes = false;
}
```

### Método `SelecionarCriterio` / `SelecionarDirecao` (novos)

```csharp
private void SelecionarCriterio(CriterioOrdenacao criterio)
{
    _criterioOrdenacao = criterio;
    AplicarOrdenacao();
    _mostrarMenuOrdenacao = false;   // Requisito 2.5
}

private void SelecionarDirecao(DirecaoOrdenacao direcao)
{
    _direcaoOrdenacao = direcao;
    AplicarOrdenacao();
    _mostrarMenuOrdenacao = false;   // Requisito 2.5
}
```

### Atualização de `FecharMenus`

```csharp
private void FecharMenus()
{
    _mostrarInserir       = false;
    _mostrarMenuAcoes     = false;
    _mostrarMenuOrdenacao = false;   // inclui o novo menu
}
```

### Migration EF Core (nova)

Um novo arquivo de migration adicionará a coluna `CriadoEm` à tabela `Notas`:

```sql
ALTER TABLE "Notas" ADD COLUMN "CriadoEm" TEXT NULL;
```

O valor padrão no banco será `NULL` para registros existentes — o fallback para `AtualizadoEm` é tratado em código (Requisito 7.2).

---

## Modelos de Dados

### Entidade `Nota` — campo adicionado

| Campo       | Tipo        | Nullable | Descrição                                      |
|-------------|-------------|----------|------------------------------------------------|
| `CriadoEm`  | `DateTime?` | Sim      | Data/hora de criação. Null em notas legadas.   |

Todos os outros campos permanecem inalterados.

### Estado de ordenação no componente

| Campo                    | Tipo                 | Valor padrão              |
|--------------------------|----------------------|---------------------------|
| `_criterioOrdenacao`     | `CriterioOrdenacao`  | `DataCriacao`             |
| `_direcaoOrdenacao`      | `DirecaoOrdenacao`   | `Decrescente`             |
| `_mostrarMenuOrdenacao`  | `bool`               | `false`                   |

O estado de ordenação **não é persistido** — vive apenas na instância do componente (por sessão/aba).

---

## Propriedades de Correção

*Uma propriedade é uma característica ou comportamento que deve ser verdadeiro em todas as execuções válidas de um sistema — essencialmente, uma declaração formal sobre o que o sistema deve fazer. Propriedades servem como ponte entre especificações legíveis por humanos e garantias de correção verificáveis por máquina.*

### Propriedade 1: Ordenação por DataCriacao preserva todos os elementos e respeita a direção

*Para qualquer* lista de notas com valores de `CriadoEm` variados e qualquer direção de ordenação, após aplicar `AplicarOrdenacao` com critério `DataCriacao`, a lista resultante deve conter exatamente os mesmos elementos e estar ordenada corretamente por `CriadoEm ?? AtualizadoEm` na direção especificada.

**Valida: Requisitos 3.3, 4.2, 4.3, 5.1**

---

### Propriedade 2: Ordenação por Titulo preserva todos os elementos e respeita a direção (case-insensitive)

*Para qualquer* lista de notas com títulos variados (incluindo misturas de maiúsculas e minúsculas) e qualquer direção de ordenação, após aplicar `AplicarOrdenacao` com critério `Titulo`, a lista resultante deve conter exatamente os mesmos elementos e estar ordenada alfabeticamente sem distinção de maiúsculas/minúsculas na direção especificada.

**Valida: Requisitos 3.4, 4.4, 4.5, 5.1**

---

### Propriedade 3: Reordenação preserva a nota selecionada

*Para qualquer* nota selecionada em uma lista e qualquer combinação de critério e direção de ordenação, após aplicar `AplicarOrdenacao`, a referência `notaSelecionada` deve apontar para o mesmo objeto `Nota` que apontava antes da reordenação.

**Valida: Requisito 5.2**

---

### Propriedade 4: Preferência de ordenação é mantida ao trocar de caderno

*Para qualquer* sequência de trocas de caderno dentro da mesma sessão, os valores de `_criterioOrdenacao` e `_direcaoOrdenacao` definidos pelo usuário devem permanecer inalterados após cada troca.

**Valida: Requisito 6.1**

---

### Propriedade 5: CriadoEm é preenchido na criação e preservado em saves

*Para qualquer* nota criada via `NotaService.Adicionar`, o campo `CriadoEm` deve ser não-nulo após a criação; e *para qualquer* número de chamadas subsequentes a `SalvarAlteracoes` sobre essa nota, o valor de `CriadoEm` deve permanecer igual ao valor definido no momento da criação.

**Valida: Requisitos 7.1, 7.3**

---

### Propriedade 6: Indicador visual de ordenação personalizada é consistente com o estado

*Para qualquer* par `(criterio, direcao)`, a propriedade `OrdenacaoPersonalizada` deve retornar `true` se e somente se o par for diferente de `(DataCriacao, Decrescente)`.

**Valida: Requisito 1.4**

---

### Propriedade 7: Abrir Menu_Ordenação fecha todos os outros menus

*Para qualquer* combinação de estados dos outros menus (`_mostrarInserir`, `_mostrarCorFonte`, `_mostrarCorFundo`, `_mostrarMenuAcoes`), após chamar `ToggleMenuOrdenacao` para abrir o menu, todos os outros campos de visibilidade de menu devem ser `false`.

**Valida: Requisito 2.4**

---

### Propriedade 8: Selecionar qualquer opção fecha o Menu_Ordenação

*Para qualquer* critério ou direção válidos passados a `SelecionarCriterio` ou `SelecionarDirecao`, após a chamada, `_mostrarMenuOrdenacao` deve ser `false`.

**Valida: Requisito 2.5**

---

## Tratamento de Erros

### Dados legados sem `CriadoEm`

Notas criadas antes desta feature terão `CriadoEm = null`. O operador `??` no método `AplicarOrdenacao` garante o fallback para `AtualizadoEm` de forma transparente, sem exceções.

### Falha na migration

Se a migration falhar (ex.: banco bloqueado), o aplicativo não iniciará — comportamento padrão do EF Core com `MigrateAsync` no startup. Não há tratamento especial necessário além do já existente.

### Lista vazia

`AplicarOrdenacao` sobre uma lista vazia retorna uma lista vazia — `OrderBy`/`OrderByDescending` sobre coleção vazia é seguro em LINQ.

### Nota selecionada removida durante reordenação

Não aplicável: a reordenação não remove elementos, apenas reordena. A referência `notaSelecionada` permanece válida.

---

## Estratégia de Testes

### Abordagem dual

- **Testes de unidade/exemplo**: verificam comportamentos específicos, casos de borda e inicialização.
- **Testes de propriedade**: verificam invariantes universais sobre a lógica de ordenação e estado.

### Biblioteca de testes de propriedade

Para .NET, usar **[FsCheck](https://fscheck.github.io/FsCheck/)** (versão `2.x` ou `3.x`) integrado com xUnit via `FsCheck.Xunit`. Cada teste de propriedade deve rodar no mínimo **100 iterações**.

Tag de referência para cada teste de propriedade:
```
// Feature: note-sorting, Property N: <texto da propriedade>
```

### Testes de unidade (exemplos)

| Cenário | O que verificar |
|---|---|
| Inicialização da página | `_criterioOrdenacao == DataCriacao` e `_direcaoOrdenacao == Decrescente` |
| Nota sem `CriadoEm` | Ordenação usa `AtualizadoEm` como fallback sem exceção |
| Menu abre ao clicar no botão | `_mostrarMenuOrdenacao == true` após `ToggleMenuOrdenacao` |
| Menu fecha ao clicar fora | `_mostrarMenuOrdenacao == false` após `FecharMenus` |
| Opções do menu presentes | "Data de criação" e "Título" e "Crescente" e "Decrescente" existem no markup |
| Valor padrão não exibe indicador | `OrdenacaoPersonalizada == false` com valores padrão |

### Testes de propriedade

Cada propriedade do documento deve ser implementada como um único teste de propriedade:

| Propriedade | Geradores necessários |
|---|---|
| P1 — Ordenação por DataCriacao | `Gen.listOf(Arb.Default.Guid, DateTime, DateTime)` para notas com CriadoEm variados |
| P2 — Ordenação por Titulo | `Gen.listOf` com títulos gerados por `Arb.Default.NonEmptyString` |
| P3 — Preserva nota selecionada | Lista de notas + índice aleatório para nota selecionada + par (critério, direção) |
| P4 — Preferência mantida ao trocar caderno | Sequência de GUIDs de cadernos + par (critério, direção) definido antes das trocas |
| P5 — CriadoEm preenchido e preservado | Nota com campos aleatórios; verificar após Adicionar e após N saves |
| P6 — OrdenacaoPersonalizada consistente | Par (CriterioOrdenacao, DirecaoOrdenacao) gerado por enum arbitrário |
| P7 — Abrir menu fecha outros | Combinação booleana dos 4 outros menus |
| P8 — Selecionar opção fecha menu | CriterioOrdenacao ou DirecaoOrdenacao aleatório |

### Testes de integração / smoke

- Verificar que a migration é aplicada corretamente e a coluna `CriadoEm` existe na tabela `Notas`.
- Verificar que notas criadas via UI têm `CriadoEm` persistido no banco.
