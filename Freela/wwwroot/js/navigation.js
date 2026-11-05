/* =========================================================
   MENU NO CELULAR
   (o perfil do usuário agora vem do login, feito no servidor;
   por isso a escolha de perfil guardada no navegador foi removida)
   ========================================================= */

const menuToggle = $("#menu-toggle");

menuToggle?.addEventListener("click", () => {
  const isOpen = $("#main-nav").classList.toggle("open");

  menuToggle.setAttribute("aria-expanded", String(isOpen));
});

/* Mostra avisos pendentes da página anterior */

flushPendingToast();
