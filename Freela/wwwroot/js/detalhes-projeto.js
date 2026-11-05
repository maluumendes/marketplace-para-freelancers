const detalhesProjeto = document.getElementById("detalhes-projeto");
const mensagemDetalhes = document.getElementById("mensagem");
const botaoEncerrar = document.getElementById("encerrar-projeto");

async function carregarDetalhesProjeto() {
    try {
        const projeto = await api(detalhesProjeto.dataset.api + "/" + detalhesProjeto.dataset.id);
        detalhesProjeto.appendChild(criarCardProjeto(projeto, true));

        // Mostra os botões somente para a empresa dona de um projeto aberto.
        const empresaLogada = detalhesProjeto.dataset.cliente === "true";
        const donoDoProjeto = String(projeto.clienteId) === detalhesProjeto.dataset.usuario;
        if (empresaLogada && donoDoProjeto && projeto.status === "Aberto") {
            document.getElementById("acoes-projeto").hidden = false;
        }

        // Integrante 3: a empresa dona vê os candidatos desta vaga
        if (empresaLogada && donoDoProjeto) {
            const verCandidatos = document.getElementById("ver-candidatos");
            verCandidatos.hidden = false;
            verCandidatos.addEventListener("click", function () {
                writeStore(SELECTED_PROJECT_KEY, projeto.id);
            });
        }

        // Integrante 3: o freelancer demonstra interesse (abre o modal de candidatura do _Layout).
        // Se ele já se candidatou, mostra o status no lugar do botão.
        const botaoInteresse = document.getElementById("tenho-interesse");
        if (botaoInteresse) {
            const minhas = await api("/api/candidaturas/minhas");
            const minha = minhas.find(c => c.projeto.id === projeto.id);

            if (minha) {
                const rotulos = { Pendente: "Candidatura enviada — aguardando a empresa", Aceita: "Match ♡ — veja em Minhas candidaturas", Rejeitada: "A empresa não selecionou sua candidatura" };
                adicionarTexto(detalhesProjeto, "p", rotulos[minha.status] || minha.status);
            } else if (projeto.status === "Aberto") {
                botaoInteresse.hidden = false;
                botaoInteresse.addEventListener("click", function () {
                    openApplicationForm(projeto);
                });
            }
        }
    } catch (erro) {
        mensagemDetalhes.textContent = erro.message;
    }
}

botaoEncerrar.addEventListener("click", async function () {
    if (!confirm("Deseja encerrar este projeto? Ele deixará de aparecer na busca.")) return;

    botaoEncerrar.disabled = true;
    try {
        await api(detalhesProjeto.dataset.api + "/" + detalhesProjeto.dataset.id + "/encerrar", { method: "PATCH" });
        window.location.reload();
    } catch (erro) {
        mensagemDetalhes.textContent = erro.message;
        botaoEncerrar.disabled = false;
    }
});

// Chamado pelo application-form.js quando a candidatura é enviada
function onApplicationSent() {
    const botaoInteresse = document.getElementById("tenho-interesse");
    if (botaoInteresse) {
        botaoInteresse.disabled = true;
        botaoInteresse.textContent = "Candidatura enviada";
    }
}

carregarDetalhesProjeto();