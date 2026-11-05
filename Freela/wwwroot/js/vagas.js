
/* VAGAS DISPONÍVEIS (tela "Procurar vagas")
   Os cards vêm de GET /api/projetos/disponiveis, com os filtros da busca.
   
   × -> POST /api/interesses (Rejeicao: a vaga some da lista)
   ♡ -> abre o modal de candidatura (application-form.js)

   INTEGRANTE 4:
   - Exibir foto de perfil da empresa.
   - Exibir biografia da empresa.
   - Exibir iniciais quando não houver foto.
*/

let availableJobs = [];
let jobBusy = false;

// =====================================
// VALORES DOS FILTROS
// =====================================

const filtros = {
  busca: "",
  categoria: "",
  orcamento: ""
};

/* ---------- CONFERÊNCIA DOS ARQUIVOS ---------- */

const ARQUIVOS_NECESSARIOS = {
  "utils.js": () => typeof $ === "function" && typeof escapeHtml === "function" &&
                    typeof currency === "function" && typeof formatDate === "function" &&
                    typeof skillsList === "function" && typeof showToast === "function" &&
                    typeof safeUrl === "function",

  "api.js": () => typeof api === "function" && typeof showApiError === "function",

  "swipe.js": () => typeof addSwipe === "function",

  "detalhes-vaga.js": () => typeof openJobDetails === "function",

  "application-form.js": () => typeof openApplicationForm === "function"
};

function arquivosDesatualizados() {
  return Object.entries(ARQUIVOS_NECESSARIOS)
    .filter(([, carregado]) => !carregado())
    .map(([arquivo]) => arquivo);
}

// =====================================
// FILTROS
// =====================================

// Monta a URL com os filtros escolhidos.
function filterQuery() {

  const params = new URLSearchParams();

  if (filtros.busca) {
    params.set("busca", filtros.busca);
  }

  if (filtros.categoria) {
    params.set("categoria", filtros.categoria);
  }

  if (filtros.orcamento) {

    const [minimo, maximo] = filtros.orcamento.split("-");

    if (minimo) {
      params.set("minimo", minimo);
    }

    if (maximo) {
      params.set("maximo", maximo);
    }
  }

  const query = params.toString();

  return query ? "?" + query : "";
}

function hasFilters() {
  return filterQuery() !== "";
}

function updateFilterUI() {

  document.getElementById("limpar-filtros")
    ?.classList.toggle("hidden", !hasFilters());

}

// =====================================
// MENUS DE FILTROS
// =====================================

function fecharMenus(exceto) {

  document.querySelectorAll(".dropdown").forEach(dropdown => {

    if (dropdown === exceto) return;

    dropdown.querySelector(".dropdown-menu").hidden = true;

    dropdown.querySelector(".dropdown-botao")
      .setAttribute("aria-expanded", "false");

  });

}

function escolherOpcao(dropdown, valor) {

  const opcoes = dropdown.querySelectorAll(".dropdown-menu button");

  opcoes.forEach(opcao => {

    const escolhida = opcao.dataset.value === valor;

    opcao.setAttribute("aria-selected", String(escolhida));

    if (escolhida) {

      dropdown.querySelector(".dropdown-texto").textContent =
        opcao.textContent.trim();

    }

  });

  // Destaca o botão quando existe filtro escolhido.
  dropdown.classList.toggle("ativo", valor !== "");

  filtros[dropdown.dataset.filtro] = valor;

}

// Configura os menus suspensos.
document.querySelectorAll(".dropdown").forEach(dropdown => {

  const botao = dropdown.querySelector(".dropdown-botao");

  const menu = dropdown.querySelector(".dropdown-menu");

  botao.addEventListener("click", () => {

    const abrir = menu.hidden;

    fecharMenus(dropdown);

    menu.hidden = !abrir;

    botao.setAttribute("aria-expanded", String(abrir));

    if (abrir) {

      (
        menu.querySelector('[aria-selected="true"]') ||
        menu.querySelector("button")
      )?.focus();

    }

  });

  menu.querySelectorAll("button").forEach(opcao => {

    opcao.addEventListener("click", () => {

      escolherOpcao(dropdown, opcao.dataset.value);

      fecharMenus();

      botao.focus();

      loadJobs();

    });

  });

});

// Fecha os menus ao clicar fora.
document.addEventListener("click", event => {

  if (!event.target.closest(".dropdown")) {
    fecharMenus();
  }

});

// Fecha os menus ao apertar Esc.
document.addEventListener("keydown", event => {

  if (event.key === "Escape") {
    fecharMenus();
  }

});

// =====================================
// CAMPO DE BUSCA
// =====================================

let esperaBusca = null;

// Pesquisa enquanto a pessoa digita.
document.getElementById("busca")?.addEventListener("input", event => {

  window.clearTimeout(esperaBusca);

  esperaBusca = window.setTimeout(() => {

    filtros.busca = event.target.value.trim();

    loadJobs();

  }, 400);

});

// Pesquisa ao pressionar Enter.
document.getElementById("busca")?.addEventListener("keydown", event => {

  if (event.key !== "Enter") return;

  window.clearTimeout(esperaBusca);

  filtros.busca = event.target.value.trim();

  loadJobs();

});

// =====================================
// LIMPAR FILTROS
// =====================================

document.getElementById("limpar-filtros")?.addEventListener("click", () => {

  document.getElementById("busca").value = "";

  filtros.busca = "";

  document.querySelectorAll(".dropdown").forEach(dropdown => {

    escolherOpcao(dropdown, "");

  });

  loadJobs();

});

// =====================================
// CARREGAR E MOSTRAR AS VAGAS
// =====================================

let ultimaBusca = 0;

// Carrega as vagas da API.
async function loadJobs() {

  const minhaBusca = ++ultimaBusca;

  document.getElementById("job-count").textContent =
    "Carregando vagas...";

  document.getElementById("jobs-error")
    ?.classList.add("hidden");

  updateFilterUI();

  try {

    const vagas = await api(
      "/api/projetos/disponiveis" + filterQuery()
    );

    // Ignora respostas antigas de pesquisas anteriores.
    if (minhaBusca !== ultimaBusca) return;

    availableJobs = Array.isArray(vagas) ? vagas : [];

    renderJob();

  } catch (error) {

    if (minhaBusca !== ultimaBusca) return;

    availableJobs = [];

    showLoadError(error);

  }

}

// =====================================
// MOSTRAR ERROS
// =====================================

function showLoadError(error) {

  if (error?.status === 401) return;

  document.getElementById("job-card")
    ?.classList.add("hidden");

  document.getElementById("job-controls")
    ?.classList.add("hidden");

  document.getElementById("jobs-empty")
    ?.classList.add("hidden");

  document.getElementById("job-count").textContent =
    "Vagas indisponíveis";

  const caixa = document.getElementById("jobs-error");

  const mensagem = document.getElementById("jobs-error-message");

  if (caixa && mensagem) {

    mensagem.textContent =
      error?.message || "Tente novamente em instantes.";

    caixa.classList.remove("hidden");

  } else {

    document.getElementById("job-count").textContent =
      "Erro ao carregar vagas: " +
      (error?.message || "tente novamente");

  }

}

// =====================================
// GERAR INICIAIS DA EMPRESA
// =====================================

function gerarIniciaisEmpresa(nome) {

  const partes = String(nome || "")
    .trim()
    .split(/\s+/)
    .filter(parte => parte.length > 2);

  if (partes.length === 0) {
    return "?";
  }

  if (partes.length === 1) {
    return partes[0].substring(0, 1).toUpperCase();
  }

  return (
    partes[0].substring(0, 1) +
    partes[partes.length - 1].substring(0, 1)
  ).toUpperCase();

}

// =====================================
// MOSTRAR CARD DA VAGA
// =====================================

function renderJob() {

  const card = $("#job-card");

  const empty = $("#jobs-empty");

  const total = availableJobs.length;

  $("#job-count").textContent =
    `${total} vaga${total === 1 ? "" : "s"} disponív${total === 1 ? "el" : "eis"}`;

  // =====================================
  // ESTADO VAZIO
  // =====================================

  if (!total) {

    const titulo = $("#jobs-empty-title");

    if (titulo) {

      titulo.textContent = hasFilters()
        ? "Nenhuma vaga encontrada 🔍"
        : "Você viu todas as vagas por enquanto! 💗";

    }

    const texto = $("#jobs-empty-text");

    if (texto) {

      texto.textContent = hasFilters()
        ? "Nenhuma vaga combina com estes filtros. Tente mudar ou limpar os filtros."
        : "Não encontramos novas oportunidades no momento. Volte mais tarde para conferir novos projetos.";

    }

    card?.classList.add("hidden");

    $("#job-controls")?.classList.add("hidden");

    empty?.classList.remove("hidden");

    activeDetailJob = null;

    return;

  }

  empty?.classList.add("hidden");

  card?.classList.remove("hidden");

  $("#job-controls")?.classList.remove("hidden");

  // Sempre exibe a primeira vaga da fila.
  const job = availableJobs[0];

  activeDetailJob = job;

  // =====================================
  // PERFIL DA EMPRESA
  // =====================================

  // Busca o caminho da foto enviado pela API.
  const fotoEmpresa = job.empresaFotoPerfil
    ? safeUrl(job.empresaFotoPerfil)
    : null;

  // Gera as iniciais para usar quando não existir foto.
  const iniciaisEmpresa = gerarIniciaisEmpresa(job.empresa);

  // Prepara a foto ou as iniciais da empresa.
  const avatarEmpresa = fotoEmpresa
    ? `
        <img
          src="${escapeHtml(fotoEmpresa)}"
          class="job-company-avatar"
          alt="Foto de ${escapeHtml(job.empresa)}">
      `
    : `
        <div class="job-company-avatar job-company-initials"
             aria-label="Iniciais da empresa ${escapeHtml(job.empresa)}">

          ${escapeHtml(iniciaisEmpresa)}

        </div>
      `;

  // Prepara a biografia.
  const bioEmpresa = job.empresaBio
    ? `
        <p class="job-company-bio">
          ${escapeHtml(job.empresaBio)}
        </p>
      `
    : `
        <p class="job-company-bio">
          Esta empresa ainda não escreveu uma biografia.
        </p>
      `;

  // =====================================
  // MONTAGEM DO CARD
  // =====================================

  card.innerHTML = `

    <!-- CATEGORIA DA VAGA -->

    <span class="category">
      ${escapeHtml(job.categoria)}
    </span>

    <!-- TÍTULO DA VAGA -->

    <h2>
      ${escapeHtml(job.titulo)}
    </h2>

    <!-- FOTO E BIOGRAFIA DA EMPRESA -->

    <div class="job-company-profile">

      ${avatarEmpresa}

      <div class="job-company-info">

        <p class="company">
          ♧ &nbsp;${escapeHtml(job.empresa)}
        </p>

        ${bioEmpresa}

      </div>

    </div>

    <!-- DESCRIÇÃO DA VAGA -->

    <p class="description">
      ${escapeHtml(job.descricao)}
    </p>

    <!-- HABILIDADES NECESSÁRIAS -->

    <div class="chips">

      ${skillsList(job.habilidades).map(skill =>
        `<span>${escapeHtml(skill)}</span>`
      ).join("")}

    </div>

    <!-- INFORMAÇÕES DA VAGA -->

    <div class="job-facts">

      <div>

        <small>▤ Orçamento</small>

        <strong>
          ${currency(job.orcamentoMinimo)}
          –
          ${currency(job.orcamentoMaximo)}
        </strong>

      </div>

      <div>

        <small>◷ Prazo</small>

        <strong>
          ${job.prazoDias} dias
        </strong>

      </div>

      <div>

        <small>Pagamento</small>

        <strong>
          ${escapeHtml(job.tipoPagamento)}
        </strong>

      </div>

      <div>

        <small>Experiência</small>

        <strong>
          ${escapeHtml(job.experiencia)}
        </strong>

      </div>

    </div>

    <!-- RODAPÉ DO CARD -->

    <div class="card-footer">

      <small>
        ▦ Publicada em
        ${escapeHtml(formatDate(job.dataCriacao))}
      </small>

      <button
        class="text-button"
        id="job-details"
        type="button">

        Ver detalhes

      </button>

    </div>

  `;

  // Abre o modal com os detalhes completos.
  $("#job-details").addEventListener("click", () => {

    openJobDetails(job);

  });

}

// =====================================
// REMOVER VAGA DA LISTA
// =====================================

// Chamado pelo application-form.js
// depois que uma candidatura é salva.

function removeJobFromList(jobId) {

  availableJobs = availableJobs.filter(
    job => job.id !== jobId
  );

  renderJob();

}

// =====================================
// RECUSAR OU DEMONSTRAR INTERESSE
// =====================================

async function decideJob(interested) {

  const job = availableJobs[0];

  if (!job || jobBusy) return;

  // Se demonstrar interesse, abre o formulário.
  if (interested) {

    openApplicationForm(job);

    return;

  }

  jobBusy = true;

  try {

    await api("/api/interesses", {

      method: "POST",

      body: {
        projetoId: job.id,
        tipo: "Rejeicao"
      }

    });

    showToast(
      "Vaga descartada",
      job.titulo,
      "reject"
    );

    removeJobFromList(job.id);

  } catch (error) {

    showApiError(error);

  } finally {

    jobBusy = false;

  }

}

// =====================================
// INICIALIZAÇÃO DA TELA
// =====================================

const faltando = arquivosDesatualizados();

if (faltando.length) {

  showLoadError({

    message:
      "A página está com arquivos desatualizados ou faltando: " +
      faltando.join(", ") +
      ". Atualize os arquivos e recarregue com Ctrl+F5."

  });

} else {

  // Botão para rejeitar a vaga.
  document.getElementById("job-reject")
    ?.addEventListener("click", () => decideJob(false));

  // Botão para demonstrar interesse.
  document.getElementById("job-like")
    ?.addEventListener("click", () => decideJob(true));

  // Botão de procurar vagas no estado vazio.
  document.getElementById("empty-find-jobs")
    ?.addEventListener("click", () => {

      location.href = "/Home/Candidaturas";

    });

  // Recupera as vagas anteriormente descartadas.
  document.getElementById("empty-reset-jobs")
    ?.addEventListener("click", async () => {

      try {

        const result = await api(
          "/api/interesses/descartadas",
          { method: "DELETE" }
        );

        showToast(
          "Pronto!",
          result?.mensagem || "",
          "info"
        );

        await loadJobs();

      } catch (error) {

        showApiError(error);

      }

    });

  // Botão para tentar carregar novamente.
  document.getElementById("jobs-retry")
    ?.addEventListener("click", loadJobs);

  // Arrastar o cartão da vaga.
  addSwipe(
    $("#job-card"),
    () => decideJob(false),
    () => decideJob(true)
  );

  // Carrega as vagas ao abrir a tela.
  loadJobs();

}
