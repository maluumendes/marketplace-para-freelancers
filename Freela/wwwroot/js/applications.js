
/* MINHAS CANDIDATURAS (freelancer)
   GET /api/candidaturas/minhas
   DELETE /api/candidaturas/{id}

   Integrante 4:
   Exibe foto e biografia da empresa nos cards.
*/

const STATUS_LABEL = {
    Pendente: "Aguardando empresa",
    Aceita: "Match ♡",
    Rejeitada: "Não selecionada"
};

let myApplications = [];

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

    "detalhes-vaga.js": () =>
        typeof openJobDetails === "function"
};

function arquivosDesatualizados() {

    return Object.entries(ARQUIVOS_NECESSARIOS)
        .filter(([, carregado]) => !carregado())
        .map(([arquivo]) => arquivo);

}

// =====================================
// MOSTRAR ERRO
// =====================================

function mostrarErro(mensagem) {

    const container =
        document.getElementById("applications-content");

    container.innerHTML = `
        <div class="empty-state" role="alert">

            <div class="empty-icon">!</div>

            <h2>Não foi possível carregar suas candidaturas</h2>

            <p></p>

            <button
                class="primary-button"
                id="applications-retry"
                type="button">

                Tentar novamente

            </button>

        </div>
    `;

    container.querySelector("p").textContent = mensagem;

    document.getElementById("applications-retry")
        .addEventListener("click", loadApplications);

}

// =====================================
// CARREGAR CANDIDATURAS
// =====================================

async function loadApplications() {

    const container =
        document.getElementById("applications-content");

    container.innerHTML = `
        <p class="loading-text">
            Carregando candidaturas...
        </p>
    `;

    try {

        const lista = await api("/api/candidaturas/minhas");

        myApplications = Array.isArray(lista)
            ? lista
            : [];

        renderApplications();

    } catch (error) {

        if (error?.status === 401) return;

        mostrarErro(
            error?.message || "Tente novamente em instantes."
        );

    }

}

// =====================================
// MOSTRAR LISTA DE CANDIDATURAS
// =====================================

function renderApplications() {

    const container = $("#applications-content");

    // ESTADO VAZIO.
    if (!myApplications.length) {

        container.innerHTML = `
            <div class="empty-state">

                <div class="empty-icon">♡</div>

                <h2>Nenhuma candidatura ainda</h2>

                <p>
                    Explore as oportunidades e candidate-se
                    aos projetos que combinam com você.
                </p>

                <button
                    class="primary-button"
                    id="empty-find-jobs"
                    type="button">

                    Procurar vagas

                </button>

            </div>
        `;

        $("#empty-find-jobs").addEventListener("click", () => {
            location.href = "/Home/Vagas";
        });

        return;
    }

    // Contagem por status.
    const count = status =>
        myApplications.filter(a => a.status === status).length;

    container.innerHTML = `
        <div class="application-summary">

            <span>
                <strong>${count("Pendente")}</strong>
                pendente(s)
            </span>

            <span class="summary-match">
                <strong>${count("Aceita")}</strong>
                match(es)
            </span>

            <span>
                <strong>${count("Rejeitada")}</strong>
                não selecionada(s)
            </span>

        </div>

        <div class="application-list">

            ${myApplications.map(renderApplicationCard).join("")}

        </div>
    `;

    // Botões de detalhes.
    container.querySelectorAll("[data-app-details]")
        .forEach(button => {

            button.addEventListener("click", () => {

                const application =
                    findApplication(button.dataset.appDetails);

                if (application) {

                    openJobDetails(
                        application.projeto,
                        { readOnly: true }
                    );

                }

            });

        });

    // Botões de retirada.
    container.querySelectorAll("[data-app-withdraw]")
        .forEach(button => {

            button.addEventListener("click", () => {
                withdrawApplication(button);
            });

        });

}

// =====================================
// GERAR INICIAIS DA EMPRESA
// =====================================

function iniciaisEmpresa(nome) {

    const partes = String(nome)
        .split(" ")
        .filter(p => p.length > 2);

    if (!partes.length) {
        return String(nome).slice(0, 1).toUpperCase();
    }

    if (partes.length === 1) {
        return partes[0][0].toUpperCase();
    }

    return (
        partes[0][0] +
        partes[partes.length - 1][0]
    ).toUpperCase();

}

// =====================================
// CARD DA CANDIDATURA
// =====================================

function renderApplicationCard(application) {

    const job = application.projeto;

    const status = application.status;

    const isMatch = status === "Aceita";

    // Verifica se existe foto válida.
    const fotoEmpresa = job.empresaFotoPerfil
        ? safeUrl(job.empresaFotoPerfil)
        : null;

    // Área exibida quando ocorre Match.
    const matchBox = isMatch ? `

        <div class="match-box">

            <strong>
                Vocês deram match em
                ${escapeHtml(formatDate(application.dataResposta))}!
            </strong>

            <span>
                Entre em contato com ${escapeHtml(job.empresa)}:
            </span>

            ${application.emailEmpresa
                ? `
                    <a href="mailto:${escapeHtml(application.emailEmpresa)}">
                        ${escapeHtml(application.emailEmpresa)}
                    </a>
                  `
                : ""
            }

            ${application.telefoneEmpresa
                ? `
                    <span>
                        ${escapeHtml(application.telefoneEmpresa)}
                    </span>
                  `
                : ""
            }

        </div>

    ` : "";

    return `

        <article class="application-card ${isMatch ? "is-match" : ""}">

            <!-- CABEÇALHO DA CANDIDATURA -->

            <div class="application-top">

                <div>

                    <h2>${escapeHtml(job.titulo)}</h2>

                    <!-- PERFIL DA EMPRESA -->

                    <div class="application-company-profile">

                        ${fotoEmpresa
                            ? `
                                <img
                                    src="${escapeHtml(fotoEmpresa)}"
                                    class="application-company-avatar"
                                    alt="Foto da empresa">
                              `
                            : `
                                <div class="application-company-avatar application-company-initials">

                                    ${escapeHtml(
                                        iniciaisEmpresa(job.empresa)
                                    )}

                                </div>
                              `
                        }

                        <div>

                            <p class="application-date">
                                ${escapeHtml(job.empresa)}
                                · enviada em
                                ${escapeHtml(formatDate(application.dataCandidatura))}
                            </p>

                            ${job.empresaBio
                                ? `
                                    <p class="application-company-bio">
                                        ${escapeHtml(job.empresaBio)}
                                    </p>
                                  `
                                : ""
                            }

                        </div>

                    </div>

                </div>

                <span class="status status-${escapeHtml(status.toLowerCase())}">

                    ${escapeHtml(STATUS_LABEL[status] || status)}

                </span>

            </div>

            <!-- MENSAGEM DA CANDIDATURA -->

            <p class="application-message">
                ${escapeHtml(application.mensagemApresentacao)}
            </p>

            <!-- PROPOSTA E PRAZO -->

            <div class="application-meta">

                Prazo:
                <strong>${application.prazoDias} dias</strong>

                · Proposta:
                <strong>${currency(application.valorProposto)}</strong>

            </div>

            <!-- INFORMAÇÕES DO MATCH -->

            ${matchBox}

            <!-- AÇÕES -->

            <div class="application-actions">

                <button
                    class="text-button"
                    type="button"
                    data-app-details="${application.id}">

                    Ver detalhes da vaga

                </button>

                ${status === "Pendente" ? `

                    <button
                        class="text-button danger-text"
                        type="button"
                        data-app-withdraw="${application.id}">

                        Retirar candidatura

                    </button>

                ` : ""}

            </div>

        </article>

    `;

}

// =====================================
// BUSCAR CANDIDATURA
// =====================================

function findApplication(id) {

    return myApplications.find(
        item => item.id === Number(id)
    );

}

// =====================================
// RETIRAR CANDIDATURA
// =====================================

async function withdrawApplication(button) {

    const application =
        findApplication(button.dataset.appWithdraw);

    if (!application) return;

    if (!window.confirm(
        `Retirar a candidatura para "${application.projeto.titulo}"?`
    )) {
        return;
    }

    button.disabled = true;

    try {

        await api(
            `/api/candidaturas/${application.id}`,
            { method: "DELETE" }
        );

        showToast(
            "Candidatura retirada",
            application.projeto.titulo,
            "reject"
        );

        myApplications = myApplications.filter(
            item => item.id !== application.id
        );

        renderApplications();

    } catch (error) {

        button.disabled = false;

        showApiError(error);

        if (error.status === 400 || error.status === 409) {
            loadApplications();
        }

    }

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

    loadApplications();

}
