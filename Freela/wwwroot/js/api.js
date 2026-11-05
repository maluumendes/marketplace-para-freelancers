/* =========================================================
   COMUNICAÇÃO COM A API (fetch)
   ÚNICA função de acesso à API do sistema: telas de vagas
   (Integrante 2) e de candidaturas/matches (Integrante 3).
   Ela cuida das 4 partes do fetch (URL, método, headers e body)
   e dos erros, para cada tela não precisar repetir esse código.

   O login é pelo cookie do ASP.NET: o navegador envia sozinho
   em toda requisição para o mesmo site. Nas que alteram dados
   vai também o token antiforgery (proteção contra CSRF).
   ========================================================= */

class ApiError extends Error {
  constructor(message, status) {
    super(message);
    this.status = status;
  }
}

async function api(url, { method = "GET", body } = {}) {
  const options = {
    method,
    headers: { "Accept": "application/json" },
    credentials: "same-origin"
  };

  // Token antiforgery (gerado pelo _Layout) nas requisições que alteram dados.
  // É o mesmo header que o projetos.js do Integrante 2 usa.
  if (method !== "GET") {
    const token = document.querySelector('[name="__RequestVerificationToken"]');
    if (token) options.headers["X-CSRF-TOKEN"] = token.value;
  }

  if (body !== undefined) {
    options.headers["Content-Type"] = "application/json";
    options.body = JSON.stringify(body);
  }

  let response;

  try {
    response = await fetch(url, options);
  } catch (error) {
    throw new ApiError("Sem conexão com o servidor.", 0);
  }

  // Algumas respostas não têm corpo (ex.: 204)
  const text = await response.text();
  let data = null;

  try {
    data = text ? JSON.parse(text) : null;
  } catch (error) {
    data = null;
  }

  // Sessão expirou: volta para o login
  if (response.status === 401) {
    location.href = "/Conta/Login";
    throw new ApiError("Faça login para continuar.", 401);
  }

  if (!response.ok) {
    throw new ApiError(errorMessage(data, response.status), response.status);
  }

  return data;
}

// A API devolve { mensagem: "..." } nos nossos erros e
// { errors: { Campo: ["..."] } } quando a validação do DTO falha.
function errorMessage(data, status) {
  if (data?.mensagem) return data.mensagem;

  // Validação: mostra todas as mensagens juntas (ex.: título e descrição inválidos)
  if (data?.errors) {
    const all = Object.values(data.errors).flat().join(" ");
    if (all) return all;
  }

  // 500: exceção no servidor (no terminal do "dotnet run" aparece o motivo).
  // A causa mais comum é o banco desatualizado: abra /teste-banco para conferir.
  if (status >= 500) return `Erro no servidor (${status}). Abra /teste-banco para conferir o banco de dados.`;

  if (status === 403) return "Você não tem permissão para esta ação.";
  if (status === 404) return "Não encontrado.";

  return data?.title || `Erro inesperado (${status}).`;
}

function showApiError(error, title = "Não foi possível concluir") {
  if (error?.status === 401) return;   // já está indo para o login

  showToast(title, error?.message || "Tente novamente.", "reject");
}