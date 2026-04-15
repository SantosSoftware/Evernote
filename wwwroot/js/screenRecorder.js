window.screenRecorder = (() => {
    let mediaRecorder = null;
    let stream = null;
    let chunks = [];
    let dotNetRef = null;
    let timerInterval = null;
    let segundos = 0;
    let notaId = null;

    function pararTimer() {
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }
    }

    function pararStream() {
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
            stream = null;
        }
    }

    function resetar() {
        pararTimer();
        pararStream();
        mediaRecorder = null;
        chunks = [];
        segundos = 0;
        notaId = null;
    }

    async function invocar(metodo, ...args) {
        try {
            if (dotNetRef) await dotNetRef.invokeMethodAsync(metodo, ...args);
        } catch (e) {
            console.error(`screenRecorder: erro ao invocar ${metodo}`, e);
        }
    }

    return {
        verificarSuporteAsync: async () => {
            return !!(navigator.mediaDevices && navigator.mediaDevices.getDisplayMedia);
        },

        iniciarAsync: async (ref, nId) => {
            dotNetRef = ref;
            notaId = nId;
            try {
                stream = await navigator.mediaDevices.getDisplayMedia({
                    video: { cursor: 'always' },
                    audio: true
                });

                chunks = [];
                segundos = 0;

                const mimeType = MediaRecorder.isTypeSupported('video/webm;codecs=vp9')
                    ? 'video/webm;codecs=vp9'
                    : 'video/webm';

                mediaRecorder = new MediaRecorder(stream, { mimeType });

                mediaRecorder.ondataavailable = e => {
                    if (e.data && e.data.size > 0) chunks.push(e.data);
                };

                mediaRecorder.onerror = async e => {
                    resetar();
                    await invocar('GravacaoErro', `Erro na gravação: ${e.error?.message ?? 'desconhecido'}`);
                };

                // Stream encerrada externamente (usuário clicou "Parar compartilhamento")
                stream.oninactive = async () => {
                    if (mediaRecorder && mediaRecorder.state !== 'inactive') {
                        await window.screenRecorder.pararAsync();
                    }
                };

                mediaRecorder.start(1000); // chunk a cada 1s

                timerInterval = setInterval(async () => {
                    segundos++;
                    await invocar('TickCronometro', segundos);
                }, 1000);

                await invocar('GravacaoIniciada');
            } catch (e) {
                resetar();
                const msg = e.name === 'NotAllowedError'
                    ? 'Permissão de captura de tela negada.'
                    : `Erro ao iniciar gravação: ${e.message}`;
                await invocar('GravacaoErro', msg);
            }
        },

        pausar: () => {
            if (mediaRecorder && mediaRecorder.state === 'recording') {
                mediaRecorder.pause();
                pararTimer();
                invocar('GravacaoPausada');
            }
        },

        retomar: () => {
            if (mediaRecorder && mediaRecorder.state === 'paused') {
                mediaRecorder.resume();
                timerInterval = setInterval(async () => {
                    segundos++;
                    await invocar('TickCronometro', segundos);
                }, 1000);
                invocar('GravacaoRetomada');
            }
        },

        pararAsync: async () => {
            if (!mediaRecorder || mediaRecorder.state === 'inactive') return;

            pararTimer();

            await new Promise(resolve => {
                mediaRecorder.onstop = resolve;
                mediaRecorder.stop();
            });

            pararStream();

            try {
                const blob = new Blob(chunks, { type: 'video/webm' });
                const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19);
                const nomeOriginal = `gravacao_${timestamp}.webm`;

                const formData = new FormData();
                formData.append('notaId', notaId);
                formData.append('arquivo', blob, nomeOriginal);
                formData.append('nomeOriginal', nomeOriginal);

                const response = await fetch('/api/gravacoes/upload', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    const err = await response.json().catch(() => ({ erro: 'Erro desconhecido no upload.' }));
                    throw new Error(err.erro ?? 'Falha no upload.');
                }

                const dto = await response.json();
                chunks = [];
                await invocar('GravacaoParada', dto);
            } catch (e) {
                await invocar('GravacaoErro', `Falha ao salvar gravação: ${e.message}`);
            } finally {
                mediaRecorder = null;
                chunks = [];
                segundos = 0;
            }
        },

        cancelar: () => {
            resetar();
            mediaRecorder = null;
        }
    };
})();
