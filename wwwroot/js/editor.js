window.evenoteEditor = {
    _selecaoSalva: null,
    _editor: null,
    _dotNetRef: null,
    _debounceTimer: null,
    _noteId: null,

    setContent: function (element, content) {
        if (element) element.innerHTML = content || '';
    },

    getContent: function (element) {
        return element ? element.innerHTML : '';
    },

    format: function (command, value) {
        document.execCommand(command, false, value || null);
    },

    clearDebounce: function () {
        clearTimeout(this._debounceTimer);
        this._debounceTimer = null;
    },

    // Atualiza qual nota está sendo editada.
    // Chamado pelo C# sempre que a nota selecionada muda.
    setNoteId: function (noteId) {
        this._noteId = noteId;
    },

    // Lê e limpa o conteúdo pendente salvo no sessionStorage.
    getPendingContent: function () {
        try {
            var id = sessionStorage.getItem('evenote_pending_id');
            var content = sessionStorage.getItem('evenote_pending_content');
            sessionStorage.removeItem('evenote_pending_id');
            sessionStorage.removeItem('evenote_pending_content');
            if (id && content !== null) return { id: id, content: content };
        } catch (e) { }
        return null;
    },

    // Inicializa o editor e a toolbar.
    // Chamado uma única vez via OnAfterRenderAsync.
    init: function (editor, toolbar, dotNetRef) {
        if (!editor || editor._evenoteInit) return;
        editor._evenoteInit = true;

        this._editor = editor;
        this._dotNetRef = dotNetRef;

        const self = this;

        function salvarSelecao() {
            const sel = window.getSelection();
            if (sel && sel.rangeCount > 0) {
                const range = sel.getRangeAt(0);
                if (editor.contains(range.commonAncestorContainer)) {
                    self._selecaoSalva = range.cloneRange();
                }
            }
        }

        editor.addEventListener('mouseup', salvarSelecao);
        editor.addEventListener('keyup', salvarSelecao);

        toolbar.addEventListener('mousedown', function (e) {
            salvarSelecao();
            e.preventDefault();
        });

        // Quando o editor perde o foco (clicar na sidebar, mudar de aba, etc.):
        // 1. Grava no sessionStorage de forma SÍNCRONA — funciona mesmo durante navegação,
        //    porque não depende de SignalR ou round-trip ao servidor.
        // 2. Também tenta enviar ao servidor via DotNetObjectReference (se estiver disponível).
        editor.addEventListener('blur', function () {
            clearTimeout(self._debounceTimer);
            self._debounceTimer = null;
            if (self._noteId) {
                try {
                    sessionStorage.setItem('evenote_pending_id', self._noteId);
                    sessionStorage.setItem('evenote_pending_content', editor.innerHTML);
                } catch (e) { }
            }
            if (self._dotNetRef) {
                self._dotNetRef.invokeMethodAsync('ConteudoAlterado', editor.innerHTML)
                    .catch(function () { });
            }
        });

        // Debounce de 300ms enquanto o usuário digita — envia ao servidor e atualiza sessionStorage.
        editor.addEventListener('input', function () {
            clearTimeout(self._debounceTimer);
            self._debounceTimer = setTimeout(function () {
                if (self._noteId) {
                    try {
                        sessionStorage.setItem('evenote_pending_id', self._noteId);
                        sessionStorage.setItem('evenote_pending_content', editor.innerHTML);
                    } catch (e) { }
                }
                if (self._dotNetRef) {
                    self._dotNetRef.invokeMethodAsync('ConteudoAlterado', editor.innerHTML)
                        .catch(function () { });
                }
            }, 300);
        });
    },

    // Insere HTML arbitrário na posição do cursor
    insertHtml: function (html) {
        try {
            if (this._selecaoSalva) {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this._selecaoSalva);
            }
            document.execCommand('insertHTML', false, html);
        } catch (e) {
            console.warn('evenoteEditor.insertHtml:', e);
        } finally {
            this._selecaoSalva = null;
        }
    },

    // Restaura a seleção salva e aplica a cor

    applyColor: function (command, color) {
        try {
            if (this._selecaoSalva) {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this._selecaoSalva);
            }
            if (color === 'transparent') {
                document.execCommand('hiliteColor', false, 'transparent');
                document.execCommand('backColor', false, 'transparent');
            } else {
                document.execCommand(command, false, color);
            }
        } catch (e) {
            console.warn('evenoteEditor.applyColor:', e);
        } finally {
            this._selecaoSalva = null;
        }
    }
};

window.evenoteArquivos = {
    download: function (url, filename) {
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    }
};

window.evenoteHome = {
    loadRascunho: function () {
        try { return localStorage.getItem('evenote_rascunho') || ''; } catch (e) { return ''; }
    },
    saveRascunho: function (text) {
        try { localStorage.setItem('evenote_rascunho', text); } catch (e) { }
    }
};

window.evenoteCalendario = {
    scrollTo: function (element, top) {
        if (element) {
            element.scrollTop = top;
        }
    }
};
