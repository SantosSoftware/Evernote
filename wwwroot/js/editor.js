window.evenoteEditor = {
    _selecaoSalva: null,

    setContent: function (element, content) {
        if (element) element.innerHTML = content || '';
    },

    getContent: function (element) {
        return element ? element.innerHTML : '';
    },

    format: function (command, value) {
        document.execCommand(command, false, value || null);
    },

    // Inicializa o editor e a toolbar.
    // Chamado uma única vez via OnAfterRenderAsync.
    init: function (editor, toolbar) {
        if (!editor || editor._evenoteInit) return;
        editor._evenoteInit = true;

        const self = this;

        // Salva a seleção toda vez que o usuário para de interagir com o editor.
        // Isso garante que sempre teremos a última seleção disponível.
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

        // No mousedown da toolbar:
        // 1. Salva a seleção ANTES de qualquer coisa (síncrono, no browser)
        // 2. Chama preventDefault() para impedir que o editor perca o foco
        toolbar.addEventListener('mousedown', function (e) {
            salvarSelecao();
            e.preventDefault();
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
