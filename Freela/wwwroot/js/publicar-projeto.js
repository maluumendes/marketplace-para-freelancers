const formularioProjeto = document.getElementById("projeto-form");
const mensagemProjeto = document.getElementById("projeto-mensagem");
const botaoSalvar = formularioProjeto.querySelector('button[type="submit"]');
const projetoId = formularioProjeto.dataset.id;
const enderecoApi = formularioProjeto.dataset.api;

// O mesmo formulário serve para publicar e editar.
async function carregarFormularioProjeto() {
    botaoSalvar.disabled = true;
    try {
        const projeto = await api(enderecoApi + "/" + projetoId);
        for (const campo of formularioProjeto.elements) {
            if (campo.name && projeto[campo.name] !== undefined) {
                campo.value = projeto[campo.name];
            }
        }
        if (projeto.status === "Encerrado") {
            mensagemProjeto.textContent = "Este projeto já foi encerrado.";
        } else {
            botaoSalvar.disabled = false;
        }
    } catch (erro) {
        mensagemProjeto.textContent = erro.message;
    }
}

if (projetoId) carregarFormularioProjeto();

formularioProjeto.addEventListener("submit", async function (evento) {
    evento.preventDefault();
    if (!formularioProjeto.reportValidity()) return;

    const campos = formularioProjeto.elements;
    const projeto = {
        titulo: campos["titulo"].value,
        descricao: campos["descricao"].value,
        categoria: campos["categoria"].value,
        habilidades: campos["habilidades"].value,
        orcamentoMinimo: Number(campos["orcamentoMinimo"].value),
        orcamentoMaximo: Number(campos["orcamentoMaximo"].value),
        prazoDias: Number(campos["prazoDias"].value),
        tipoPagamento: campos["tipoPagamento"].value,
        experiencia: campos["experiencia"].value
    };

    botaoSalvar.disabled = true;
    mensagemProjeto.textContent = "Salvando...";

    try {
        let id = projetoId;
        if (id) {
            await api(enderecoApi + "/" + id, { method: "PUT", body: projeto });
        } else {
            const publicado = await api(enderecoApi, { method: "POST", body: projeto });
            id = publicado.id;
        }
        window.location.href = formularioProjeto.dataset.detalhes + "/" + id;
    } catch (erro) {
        mensagemProjeto.textContent = erro.message;
        botaoSalvar.disabled = false;
    }
});