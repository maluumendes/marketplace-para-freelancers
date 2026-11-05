// ========================================
// RESUMO DAS ATIVIDADES DO USUÁRIO
// Integrante 4
// ========================================

// Local onde os cards do resumo serão exibidos.
const resumoContainer = document.getElementById("painel-resumo");

// Busca os dados do usuário autenticado.
async function carregarResumoPainel() {

    // Se a tela não possuir esse elemento, não executa.
    if (!resumoContainer) return;

    try {

        // Consulta o endpoint criado no PainelController.
        const resumo = await api("/api/painel/resumo");

        // Decide quais informações mostrar de acordo com o perfil.
        let metricas = [];

        if (resumo.tipoUsuario === "FREELANCER") {

            metricas = [
                ["Candidaturas ativas", resumo.candidaturasAtivas],
                ["Aguardando resposta", resumo.pendentes],
                ["Aceitas", resumo.aceitas],
                ["Não selecionadas", resumo.rejeitadas],
                ["Retiradas", resumo.retiradas],
                ["Matches", resumo.matches]
            ];

        } else if (resumo.tipoUsuario === "CLIENTE") {

            metricas = [
                ["Projetos publicados", resumo.projetosPublicados],
                ["Projetos abertos", resumo.projetosAbertos],
                ["Projetos encerrados", resumo.projetosEncerrados],
                ["Candidaturas recebidas", resumo.candidaturasRecebidas],
                ["Aguardando análise", resumo.candidaturasPendentes],
                ["Aceitas", resumo.candidaturasAceitas],
                ["Rejeitadas", resumo.candidaturasRejeitadas],
                ["Matches", resumo.matches]
            ];

        }

        // Limpa o conteúdo anterior.
        resumoContainer.replaceChildren();

        // Cria o título da seção.
        const titulo = document.createElement("h2");
        titulo.textContent = "Resumo das atividades";

        resumoContainer.appendChild(titulo);

        // Cria a grade dos cards.
        const grade = document.createElement("div");
        grade.className = "resumo-grid";

        // Cria um card para cada informação.
        metricas.forEach(([nome, quantidade]) => {

            const card = document.createElement("article");
            card.className = "resumo-card";

            const numero = document.createElement("strong");

            // Garante que o valor seja exibido como número.
            numero.textContent = Number(quantidade ?? 0).toLocaleString("pt-BR");

            const descricao = document.createElement("span");
            descricao.textContent = nome;

            card.appendChild(numero);
            card.appendChild(descricao);

            grade.appendChild(card);

        });

        resumoContainer.appendChild(grade);

    } catch (erro) {

        // Caso ocorra um erro na API, informa na tela.
        console.error("Erro ao carregar resumo:", erro);

        resumoContainer.textContent =
            "Não foi possível carregar o resumo das atividades.";

    }
}

// Executa o carregamento quando o arquivo é carregado.
carregarResumoPainel();