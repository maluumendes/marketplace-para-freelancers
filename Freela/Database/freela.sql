-- =========================================================
-- PROJETO FLUXO - BANCO DE DADOS
-- =========================================================
-- Como executar:
-- mysql -u root -p < freela.sql
--
-- IMPORTANTE:
-- Este script recria o banco do zero.
-- Todos os dados anteriores serão apagados.
-- =========================================================

SET NAMES utf8mb4;

-- =========================================================
-- CRIAÇÃO DO BANCO
-- =========================================================

DROP DATABASE IF EXISTS freela;

CREATE DATABASE freela
CHARACTER SET utf8mb4
COLLATE utf8mb4_0900_ai_ci;

USE freela;

-- =========================================================
-- CONTAS
-- =========================================================

-- 1. Usuários (login / autenticação)

CREATE TABLE Usuarios (
    Id            INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Email         VARCHAR(150) NOT NULL,
    Senha         VARCHAR(255) NOT NULL,
    TipoUsuario   VARCHAR(20)  NOT NULL,
    DataCadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT UQ_Usuarios_Email UNIQUE (Email),

    CONSTRAINT CK_Usuarios_TipoUsuario
        CHECK (TipoUsuario IN ('CLIENTE', 'FREELANCER'))
);

-- =========================================================
-- 2. Clientes / empresas
-- Relação 1:1 com Usuarios
-- =========================================================

CREATE TABLE Clientes (
    Id                 INT          NOT NULL PRIMARY KEY,
    NomeOuRazaoSocial  VARCHAR(150) NOT NULL,
    CpfOuCnpj          VARCHAR(20)  NOT NULL,
    Telefone           VARCHAR(20)  NULL,
    Bio                TEXT         NULL,

    -- NOVO: caminho da foto ou logo da empresa
    FotoPerfil         VARCHAR(255) NULL,

    CONSTRAINT UQ_Clientes_CpfOuCnpj UNIQUE (CpfOuCnpj),

    CONSTRAINT FK_Clientes_Usuarios
        FOREIGN KEY (Id)
        REFERENCES Usuarios(Id)
        ON DELETE CASCADE
);

-- =========================================================
-- 3. Freelancers
-- Relação 1:1 com Usuarios
-- =========================================================

CREATE TABLE Freelancers (
    Id             INT          NOT NULL PRIMARY KEY,
    NomeCompleto   VARCHAR(150) NOT NULL,
    Cpf            VARCHAR(14)  NOT NULL,
    Telefone       VARCHAR(20)  NULL,
    Bio            TEXT         NULL,
    LinkPortfolio  VARCHAR(255) NULL,

    -- NOVO: caminho da foto de perfil
    FotoPerfil     VARCHAR(255) NULL,

    CONSTRAINT UQ_Freelancers_Cpf UNIQUE (Cpf),

    CONSTRAINT FK_Freelancers_Usuarios
        FOREIGN KEY (Id)
        REFERENCES Usuarios(Id)
        ON DELETE CASCADE
);

-- =========================================================
-- TABELAS DE APOIO
-- =========================================================

-- 4. Categorias das vagas

CREATE TABLE Categorias (
    Id    INT         NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome  VARCHAR(50) NOT NULL,

    CONSTRAINT UQ_Categorias_Nome UNIQUE (Nome)
);

-- 5. Habilidades

CREATE TABLE Habilidades (
    Id    INT         NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nome  VARCHAR(50) NOT NULL,

    CONSTRAINT UQ_Habilidades_Nome UNIQUE (Nome)
);

-- 6. Habilidades de cada freelancer (N:M)

CREATE TABLE FreelancerHabilidades (
    FreelancerId  INT NOT NULL,
    HabilidadeId  INT NOT NULL,

    PRIMARY KEY (FreelancerId, HabilidadeId),

    CONSTRAINT FK_FH_Freelancers
        FOREIGN KEY (FreelancerId)
        REFERENCES Freelancers(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_FH_Habilidades
        FOREIGN KEY (HabilidadeId)
        REFERENCES Habilidades(Id)
        ON DELETE CASCADE
);

-- =========================================================
-- VAGAS (INTEGRANTE 2)
-- =========================================================

-- 7. Projetos publicados pelas empresas

CREATE TABLE Projetos (
    Id               INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ClienteId        INT           NOT NULL,
    CategoriaId      INT           NOT NULL,
    Titulo           VARCHAR(100)  NOT NULL,
    Descricao        TEXT          NOT NULL,
    OrcamentoMinimo  DECIMAL(12,2) NOT NULL,
    OrcamentoMaximo  DECIMAL(12,2) NOT NULL,
    PrazoDias        INT           NOT NULL,
    TipoPagamento    VARCHAR(30)   NOT NULL,
    Experiencia      VARCHAR(30)   NOT NULL,
    Status           VARCHAR(20)   NOT NULL DEFAULT 'ABERTO',
    DataCriacao      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Projetos_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id),

    CONSTRAINT FK_Projetos_Categorias
        FOREIGN KEY (CategoriaId)
        REFERENCES Categorias(Id),

    CONSTRAINT CK_Projetos_Orcamento
        CHECK (
            OrcamentoMinimo >= 0
            AND OrcamentoMaximo >= OrcamentoMinimo
        ),

    CONSTRAINT CK_Projetos_Prazo
        CHECK (PrazoDias BETWEEN 1 AND 3650),

    CONSTRAINT CK_Projetos_TipoPagamento
        CHECK (
            TipoPagamento IN (
                'Valor fixo',
                'Por hora',
                'Por entrega'
            )
        ),

    CONSTRAINT CK_Projetos_Experiencia
        CHECK (
            Experiencia IN (
                'Iniciante',
                'Intermediário',
                'Avançado'
            )
        ),

    CONSTRAINT CK_Projetos_Status
        CHECK (Status IN ('ABERTO', 'ENCERRADO')),

    INDEX IX_Projetos_Status_CategoriaId (Status, CategoriaId)
);

-- =========================================================
-- 8. Habilidades pedidas em cada projeto (N:M)
-- =========================================================

CREATE TABLE ProjetoHabilidades (
    ProjetoId     INT NOT NULL,
    HabilidadeId  INT NOT NULL,

    PRIMARY KEY (ProjetoId, HabilidadeId),

    CONSTRAINT FK_PH_Projetos
        FOREIGN KEY (ProjetoId)
        REFERENCES Projetos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_PH_Habilidades
        FOREIGN KEY (HabilidadeId)
        REFERENCES Habilidades(Id)
        ON DELETE CASCADE
);

-- =========================================================
-- INTERAÇÕES (INTEGRANTE 3)
-- =========================================================

-- 9. Candidaturas

CREATE TABLE Candidaturas (
    Id                    INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProjetoId             INT           NOT NULL,
    FreelancerId          INT           NOT NULL,
    MensagemApresentacao  VARCHAR(1000) NOT NULL,
    ValorProposto         DECIMAL(12,2) NOT NULL,
    PrazoDias             INT           NOT NULL,
    Status                VARCHAR(20)   NOT NULL DEFAULT 'PENDENTE',
    DataCandidatura       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    DataResposta          DATETIME      NULL,

    CONSTRAINT UQ_Projeto_Freelancer
        UNIQUE (ProjetoId, FreelancerId),

    CONSTRAINT FK_Candidaturas_Projetos
        FOREIGN KEY (ProjetoId)
        REFERENCES Projetos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Candidaturas_Freelancers
        FOREIGN KEY (FreelancerId)
        REFERENCES Freelancers(Id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Candidaturas_Status
        CHECK (
            Status IN (
                'PENDENTE',
                'ACEITA',
                'REJEITADA',
                'RETIRADA'
            )
        ),

    CONSTRAINT CK_Candidaturas_Valor
        CHECK (ValorProposto > 0),

    CONSTRAINT CK_Candidaturas_Prazo
        CHECK (PrazoDias BETWEEN 1 AND 3650)
);

-- =========================================================
-- 10. Interesses
-- =========================================================

CREATE TABLE Interesses (
    Id            INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProjetoId     INT          NOT NULL,
    FreelancerId  INT          NOT NULL,
    Origem        VARCHAR(20)  NOT NULL,
    Tipo          VARCHAR(20)  NOT NULL,
    DataRegistro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT UQ_Interesses_Par_Origem
        UNIQUE (ProjetoId, FreelancerId, Origem),

    CONSTRAINT FK_Interesses_Projetos
        FOREIGN KEY (ProjetoId)
        REFERENCES Projetos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Interesses_Freelancers
        FOREIGN KEY (FreelancerId)
        REFERENCES Freelancers(Id)
        ON DELETE CASCADE,

    CONSTRAINT CK_Interesses_Origem
        CHECK (Origem IN ('FREELANCER', 'CLIENTE')),

    CONSTRAINT CK_Interesses_Tipo
        CHECK (Tipo IN ('INTERESSE', 'REJEICAO'))
);

-- =========================================================
-- 11. Matches
-- =========================================================

CREATE TABLE Matches (
    Id             INT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ProjetoId      INT       NOT NULL,
    FreelancerId   INT       NOT NULL,
    CandidaturaId  INT       NOT NULL,
    DataMatch      DATETIME  NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT UQ_Matches_Par
        UNIQUE (ProjetoId, FreelancerId),

    CONSTRAINT UQ_Matches_Candidatura
        UNIQUE (CandidaturaId),

    CONSTRAINT FK_Matches_Projetos
        FOREIGN KEY (ProjetoId)
        REFERENCES Projetos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Matches_Freelancers
        FOREIGN KEY (FreelancerId)
        REFERENCES Freelancers(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_Matches_Candidaturas
        FOREIGN KEY (CandidaturaId)
        REFERENCES Candidaturas(Id)
        ON DELETE CASCADE
);

-- =========================================================
-- DADOS INICIAIS
-- =========================================================

INSERT INTO Categorias (Nome) VALUES
    ('Desenvolvimento Web'),
    ('Design'),
    ('Conteúdo'),
    ('Audiovisual'),
    ('Marketing'),
    ('Dados'),
    ('Backend'),
    ('Outros');

-- =========================================================
-- DADOS DE TESTE (desenvolvimento)
-- Para um banco "limpo", apague daqui até o fim da seção.
--
-- Senha de TODAS as contas de teste: Freela@123
--   Empresas (CLIENTE):     xyz@freela.com, aurora@freela.com, norte@freela.com, nuvem@freela.com
--   Freelancers:            joao@freela.com, marina.duarte@freela.com, lucas@freela.com,
--                           marina.costa@freela.com, ana@freela.com, pedro@freela.com
--
-- Situações já prontas para testar (16 candidaturas):
--   Empresas (todas com candidatos esperando resposta):
--   - xyz:    "Desenvolvedor Web Full Stack" com 3 pendentes; "Dashboard" e "Gateway" com 1 pendente cada;
--             MATCH com João no "Gateway de pagamento"
--   - aurora: "Identidade visual" e "UI/UX" com 1 pendente cada; MATCH com Marina Costa no "UI/UX"
--   - norte:  "Redator SEO" e "Social media" (2) com pendentes; Pedro REJEITADO em "Edição de vídeos"
--   - nuvem:  "App mobile" com 2 pendentes; MATCH com Lucas na "Landing page";
--             "Migração de servidor" ENCERRADA (não aparece na busca)
--   Freelancers: João tem 3 pendentes + 1 match e descartou (×) "Social media"
-- As senhas estão em hash BCrypt, como o ContaController grava no cadastro.
-- =========================================================

-- Contas
INSERT INTO Usuarios (Id, Email, Senha, TipoUsuario, DataCadastro) VALUES
    (1, 'xyz@freela.com', '$2b$11$EjrwYL30CCCAselfQNkfn.eVTCzmDN7J0PY4QB0NVJUoGmIE7Mmim', 'CLIENTE', '2026-10-01 09:00:00'),
    (2, 'aurora@freela.com', '$2b$11$Bhpi2aoYIAIYTS1i/szdIe0VHPjW/ZH0e7.A.L9IfSIOj1IFWNB/K', 'CLIENTE', '2026-10-01 09:00:00'),
    (3, 'norte@freela.com', '$2b$11$6MTuDQ5m1UOTJ/pP2lx9E.VMAT3AQkG/7E0SpUlWbUyGh4dekuCQe', 'CLIENTE', '2026-10-01 09:00:00'),
    (4, 'nuvem@freela.com', '$2b$11$Bb77aahti.D5zlhmrJDsju5NgYDMB81DzxUJ9otPuqQ5DcOrTRB/q', 'CLIENTE', '2026-10-01 09:00:00'),
    (5, 'joao@freela.com', '$2b$11$PpZv..SKx.pQALJSLjuq5eDyJK26pGLJMyk7/2SuF0RbIBHKrchsK', 'FREELANCER', '2026-10-02 09:00:00'),
    (6, 'marina.duarte@freela.com', '$2b$11$rAfKTeWoXNsyHCy4CTgHrObx.DXp29498NGsnx/RMjUSr02fXSxvi', 'FREELANCER', '2026-10-02 09:00:00'),
    (7, 'lucas@freela.com', '$2b$11$.NqCMGFQUmHAYdFuz6YtfuHz9UjpOHP9vtmteo4XBZkXxWn6DxsWi', 'FREELANCER', '2026-10-02 09:00:00'),
    (8, 'marina.costa@freela.com', '$2b$11$oQZ.IoABznp8T3XUdWo.buhEl96dToWlFzRP5KkVBdXA7YvPwG2NS', 'FREELANCER', '2026-10-02 09:00:00'),
    (9, 'ana@freela.com', '$2b$11$p3pSQ5/421/iVzxXSiH5ueJ5mZg8wl7JgYS/eP6BoHezYgiWufJu6', 'FREELANCER', '2026-10-02 09:00:00'),
    (10, 'pedro@freela.com', '$2b$11$QgObrSlLxgir2psnWTb7P.q2.L2MarJMjPpx.5QdWdS3Qv.ChsbK.', 'FREELANCER', '2026-10-02 09:00:00');

INSERT INTO Clientes (Id, NomeOuRazaoSocial, CpfOuCnpj, Telefone, Bio) VALUES
    (1, 'Empresa XYZ Tecnologia', '11222333000181', '(41) 3333-1001', 'Software sob medida para varejo e logística.'),
    (2, 'Estúdio Aurora', '22333444000192', '(41) 3333-1002', 'Estúdio de design focado em produtos digitais e marcas.'),
    (3, 'Agência Norte', '33444555000103', '(11) 3333-1003', 'Agência de marketing digital e conteúdo.'),
    (4, 'Nuvem Digital', '44555666000114', '(21) 3333-1004', 'Startup de soluções em nuvem para pequenos negócios.');

INSERT INTO Freelancers (Id, NomeCompleto, Cpf, Telefone, Bio, LinkPortfolio) VALUES
    (5, 'João da Silva', '11122233344', '(41) 99999-0005', 'Desenvolvedor Full Stack. Trabalho com aplicações web de ponta a ponta, com foco em performance e código limpo.', 'https://portfolio.example.com/joao'),
    (6, 'Marina Duarte', '22233344455', '(41) 99999-0006', 'Engenheira de Software. Especialista em integrações e arquitetura .NET com front-end moderno.', 'https://portfolio.example.com/marina-duarte'),
    (7, 'Lucas Mendes', '33344455566', '(41) 99999-0007', 'Desenvolvedor Front-end. Crio interfaces responsivas e acessíveis com atenção aos detalhes.', 'https://portfolio.example.com/lucas'),
    (8, 'Marina Costa', '44455566677', '(41) 99999-0008', 'Designer de Produto. Transformo ideias em experiências digitais intuitivas, acessíveis e consistentes.', 'https://portfolio.example.com/marina-costa'),
    (9, 'Ana Oliveira', '55566677788', '(41) 99999-0009', 'Redatora e estrategista de conteúdo. Textos otimizados para busca sem perder a naturalidade.', NULL),
    (10, 'Pedro Martins', '66677788899', '(41) 99999-0010', 'Editor de vídeo. Transformo gravações em vídeos curtos e dinâmicos para redes sociais.', 'https://portfolio.example.com/pedro');

-- Habilidades (usadas pelos freelancers e pelas vagas)
INSERT INTO Habilidades (Id, Nome) VALUES
    (1, 'React'),
    (2, 'C#'),
    (3, 'SQL'),
    (4, 'Azure'),
    (5, '.NET'),
    (6, 'Docker'),
    (7, 'HTML'),
    (8, 'CSS'),
    (9, 'JavaScript'),
    (10, 'Figma'),
    (11, 'UI/UX'),
    (12, 'Design System'),
    (13, 'SEO'),
    (14, 'Copywriting'),
    (15, 'Pesquisa'),
    (16, 'Premiere'),
    (17, 'After Effects'),
    (18, 'Motion'),
    (19, 'SQL Server'),
    (20, 'Tailwind'),
    (21, 'Webhooks'),
    (22, 'Power BI'),
    (23, 'Excel'),
    (24, 'Prototipagem'),
    (25, 'Branding'),
    (26, 'Illustrator'),
    (27, 'Instagram'),
    (28, 'Canva'),
    (29, 'React Native'),
    (30, 'TypeScript'),
    (31, 'Firebase'),
    (32, 'Linux');

INSERT INTO FreelancerHabilidades (FreelancerId, HabilidadeId) VALUES
    (5, 1),
    (5, 2),
    (5, 3),
    (5, 4),
    (6, 2),
    (6, 5),
    (6, 1),
    (6, 6),
    (7, 7),
    (7, 8),
    (7, 9),
    (7, 1),
    (8, 10),
    (8, 11),
    (8, 12),
    (9, 13),
    (9, 14),
    (9, 15),
    (10, 16),
    (10, 17),
    (10, 18);

-- Vagas (a categoria é buscada pelo nome, cadastrada acima)
INSERT INTO Projetos (Id, ClienteId, CategoriaId, Titulo, Descricao, OrcamentoMinimo, OrcamentoMaximo, PrazoDias, TipoPagamento, Experiencia, Status, DataCriacao) VALUES
    (1, 1, (SELECT Id FROM Categorias WHERE Nome = 'Desenvolvimento Web'), 'Desenvolvedor Web Full Stack',
     'Criar uma aplicação web para gestão de pedidos com painel administrativo, autenticação e relatórios. O projeto já possui protótipo em Figma e API parcialmente implementada em C#.',
     2000.00, 3000.00, 30, 'Valor fixo', 'Intermediário', 'ABERTO', '2026-10-20 09:00:00'),
    (2, 1, (SELECT Id FROM Categorias WHERE Nome = 'Backend'), 'Integração de gateway de pagamento',
     'Integrar gateway de pagamento com o checkout existente, incluindo webhooks, conciliação e testes automatizados.',
     3000.00, 4500.00, 25, 'Valor fixo', 'Avançado', 'ABERTO', '2026-10-22 10:30:00'),
    (3, 1, (SELECT Id FROM Categorias WHERE Nome = 'Dados'), 'Dashboard de análise de vendas',
     'Construir painel de indicadores comerciais, com filtros, gráficos interativos e integração com banco de dados.',
     1800.00, 3200.00, 25, 'Valor fixo', 'Intermediário', 'ABERTO', '2026-10-24 14:00:00'),
    (4, 2, (SELECT Id FROM Categorias WHERE Nome = 'Design'), 'Designer de interfaces UI/UX',
     'Criar protótipos de alta fidelidade para uma plataforma de educação, com foco em acessibilidade e usabilidade.',
     1500.00, 2500.00, 20, 'Valor fixo', 'Intermediário', 'ABERTO', '2026-10-18 11:00:00'),
    (5, 2, (SELECT Id FROM Categorias WHERE Nome = 'Design'), 'Identidade visual para marca de cosméticos',
     'Desenvolvimento de logotipo, paleta de cores, tipografia e manual de marca para lançamento de linha premium.',
     1500.00, 2200.00, 21, 'Valor fixo', 'Avançado', 'ABERTO', '2026-10-26 16:00:00'),
    (6, 3, (SELECT Id FROM Categorias WHERE Nome = 'Conteúdo'), 'Redator de conteúdo SEO',
     'Produção de 12 artigos mensais otimizados para SEO no nicho de finanças pessoais, com pesquisa de palavras-chave.',
     80.00, 120.00, 30, 'Por hora', 'Iniciante', 'ABERTO', '2026-10-15 08:30:00'),
    (7, 3, (SELECT Id FROM Categorias WHERE Nome = 'Marketing'), 'Social media para marca de moda',
     'Planejamento de calendário editorial, criação de legendas e organização de publicações para Instagram.',
     900.00, 1400.00, 15, 'Por entrega', 'Iniciante', 'ABERTO', '2026-10-27 09:15:00'),
    (8, 3, (SELECT Id FROM Categorias WHERE Nome = 'Audiovisual'), 'Edição de vídeos para redes sociais',
     'Edição de 20 vídeos curtos por mês, com legendas, cortes dinâmicos e trilha sonora.',
     1200.00, 1800.00, 15, 'Por entrega', 'Intermediário', 'ABERTO', '2026-10-21 13:45:00'),
    (9, 4, (SELECT Id FROM Categorias WHERE Nome = 'Desenvolvimento Web'), 'Landing page para startup',
     'Criar uma landing page responsiva com formulário de contato, animações leves e otimização de desempenho.',
     700.00, 1300.00, 12, 'Valor fixo', 'Iniciante', 'ABERTO', '2026-10-19 17:00:00'),
    (10, 4, (SELECT Id FROM Categorias WHERE Nome = 'Desenvolvimento Web'), 'App mobile de agendamento',
     'Aplicativo para clientes agendarem horários, com notificações, pagamento integrado e área do profissional.',
     5000.00, 8000.00, 60, 'Por entrega', 'Avançado', 'ABERTO', '2026-10-28 10:00:00'),
    (11, 4, (SELECT Id FROM Categorias WHERE Nome = 'Backend'), 'Migração de servidor para a nuvem',
     'Migrar a aplicação atual para containers na nuvem, com pipeline de deploy automatizado.',
     2500.00, 4000.00, 20, 'Valor fixo', 'Avançado', 'ENCERRADO', '2026-10-05 09:00:00');

INSERT INTO ProjetoHabilidades (ProjetoId, HabilidadeId) VALUES
    (1, 1),
    (1, 2),
    (1, 19),
    (1, 20),
    (2, 2),
    (2, 5),
    (2, 21),
    (2, 4),
    (3, 3),
    (3, 22),
    (3, 23),
    (4, 10),
    (4, 11),
    (4, 24),
    (5, 25),
    (5, 26),
    (5, 10),
    (6, 13),
    (6, 14),
    (6, 15),
    (7, 27),
    (7, 28),
    (7, 14),
    (8, 16),
    (8, 17),
    (8, 18),
    (9, 7),
    (9, 8),
    (9, 9),
    (9, 13),
    (10, 29),
    (10, 30),
    (10, 31),
    (11, 6),
    (11, 4),
    (11, 32);

-- Candidaturas em situações diferentes
INSERT INTO Candidaturas (Id, ProjetoId, FreelancerId, MensagemApresentacao, ValorProposto, PrazoDias, Status, DataCandidatura, DataResposta) VALUES
    (1, 1, 5, 'Olá! Já entreguei três painéis administrativos parecidos. Posso começar essa semana.', 2500.00, 28, 'PENDENTE', '2026-10-28 09:10:00', NULL),
    (2, 1, 6, 'Tenho disponibilidade integral e experiência com relatórios em SQL Server.', 2900.00, 30, 'PENDENTE', '2026-10-29 15:20:00', NULL),
    (3, 1, 7, 'Tenho experiência com interfaces responsivas e posso começar após alinharmos os requisitos.', 2200.00, 21, 'PENDENTE', '2026-10-30 11:05:00', NULL),
    (4, 4, 8, 'Posso apresentar um conceito visual e um protótipo navegável na primeira semana.', 1800.00, 14, 'ACEITA', '2026-10-22 10:00:00', '2026-10-24 16:30:00'),
    (5, 9, 7, 'Faço a landing page com foco em desempenho e acessibilidade, com entregas parciais.', 1100.00, 10, 'ACEITA', '2026-10-23 08:40:00', '2026-10-25 09:00:00'),
    (6, 8, 10, 'Tenho experiência com conteúdo vertical e posso seguir a identidade da sua marca.', 1500.00, 15, 'REJEITADA', '2026-10-24 19:00:00', '2026-10-26 10:15:00'),
    (7, 6, 9, 'Posso montar uma pauta estratégica e entregar os primeiros artigos para aprovação.', 100.00, 30, 'PENDENTE', '2026-10-25 14:30:00', NULL),
    (8, 2, 6, 'Já integrei três gateways em .NET, incluindo webhooks e conciliação.', 4200.00, 25, 'PENDENTE', '2026-10-29 08:00:00', NULL);

-- Interesses: a candidatura é o ♡ do freelancer; aceitar/recusar é a decisão da empresa
INSERT INTO Interesses (ProjetoId, FreelancerId, Origem, Tipo, DataRegistro) VALUES
    (1, 5, 'FREELANCER', 'INTERESSE', '2026-10-28 09:10:00'),
    (1, 6, 'FREELANCER', 'INTERESSE', '2026-10-29 15:20:00'),
    (1, 7, 'FREELANCER', 'INTERESSE', '2026-10-30 11:05:00'),
    (4, 8, 'FREELANCER', 'INTERESSE', '2026-10-22 10:00:00'),
    (4, 8, 'CLIENTE', 'INTERESSE', '2026-10-24 16:30:00'),
    (9, 7, 'FREELANCER', 'INTERESSE', '2026-10-23 08:40:00'),
    (9, 7, 'CLIENTE', 'INTERESSE', '2026-10-25 09:00:00'),
    (8, 10, 'FREELANCER', 'INTERESSE', '2026-10-24 19:00:00'),
    (8, 10, 'CLIENTE', 'REJEICAO', '2026-10-26 10:15:00'),
    (6, 9, 'FREELANCER', 'INTERESSE', '2026-10-25 14:30:00'),
    (2, 6, 'FREELANCER', 'INTERESSE', '2026-10-29 08:00:00'),
    (7, 5, 'FREELANCER', 'REJEICAO', '2026-10-28 09:00:00');

-- Matches: interesse dos dois lados nas candidaturas ACEITAS
INSERT INTO Matches (ProjetoId, FreelancerId, CandidaturaId, DataMatch) VALUES
    (4, 8, 4, '2026-10-24 16:30:00'),
    (9, 7, 5, '2026-10-25 09:00:00');

-- ---------------------------------------------------------
-- Mais candidaturas de teste: todas as empresas têm candidatos pendentes
-- e todos os freelancers têm candidaturas enviadas
-- ---------------------------------------------------------
INSERT INTO Candidaturas (Id, ProjetoId, FreelancerId, MensagemApresentacao, ValorProposto, PrazoDias, Status, DataCandidatura, DataResposta) VALUES
    (9, 5, 8, 'Tenho experiência com marcas de beleza e posso entregar o manual de marca completo em três semanas.', 2000.00, 21, 'PENDENTE', '2026-10-29 10:20:00', NULL),
    (10, 4, 7, 'Faço o protótipo em Figma com foco em acessibilidade e já testo a navegação com usuários.', 2100.00, 18, 'PENDENTE', '2026-10-30 16:45:00', NULL),
    (11, 10, 5, 'Já publiquei dois apps de agendamento em React Native e posso integrar o pagamento e as notificações.', 7200.00, 55, 'PENDENTE', '2026-10-30 09:30:00', NULL),
    (12, 10, 6, 'Posso cuidar da API e das notificações, com testes automatizados desde o início.', 7800.00, 60, 'PENDENTE', '2026-10-31 14:10:00', NULL),
    (13, 7, 9, 'Monto o calendário editorial e entrego as legendas da primeira quinzena para aprovação.', 1200.00, 15, 'PENDENTE', '2026-10-29 18:00:00', NULL),
    (14, 3, 5, 'Construo o painel em Power BI com atualização automática a partir do banco de dados.', 2800.00, 22, 'PENDENTE', '2026-10-31 08:50:00', NULL),
    (15, 2, 5, 'Integrei gateways em três lojas virtuais e entrego webhooks e conciliação com testes.', 3900.00, 24, 'ACEITA', '2026-10-23 11:00:00', '2026-10-27 15:40:00'),
    (16, 7, 10, 'Posso criar vídeos curtos para os posts e cuidar das legendas também.', 1100.00, 15, 'PENDENTE', '2026-10-31 19:25:00', NULL);

INSERT INTO Interesses (ProjetoId, FreelancerId, Origem, Tipo, DataRegistro) VALUES
    (5, 8, 'FREELANCER', 'INTERESSE', '2026-10-29 10:20:00'),
    (4, 7, 'FREELANCER', 'INTERESSE', '2026-10-30 16:45:00'),
    (10, 5, 'FREELANCER', 'INTERESSE', '2026-10-30 09:30:00'),
    (10, 6, 'FREELANCER', 'INTERESSE', '2026-10-31 14:10:00'),
    (7, 9, 'FREELANCER', 'INTERESSE', '2026-10-29 18:00:00'),
    (3, 5, 'FREELANCER', 'INTERESSE', '2026-10-31 08:50:00'),
    (2, 5, 'FREELANCER', 'INTERESSE', '2026-10-23 11:00:00'),
    (2, 5, 'CLIENTE', 'INTERESSE', '2026-10-27 15:40:00'),
    (7, 10, 'FREELANCER', 'INTERESSE', '2026-10-31 19:25:00');

INSERT INTO Matches (ProjetoId, FreelancerId, CandidaturaId, DataMatch) VALUES
    (2, 5, 15, '2026-10-27 15:40:00');



-- =========================================================
-- USUÁRIO DA APLICAÇÃO
-- =========================================================

CREATE USER IF NOT EXISTS
    'freela'@'localhost'
    IDENTIFIED BY 'freelancer';

ALTER USER 'freela'@'localhost'
IDENTIFIED BY 'freelancer';

GRANT ALL PRIVILEGES
    ON freela.*
    TO 'freela'@'localhost';

FLUSH PRIVILEGES;

-- =========================================================
-- FIM DO SCRIPT
-- =========================================================