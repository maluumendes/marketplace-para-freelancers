const listaProjetos = document.getElementById("lista-projetos");
const mensagemBusca = document.getElementById("mensagem");
const formularioFiltros = document.getElementById("filtros");
let buscandoProjetos = false;

async function carregarProjetos() {
    if (buscandoProjetos) return;
    buscandoProjetos = true;
    mensagemBusca.textContent = "Carregando...";
    listaProjetos.replaceChildren();

    const parametros = new URLSearchParams();
    if (listaProjetos.dataset.meus === "true") parametros.set("meus", "true");

    if (formularioFiltros) {
        for (const campo of formularioFiltros.elements) {
            if (campo.name && campo.value.trim() !== "") {
                parametros.set(campo.name, campo.value.trim());
            }
        }
        formularioFiltros.querySelector('button[type="submit"]').disabled = true;
    }

    try {
        const projetos = await api(listaProjetos.dataset.api + "?" + parametros);
        mensagemBusca.textContent = projetos.length + " projeto(s) encontrado(s).";
        if (projetos.length === 0) mensagemBusca.textContent = "Nenhum projeto encontrado.";

        for (const projeto of projetos) {
            const card = criarCardProjeto(projeto);
            const detalhes = document.createElement("a");
            detalhes.textContent = "Ver detalhes";
            detalhes.href = listaProjetos.dataset.detalhes + "/" + projeto.id;
            card.appendChild(detalhes);

            // Integrante 3: em "Meus projetos" a API traz as contagens de candidaturas
            if (projeto.candidaturasPendentes !== null && projeto.candidaturasPendentes !== undefined) {
                const candidatos = document.createElement("a");
                candidatos.textContent = "Ver candidatos (" + projeto.candidaturasPendentes + " pendente(s), "
                    + projeto.totalMatches + " match(es))";
                candidatos.href = "/Home/Candidatos";
                candidatos.addEventListener("click", function () {
                    writeStore(SELECTED_PROJECT_KEY, projeto.id);
                });
                card.appendChild(candidatos);
            }

            listaProjetos.appendChild(card);
        }
    } catch (erro) {
        mensagemBusca.textContent = erro.message;
    } finally {
        buscandoProjetos = false;
        if (formularioFiltros) formularioFiltros.querySelector('button[type="submit"]').disabled = false;
    }
}

if (formularioFiltros) {
    formularioFiltros.addEventListener("submit", function (evento) {
        evento.preventDefault();
        carregarProjetos();
    });
}

carregarProjetos();