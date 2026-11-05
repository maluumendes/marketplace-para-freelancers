/* FECHAR MODAIS */

$$("[data-close]").forEach(button => {
  button.addEventListener("click", () => {
    document.getElementById(button.dataset.close)?.close();
  });
});

$$("dialog").forEach(dialog => {
  dialog.addEventListener("click", event => {
    if (event.target !== dialog) return;

    dialog.close();
  });
});