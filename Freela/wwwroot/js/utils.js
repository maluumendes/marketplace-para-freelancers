/* FUNÇÕES AUXILIARES */

const $ = selector => document.querySelector(selector);

const $$ = selector => [...document.querySelectorAll(selector)];

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, character => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#039;"
  })[character]);
}

function currency(value) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    maximumFractionDigits: 2
  }).format(value);
}

// Formata uma data (a da API vem como texto ISO: "2026-11-01T17:06:00")
function formatDate(value = new Date()) {
  const date = value instanceof Date ? value : new Date(value);

  if (Number.isNaN(date.getTime())) return "";

  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "short",
    year: "numeric"
  }).format(date);
}

// Só aceita links http/https (evita "javascript:..." vindo do banco)
function safeUrl(value) {
  try {
    const url = new URL(value, location.origin);
    return url.protocol === "http:" || url.protocol === "https:" ? url.href : null;
  } catch (error) {
    return null;
  }
}

/* NOTIFICAÇÕES (TOAST) */

function showToast(title, message, type = "success") {
  const toastRegion = $("#toast-region");

  if (!toastRegion) return;

  const toast = document.createElement("div");

  toast.className =
    "toast" + (type === "reject" ? " reject-toast" : "");

  const icon = document.createElement("span");
  icon.className = "toast-icon";
  icon.setAttribute("aria-hidden", "true");
  icon.textContent =
    type === "success" ? "✓" :
    type === "reject" ? "×" : "i";

  const content = document.createElement("span");
  const strong = document.createElement("strong");

  strong.textContent = title;
  content.append(strong, document.createTextNode(message));

  toast.append(icon, content);
  toastRegion.appendChild(toast);

  window.setTimeout(() => {
    toast.remove();
  }, 4000);
}

/* CAMPOS DA VAGA */

// A API devolve as habilidades como texto "React, C#, SQL" (formato do Integrante 2)
function skillsList(value) {
  return String(value || "")
    .split(",")
    .map(skill => skill.trim())
    .filter(Boolean);
}