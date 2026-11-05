
// Funções utilizadas nas telas de projetos.
// As chamadas à API utilizam api() de wwwroot/js/api.js.

// =====================================
// FORMATAR ORÇAMENTO
// =====================================

function formatarOrcamento(valor) {

    return valor.toLocaleString("pt-BR", {
        style: "currency",
        currency: "BRL"
    });

}

// =====================================
// ADICIONAR TEXTO COM SEGURANÇA
// =====================================

function adicionarTexto(elemento, tipo, texto) {

    const campo = document.createElement(tipo);

    campo.textContent = texto;

    elemento.appendChild(campo);

}

// =====================================
// GERAR INICIAIS DA EMPRESA
// =====================================

function gerarIniciaisEmpresa(nome) {

    const partes = String(nome)
        .split(" ")
        .filter(p => p.length > 2);

    if (partes.length === 0) {
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
// CRIAR CARD DO PROJETO
// =====================================

function criarCardProjeto(projeto, completo = false) {

    const card = document.createElement("article");

    card.className = "projeto-card";

    // =====================================
    // PERFIL DA EMPRESA
    // =====================================

    const perfilEmpresa = document.createElement("div");

    perfilEmpresa.className = "projeto-empresa-perfil";

    const foto = projeto.empresaFotoPerfil
        ? safeUrl(projeto.empresaFotoPerfil)
        : null;

    if (foto) {

        const imagem = document.createElement("img");

        imagem.src = foto;

        imagem.alt = "Foto da empresa";

        imagem.className = "projeto-empresa-avatar";

        perfilEmpresa.appendChild(imagem);

    } else {

        // Caso não tenha foto, mostra as iniciais.
        const avatar = document.createElement("div");

        avatar.className =
            "projeto-empresa-avatar projeto-iniciais";

        avatar.textContent =
            gerarIniciaisEmpresa(projeto.empresa);

        perfilEmpresa.appendChild(avatar);

    }

    const informacoesEmpresa = document.createElement("div");

    informacoesEmpresa.className = "projeto-empresa-info";

    adicionarTexto(
        informacoesEmpresa,
        "strong",
        projeto.empresa
    );

    adicionarTexto(
        informacoesEmpresa,
        "p",
        projeto.empresaBio ||
        "Esta empresa ainda não escreveu uma biografia."
    );

    perfilEmpresa.appendChild(informacoesEmpresa);

    card.appendChild(perfilEmpresa);

    // =====================================
    // INFORMAÇÕES DA VAGA
    // =====================================

    adicionarTexto(card, "h2", projeto.titulo);

    adicionarTexto(
        card,
        "p",
        projeto.empresa + " • " + projeto.categoria
    );

    let descricao = projeto.descricao;

    if (!completo && descricao.length > 200) {
        descricao = descricao.substring(0, 200) + "...";
    }

    adicionarTexto(card, "p", descricao);

    adicionarTexto(
        card,
        "p",
        formatarOrcamento(projeto.orcamentoMinimo) +
        " a " +
        formatarOrcamento(projeto.orcamentoMaximo)
    );

    adicionarTexto(
        card,
        "p",
        "Pagamento: " + projeto.tipoPagamento
    );

    adicionarTexto(
        card,
        "p",
        "Prazo: " + projeto.prazoDias + " dias"
    );

    adicionarTexto(
        card,
        "p",
        "Habilidades: " + projeto.habilidades
    );

    adicionarTexto(
        card,
        "p",
        "Experiência: " + projeto.experiencia
    );

    adicionarTexto(
        card,
        "p",
        "Status: " + projeto.status
    );

    return card;

}
