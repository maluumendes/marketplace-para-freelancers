/* CANDIDATAR-SE (modal "Enviar candidatura")
   POST /api/candidaturas
   Enviar a candidatura É o interesse do freelancer na vaga.
   Se a empresa já tinha demonstrado interesse, a API devolve match = true. */

let applicationBusy = false;

function openApplicationForm(job) {
  activeDetailJob = job;

  $("#modal-job-title").textContent = job.titulo;
  $("#presentation").value = "";
  // Sugestões a partir da vaga (o freelancer pode mudar)
  $("#proposal").value = String(job.orcamentoMinimo || "");
  $("#deadline").value = String(job.prazoDias || "");

  $("#application-modal").showModal();
}

$("#application-form").addEventListener("submit", async event => {
  event.preventDefault();

  const job = activeDetailJob;

  if (!job || applicationBusy) return;

  const proposal = Number($("#proposal").value);
  const deadline = Number($("#deadline").value);
  const message = $("#presentation").value.trim();

  // Mesma regra do CriarCandidaturaDto (o servidor confere de novo)
  if (
    message.length < 10 ||
    !Number.isFinite(proposal) ||
    proposal <= 0 ||
    !Number.isInteger(deadline) ||
    deadline <= 0
  ) {
    showToast(
      "Confira os dados",
      "Escreva uma apresentação (mín. 10 caracteres) e informe proposta e prazo.",
      "info"
    );

    return;
  }

  const submitButton = $("#application-form button[type=submit]");
  applicationBusy = true;
  submitButton.disabled = true;

  try {
    const result = await api("/api/candidaturas", {
      method: "POST",
      body: {
        projetoId: job.id,
        mensagemApresentacao: message,
        valorProposto: proposal,
        prazoDias: deadline
      }
    });

    $("#application-modal").close();

    if (typeof removeJobFromList === "function") {
      removeJobFromList(job.id);   // tela Procurar vagas: mostra a próxima vaga
    }

    if (typeof onApplicationSent === "function") {
      onApplicationSent(job);      // outras telas (ex.: Detalhes do projeto)
    }

    showToast(
      result?.match ? "Deu match! 🎉" : "Candidatura enviada!",
      job.titulo,
      "success"
    );
  } catch (error) {
    // 409 = já tinha candidatura: tira a vaga da lista mesmo assim
    if (error.status === 409) {
      $("#application-modal").close();
      if (typeof removeJobFromList === "function") removeJobFromList(job.id);
    }

    showApiError(error);
  } finally {
    applicationBusy = false;
    submitButton.disabled = false;
  }
});