
/* DETALHES DA VAGA
   Recebe o projeto no formato da resposta da API.
   Mostra também a foto e a biografia da empresa.
*/

const jobDetailsModal = $("#job-details-modal");

function openJobDetails(job, options = {}) {

    activeDetailJob = job;

    // Verifica se a empresa possui foto.
    const fotoEmpresa = job.empresaFotoPerfil
        ? safeUrl(job.empresaFotoPerfil)
        : null;

    const iniciais = String(job.empresa || "")
        .split(" ")
        .filter(p => p.length > 2);

    const iniciaisEmpresa = iniciais.length > 1
        ? (iniciais[0][0] + iniciais[iniciais.length - 1][0]).toUpperCase()
        : (iniciais[0]?.[0] || "?").toUpperCase();

    $("#job-details-content").innerHTML = `

        <div class="job-details">

            <span class="category">
                ${escapeHtml(job.categoria)}
            </span>

            <h2>${escapeHtml(job.titulo)}</h2>

            <!-- PERFIL DA EMPRESA -->

            <div class="detalhes-empresa-perfil">

                ${fotoEmpresa
                    ? `
                        <img
                            src="${escapeHtml(fotoEmpresa)}"
                            alt="Foto da empresa"
                            class="detalhes-empresa-avatar">
                      `
                    : `
                        <div class="detalhes-empresa-avatar detalhes-empresa-iniciais">
                            ${escapeHtml(iniciaisEmpresa)}
                        </div>
                      `
                }

                <div>

                    <strong>
                        ${escapeHtml(job.empresa)}
                    </strong>

                    <p>
                        Publicada em
                        ${escapeHtml(formatDate(job.dataCriacao))}
                    </p>

                    ${job.empresaBio
                        ? `
                            <p class="detalhes-empresa-bio">
                                ${escapeHtml(job.empresaBio)}
                            </p>
                          `
                        : `
                            <p class="detalhes-empresa-bio">
                                Esta empresa ainda não escreveu uma biografia.
                            </p>
                          `
                    }

                </div>

            </div>

            <!-- DESCRIÇÃO -->

            <h3>Descrição completa</h3>

            <p>${escapeHtml(job.descricao)}</p>

            <!-- HABILIDADES -->

            <h3>Habilidades necessárias</h3>

            <div class="chips">

                ${skillsList(job.habilidades).map(skill =>
                    `<span>${escapeHtml(skill)}</span>`
                ).join("")}

            </div>

            <!-- INFORMAÇÕES DA VAGA -->

            <div class="job-facts">

                <div>
                    <small>Orçamento</small>

                    <strong>
                        ${currency(job.orcamentoMinimo)}
                        –
                        ${currency(job.orcamentoMaximo)}
                    </strong>
                </div>

                <div>
                    <small>Tipo de pagamento</small>

                    <strong>
                        ${escapeHtml(job.tipoPagamento)}
                    </strong>
                </div>

                <div>
                    <small>Prazo</small>

                    <strong>${job.prazoDias} dias</strong>
                </div>

                <div>
                    <small>Experiência necessária</small>

                    <strong>${escapeHtml(job.experiencia)}</strong>
                </div>

            </div>

            <a class="text-button"
               href="/Projetos/Detalhes/${Number(job.id)}">

                Abrir página do projeto ↗

            </a>

        </div>

    `;

    const mainAction = $("#details-main-action");

    mainAction.textContent = "Tenho interesse";

    // No histórico de candidaturas, o modal é somente leitura.
    mainAction.classList.toggle(
        "hidden",
        Boolean(options.readOnly)
    );

    jobDetailsModal.showModal();

}

// =====================================
// BOTÃO DE INTERESSE
// =====================================

$("#details-main-action").addEventListener("click", () => {

    if (!activeDetailJob) return;

    jobDetailsModal.close();

    if (typeof openApplicationForm === "function") {

        openApplicationForm(activeDetailJob);

    }

});
