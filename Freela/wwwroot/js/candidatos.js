
/* CANDIDATOS PARA A EMPRESA
   1) Carrega os projetos da empresa.
   2) Mostra os candidatos pendentes.
   3) Permite aceitar ou rejeitar.
   4) Lista os matches realizados.
   5) Exibe foto e biografia dos freelancers.
*/

let myProjects = [];
let pendingCandidates = [];
let allMatches = [];
let candidateBusy = false;

// =====================================
// CONFERÊNCIA DOS ARQUIVOS
// =====================================

const ARQUIVOS_NECESSARIOS = {
    "utils.js": () =>
        typeof $ === "function" &&
        typeof escapeHtml === "function" &&
        typeof currency === "function" &&
        typeof formatDate === "function" &&
        typeof safeUrl === "function" &&
        typeof showToast === "function",

    "api.js": () =>
        typeof api === "function" &&
        typeof showApiError === "function",

    "storage.js": () =>
        typeof readStore === "function" &&
        typeof writeStore === "function" &&
        typeof SELECTED_PROJECT_KEY !== "undefined",

    "swipe.js": () =>
        typeof addSwipe === "function"
};

function arquivosDesatualizados() {
    return Object.entries(ARQUIVOS_NECESSARIOS)
        .filter(([, carregado]) => !carregado())
        .map(([arquivo]) => arquivo);
}

// =====================================
// MOSTRAR ERROS
// =====================================

function mostrarErro(mensagem) {

    [
        "#project-select-wrap",
        "#candidate-stage",
        "#candidate-empty",
        "#projects-empty",
        "#matches-section"
    ].forEach(seletor => {
        document.querySelector(seletor)?.classList.add("hidden");
    });

    document.getElementById("candidate-count").textContent =
        "Candidatos indisponíveis";

    const caixa = document.getElementById("candidates-error");

    const texto = document.getElementById("candidates-error-message");

    if (caixa && texto) {

        texto.textContent = mensagem;

        caixa.classList.remove("hidden");

    } else {

        document.getElementById("candidate-count").textContent =
            "Erro: " + mensagem;

    }
}

function selectedProjectId() {
    return Number($("#project-select").value) || null;
}

// =====================================
// CARREGAR PROJETOS
// =====================================

async function loadProjects() {

    document.getElementById("candidates-error")?.classList.add("hidden");

    $("#candidate-count").textContent = "Carregando...";

    try {

        await carregarTela();

    } catch (error) {

        if (error?.status === 401) return;

        mostrarErro(error?.message || "Tente novamente em instantes.");

    }
}

async function carregarTela() {

    myProjects = await api("/api/projetos?meus=true");

    if (!Array.isArray(myProjects)) {
        myProjects = [];
    }

    $("#project-select-wrap").classList.remove("hidden");

    if (!myProjects.length) {

        $("#project-select-wrap").classList.add("hidden");

        $("#candidate-stage").classList.add("hidden");

        $("#candidate-empty").classList.add("hidden");

        $("#projects-empty").classList.remove("hidden");

        $("#candidate-count").textContent =
            "Nenhuma vaga publicada";

        return;
    }

    // Mantém a vaga selecionada anteriormente.
    const remembered = Number(readStore(SELECTED_PROJECT_KEY, 0));

    const withPending = myProjects.find(
        p => p.candidaturasPendentes > 0
    );

    const initial = myProjects.some(p => p.id === remembered)
        ? remembered
        : (withPending || myProjects[0]).id;

    renderProjectOptions(initial);

    await Promise.all([
        loadCandidates(),
        loadMatches()
    ]);
}

// =====================================
// OPÇÕES DE PROJETOS
// =====================================

function renderProjectOptions(selectedId) {

    $("#project-select").innerHTML = myProjects.map(project => `
        <option value="${project.id}"
            ${project.id === selectedId ? "selected" : ""}>

            ${escapeHtml(project.titulo)}
            ${project.status === "Encerrado" ? " [encerrada]" : ""}

            (${project.candidaturasPendentes ?? 0}
            pendente${project.candidaturasPendentes === 1 ? "" : "s"})

        </option>
    `).join("");
}

// =====================================
// CARREGAR CANDIDATOS
// =====================================

async function loadCandidates() {

    const projectId = selectedProjectId();

    if (!projectId) return;

    writeStore(SELECTED_PROJECT_KEY, projectId);

    $("#candidate-count").textContent = "Carregando candidatos...";

    try {

        pendingCandidates = await api(
            `/api/candidaturas/projeto/${projectId}?status=Pendente`
        );

        renderCandidate();

    } catch (error) {

        if (error?.status === 401) return;

        mostrarErro(error?.message || "Tente novamente em instantes.");

    }
}

// =====================================
// CARREGAR MATCHES
// =====================================

async function loadMatches() {

    try {

        allMatches = await api("/api/matches");

    } catch (error) {

        allMatches = [];

    }

    try {

        renderMatches();

    } catch (error) {

        console.error("Erro ao mostrar matches:", error);

    }
}

// =====================================
// MOSTRAR CARD DO CANDIDATO
// =====================================

function renderCandidate() {

    const stage = $("#candidate-stage");

    const empty = $("#candidate-empty");

    const remaining = pendingCandidates.length;

    $("#candidate-count").textContent =
        `${remaining} candidato${remaining === 1 ? "" : "s"} pendente${remaining === 1 ? "" : "s"}`;

    // Atualiza a quantidade de pendentes no seletor.
    const project = myProjects.find(
        p => p.id === selectedProjectId()
    );

    if (project) {

        project.candidaturasPendentes = remaining;

        renderProjectOptions(project.id);

    }

    if (!remaining) {

        stage.classList.add("hidden");

        empty.classList.remove("hidden");

        return;
    }

    stage.classList.remove("hidden");

    empty.classList.add("hidden");

    const person = pendingCandidates[0];

    const card = stage.querySelector(".candidate-card");

    const portfolio = person.linkPortfolio
        ? safeUrl(person.linkPortfolio)
        : null;

    // Verifica se existe uma foto válida.
    const foto = person.fotoPerfil
        ? safeUrl(person.fotoPerfil)
        : null;

    card.innerHTML = `

        <!-- FOTO DO FREELANCER -->

        ${foto
            ? `
                <img
                    class="avatar candidate-avatar"
                    src="${escapeHtml(foto)}"
                    alt="Foto de ${escapeHtml(person.nome)}">
              `
            : `
                <div class="avatar">
                    ${escapeHtml(person.iniciais)}
                </div>
              `
        }

        <h2>${escapeHtml(person.nome)}</h2>

        <p class="candidate-role">
            Candidatou-se em
            ${escapeHtml(formatDate(person.dataCandidatura))}
        </p>

        <!-- BIOGRAFIA DO FREELANCER -->

        <p class="candidate-bio">
            ${escapeHtml(
                person.bio ||
                "Este freelancer ainda não escreveu uma bio."
            )}
        </p>

        <div class="candidate-facts">

            <div>
                <small>Proposta</small>

                <strong>${currency(person.valorProposto)}</strong>
            </div>

            <div>
                <small>Prazo estimado</small>

                <strong>${person.prazoDias} dias</strong>
            </div>

        </div>

        <blockquote class="candidate-message">
            “${escapeHtml(person.mensagemApresentacao)}”
        </blockquote>

        ${portfolio ? `
            <a
                class="text-button portfolio-button"
                href="${escapeHtml(portfolio)}"
                target="_blank"
                rel="noopener noreferrer">

                Ver portfólio ↗

            </a>
        ` : ""}

    `;
}

// =====================================
// ACEITAR OU REJEITAR CANDIDATO
// =====================================

async function decideCandidate(accepted) {

    const person = pendingCandidates[0];

    if (!person || candidateBusy) return;

    candidateBusy = true;

    try {

        const result = await api(
            `/api/candidaturas/${person.candidaturaId}/decisao`,
            {
                method: "PUT",
                body: {
                    aceitar: accepted
                }
            }
        );

        showToast(
            result?.match ? "Deu match! 🎉" : "Candidato recusado",
            `${person.nome} · ${$("#project-select").selectedOptions[0]?.textContent.trim() || ""}`,
            result?.match ? "success" : "reject"
        );

        pendingCandidates.shift();

        renderCandidate();

        if (result?.match) {
            loadMatches();
        }

    } catch (error) {

        showApiError(error);

        if (error.status === 409) {
            loadCandidates();
        }

    } finally {

        candidateBusy = false;

    }
}

// =====================================
// MOSTRAR MATCHES
// =====================================

function renderMatches() {

    const section = $("#matches-section");

    const projectId = selectedProjectId();

    const matches = allMatches.filter(
        match => match.projetoId === projectId
    );

    if (!matches.length) {

        section.classList.add("hidden");

        return;
    }

    section.classList.remove("hidden");

    $("#match-list").innerHTML = matches.map(match => {

        const foto = match.fotoFreelancer
            ? safeUrl(match.fotoFreelancer)
            : null;

        return `
            <article class="match-card">

                <!-- FOTO DO FREELANCER -->

                ${foto
                    ? `
                        <img
                            class="avatar small-avatar match-photo"
                            src="${escapeHtml(foto)}"
                            alt="Foto de ${escapeHtml(match.nomeFreelancer)}">
                      `
                    : `
                        <div class="avatar small-avatar">
                            ${escapeHtml(initialsOf(match.nomeFreelancer))}
                        </div>
                      `
                }

                <div class="match-info">

                    <strong>
                        ${escapeHtml(match.nomeFreelancer)}
                    </strong>

                    <!-- BIOGRAFIA -->

                    ${match.bioFreelancer
                        ? `
                            <p class="match-bio">
                                ${escapeHtml(match.bioFreelancer)}
                            </p>
                          `
                        : ""
                    }

                    <small>
                        Match em ${escapeHtml(formatDate(match.dataMatch))}
                        · ${currency(match.valorProposto)}
                        · ${match.prazoDias} dias
                    </small>

                    ${match.emailContato
                        ? `
                            <a href="mailto:${escapeHtml(match.emailContato)}">
                                ${escapeHtml(match.emailContato)}
                            </a>
                          `
                        : ""
                    }

                    ${match.telefoneContato
                        ? `<small>${escapeHtml(match.telefoneContato)}</small>`
                        : ""
                    }

                </div>

            </article>
        `;

    }).join("");
}

// =====================================
// GERAR INICIAIS
// =====================================

function initialsOf(name) {

    const parts = String(name)
        .split(" ")
        .filter(part => part.length > 2);

    if (!parts.length) {
        return String(name).slice(0, 1).toUpperCase();
    }

    return (
        parts[0][0] +
        (parts.length > 1 ? parts[parts.length - 1][0] : "")
    ).toUpperCase();
}

// =====================================
// INICIALIZAÇÃO
// =====================================

const faltando = arquivosDesatualizados();

if (faltando.length) {

    mostrarErro(
        "A página está com arquivos desatualizados ou faltando: " +
        faltando.join(", ") +
        ". Atualize os arquivos e recarregue com Ctrl+F5."
    );

} else {

    $("#project-select").addEventListener("change", () => {

        loadCandidates();

        renderMatches();

    });

    $("#candidate-reject").addEventListener(
        "click",
        () => decideCandidate(false)
    );

    $("#candidate-accept").addEventListener(
        "click",
        () => decideCandidate(true)
    );

    $("#reset-candidates")?.addEventListener(
        "click",
        loadCandidates
    );

    document.getElementById("candidates-retry")?.addEventListener(
        "click",
        loadProjects
    );

    // Arrastar o cartão do candidato.
    addSwipe(
        $(".candidate-card"),
        () => decideCandidate(false),
        () => decideCandidate(true)
    );

    loadProjects();

}
