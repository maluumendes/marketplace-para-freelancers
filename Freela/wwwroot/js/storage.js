/* =========================================================
   ESTADO DA TELA
   Vagas, candidaturas, interesses e matches ficam no BANCO (via API).
   No navegador sobra só o que é da tela aberta e o aviso (toast)
   que precisa sobreviver à troca de página.
   ========================================================= */

const STORAGE = window.sessionStorage;

const KEYS = {
  pendingToast: "freela-pending-toast"
};

function readStore(key, fallback) {
  try {
    const raw = STORAGE.getItem(key);
    return raw === null ? fallback : JSON.parse(raw);
  } catch (error) {
    return fallback;
  }
}

function writeStore(key, value) {
  try {
    STORAGE.setItem(key, JSON.stringify(value));
  } catch (error) {
    /* armazenamento indisponível: segue sem salvar */
  }
}

/* ESTADO */

let activeDetailJob = null;   // vaga aberta no modal de detalhes/candidatura

// Vaga que a tela "Candidatos" deve abrir selecionada
// (gravada por "Ver candidatos" em Meus projetos / Detalhes)
const SELECTED_PROJECT_KEY = "freela-selected-project";

/* NOTIFICAÇÃO QUE SOBREVIVE À TROCA DE PÁGINA */

function queueToast(title, message, type = "success") {
  writeStore(KEYS.pendingToast, { title, message, type });
}

function flushPendingToast() {
  const toast = readStore(KEYS.pendingToast, null);

  if (!toast) return;

  try {
    STORAGE.removeItem(KEYS.pendingToast);
  } catch (error) {
    /* ignora */
  }

  showToast(toast.title, toast.message, toast.type);
}