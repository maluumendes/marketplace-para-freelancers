# Freela.

> Marketplace que conecta **clientes** que precisam de um projeto a **freelancers** que sabem fazer.

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC%20%2B%20Web%20API-ef6d91)
![MySQL](https://img.shields.io/badge/MySQL-8-4479A1)

Projeto acadêmico desenvolvido em grupo.

---

## Sumário

- [O problema](#o-problema)
- [A solução](#a-solução)
- [Funcionalidades](#funcionalidades)
- [Tecnologias](#tecnologias)
- [Arquitetura](#arquitetura)
- [Como rodar](#como-rodar)
- [Contas de teste](#contas-de-teste)
- [Rotas da API](#rotas-da-api)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Fora do escopo (bônus)](#fora-do-escopo-bônus)
- [Equipe](#equipe)

---

## O problema

Clientes têm dificuldade para encontrar freelancers com as habilidades certas para seus projetos. Freelancers, por sua vez, têm dificuldade para encontrar oportunidades de trabalho e acompanhar as propostas que enviam.

## A solução

O **Freela.** reúne os dois lados em um só lugar: o cliente publica o projeto, o freelancer encontra a vaga e envia uma proposta com valor e prazo, e o cliente decide quem contratar.

**Público-alvo**

| Perfil | Cadastro | O que faz no sistema |
|---|---|---|
| **Cliente** | CPF ou CNPJ | Publica projetos, recebe candidaturas, aceita ou rejeita |
| **Freelancer** | CPF | Busca vagas, envia candidaturas, acompanha o status |

---

## Funcionalidades

- **Cadastro e login**: senha armazenada com hash BCrypt e login por cookie, com dois papéis (`CLIENTE` e `FREELANCER`).
- **Perfis**: cliente e freelancer com nome, telefone e bio. O freelancer também tem link de portfólio.
- **Gerenciamento do perfil**: cada usuário edita as próprias informações.
- **Publicação de projetos**: título, descrição, faixa de orçamento, prazo, tipo de pagamento, nível de experiência, categoria e habilidades. O dono pode editar e encerrar o projeto.
- **Visualização e busca**: busca por texto (título, descrição, empresa e habilidade), com filtros de categoria, orçamento e habilidades.
- **Candidatura**: mensagem de apresentação, valor proposto e prazo. Só é possível uma candidatura por vaga, e ela pode ser retirada enquanto estiver pendente.
- **Aceitar ou rejeitar**: o cliente vê os candidatos de cada vaga e decide. O freelancer acompanha o status em "Minhas candidaturas".
- **Painel de resumo**: contagem de projetos, candidaturas e status para cada tipo de conta.

### Segurança e regras de negócio

- As senhas nunca são gravadas em texto puro (BCrypt).
- Todas as requisições que alteram dados exigem token antifalsificação (CSRF), enviado no header `X-CSRF-TOKEN`.
- O servidor confere o dono de cada vaga e candidatura; esconder o botão na tela não basta.
- O índice único `(ProjetoId, FreelancerId)` impede candidatura duplicada direto no banco.
- Vaga encerrada não recebe candidaturas.
- Nas rotas `/api`, quem não está logado recebe `401` e quem não tem permissão recebe `403`, sem redirecionar para a tela de login.

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Front-end | Razor Views (ASP.NET Core MVC), HTML, CSS e JavaScript (`fetch`) |
| Back-end | ASP.NET Core Web API em C# (.NET 10) |
| Acesso a dados | Entity Framework Core com Pomelo.EntityFrameworkCore.MySql |
| Banco de dados | MySQL 8 |
| Autenticação | Cookie Authentication com papéis |
| Senhas | BCrypt.Net-Next |

---

## Arquitetura

```
┌──────────────────────┐      fetch (JSON)     ┌──────────────────────┐      EF Core      ┌─────────────┐
│      Front-end       │ ────────────────────► │      API em C#       │ ────────────────► │    MySQL    │
│ Razor Views + JS     │ ◄──────────────────── │ Controllers + regras │ ◄──────────────── │   freela    │
└──────────────────────┘                       └──────────────────────┘                   └─────────────┘
```

- Os **controllers MVC** (`HomeController`, `ContaController`, `ProjetosController`) entregam as telas e controlam o acesso por tipo de conta.
- Os **controllers de API** (`[ApiController]`, rotas `/api/...`) concentram as regras de negócio e o acesso ao banco.
- O **JavaScript** de cada tela busca e envia os dados pela API.

---

## Como rodar

### Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [MySQL Server 8](https://dev.mysql.com/downloads/mysql/). O script usa a collation `utf8mb4_0900_ai_ci`, que só existe a partir do MySQL 8.
- Git

### 1. Clonar o repositório

```bash
git clone https://github.com/maluumendes/marketplace-para-freelancers
cd Freela
```

### 2. Criar o banco de dados

Rode o script como **root** (ou outro usuário administrador do MySQL):

```bash
mysql -u root -p < Database/freela.sql
```

O script:

1. **apaga** o banco `freela` se ele já existir e o cria de novo;
2. cria as tabelas;
3. insere categorias e dados de teste;
4. cria o usuário `freela` (senha `freelancer`) com acesso ao banco `freela`.

> Rodar o script de novo apaga todos os dados do banco `freela`.

No Windows, se o `mysql` não estiver no PATH, abra o arquivo `Database/freela.sql` no MySQL Workbench e execute o script inteiro.

### 3. Conferir a conexão

A string de conexão fica em `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=freela;User=freela;Password=freelancer;"
}
```

Se o seu MySQL usa outra porta, outro usuário ou outra senha, altere aqui.

### 4. Restaurar os pacotes e rodar

```bash
dotnet restore
dotnet run
```

O sistema abre em:

- http://localhost:5272
- https://localhost:7299 (com o perfil `https`: `dotnet run --launch-profile https`)

### 5. Testar o banco (opcional)

Em ambiente de desenvolvimento, acesse **http://localhost:5272/teste-banco**. A página mostra quantos registros há em cada tabela e avisa se o banco estiver desatualizado.

### Problemas comuns

| Sintoma | Causa provável | Solução |
|---|---|---|
| `NETSDK1013: The TargetFramework value ... was not recognized` | `<TargetFramework>` quebrado em duas linhas no `.csproj` | Deixar `<TargetFramework>net10.0</TargetFramework>` em uma linha só |
| `Access denied for user 'freela'@'localhost'` | O script não foi rodado como root, ou a senha mudou | Rodar o `freela.sql` de novo como root |
| `Unknown collation: 'utf8mb4_0900_ai_ci'` | MySQL 5.7 ou MariaDB | Usar MySQL 8 |
| Acentos aparecem errados | Script executado sem UTF-8 | Executar o script com `utf8mb4` (o próprio arquivo já tem `SET NAMES utf8mb4`) |
| `/teste-banco` mostra `ERRO` em alguma tabela | Banco de uma versão antiga do script | Rodar o `freela.sql` de novo e reiniciar o `dotnet run` |

---

## Contas de teste

A senha de **todas** as contas é **`Freela@123`**.

| Tipo | E-mails |
|---|---|
| Cliente | `xyz@freela.com`, `aurora@freela.com`, `norte@freela.com`, `nuvem@freela.com` |
| Freelancer | `joao@freela.com`, `marina.duarte@freela.com`, `lucas@freela.com`, `marina.costa@freela.com`, `ana@freela.com`, `pedro@freela.com` |

O banco de teste já traz candidaturas em vários status (pendentes, aceitas, rejeitada) e um projeto encerrado, para testar qualquer fluxo sem precisar cadastrar nada.

### Roteiro rápido de teste

1. Entre como `nuvem@freela.com` e publique um projeto.
2. Em outra janela (anônima), entre como `joao@freela.com`, busque o projeto e envie uma candidatura.
3. Volte ao cliente, abra os candidatos da vaga e aceite o João.
4. Volte ao freelancer: a candidatura aparece como **Aceita** em "Minhas candidaturas".

---

## Rotas da API

Todas as rotas exigem login. As que alteram dados exigem o header `X-CSRF-TOKEN`.

### Projetos — `/api/projetos`

| Verbo | Rota | Quem | Descrição |
|---|---|---|---|
| GET | `/api/projetos` | Todos | Busca projetos abertos (`busca`, `categoria`, `minimo`, `maximo`, `habilidades`) |
| GET | `/api/projetos?meus=true` | Cliente | Projetos do cliente, com contagem de candidaturas |
| GET | `/api/projetos/{id}` | Todos | Detalhes do projeto |
| POST | `/api/projetos` | Cliente | Publica um projeto |
| PUT | `/api/projetos/{id}` | Cliente (dono) | Edita um projeto aberto |
| PATCH | `/api/projetos/{id}/encerrar` | Cliente (dono) | Encerra o projeto |

### Candidaturas — `/api/candidaturas`

| Verbo | Rota | Quem | Descrição |
|---|---|---|---|
| POST | `/api/candidaturas` | Freelancer | Envia uma candidatura |
| GET | `/api/candidaturas/minhas` | Freelancer | Lista as candidaturas do freelancer |
| DELETE | `/api/candidaturas/{id}` | Freelancer (dono) | Retira uma candidatura pendente |
| GET | `/api/candidaturas/{id}` | Freelancer ou cliente envolvido | Detalhes da candidatura |
| GET | `/api/candidaturas/projeto/{projetoId}` | Cliente (dono) | Candidatos de um projeto (`?status=Pendente`) |
| PUT | `/api/candidaturas/{id}/decisao` | Cliente (dono) | Aceita ou rejeita (`{ "aceitar": true }`) |

### Outras

| Verbo | Rota | Quem | Descrição |
|---|---|---|---|
| GET | `/api/painel/resumo` | Todos | Resumo de acordo com o tipo de conta |
| GET | `/api/matches` | Todos | Matches do usuário (bônus) |
| POST | `/api/interesses` | Todos | Registra interesse ou rejeição (bônus) |
| [VERBO] | [ROTA DO PERFIL] | Todos | Consulta e edição do próprio perfil |

---

## Estrutura de pastas

```
Freela/
├── Controllers/        # Telas (MVC) e APIs ([ApiController])
├── Data/               # AppDbContext (mapeamento do EF Core)
├── Database/
│   └── freela.sql      # Criação do banco, dados de teste e usuário do MySQL
├── Extensions/         # Atalhos (ex.: ler o Id do usuário logado)
├── Models/             # Entidades, ViewModels e respostas da API
├── Services/           # Regras de interesse, candidatura e match
├── Views/              # Telas Razor (Conta, Home, Projetos, Shared)
├── wwwroot/
│   ├── css/            # Estilos do site
│   ├── js/             # Scripts de cada tela (comunicação com a API)
│   └── image/
├── appsettings.json    # String de conexão
├── Freela.csproj
└── Program.cs          # Configuração da aplicação
```

### Banco de dados

| Tabela | Conteúdo |
|---|---|
| `Usuarios` | E-mail, senha (hash) e tipo de conta |
| `Clientes` / `Freelancers` | Perfil de cada tipo (relação 1:1 com `Usuarios`) |
| `Categorias` / `Habilidades` | Tabelas de apoio |
| `FreelancerHabilidades` | Habilidades de cada freelancer (N:N) |
| `Projetos` | Vagas publicadas pelos clientes |
| `ProjetoHabilidades` | Habilidades pedidas em cada projeto (N:N) |
| `Candidaturas` | Propostas dos freelancers |
| `Interesses` / `Matches` | Funcionalidade bônus de match |

---

## Fora do escopo (bônus)

O escopo combinado não incluía o **sistema de recomendação no estilo "match"**. Mesmo assim, uma primeira versão foi implementada com o tempo que sobrou:

- o freelancer avalia uma vaga por vez (♡ interesse / × descartar);
- o cliente avalia um candidato por vez;
- quando os dois lados demonstram interesse, o sistema registra o **match**.

### Ideias para próximas versões

- Recomendação de freelancers pelas habilidades pedidas na vaga
- Avaliações entre cliente e freelancer
- Chat dentro da plataforma
- Pagamento protegido, liberado na entrega

---

## Equipe

| Integrante | Responsabilidade |
|---|---|
| Maria Luíza Barcelos Mendes | Banco de dados, cadastro e login |
| Maria Luiza Cordeiro Lisboa | Publicação e busca de projetos |
| Julia Soares Catharino | Candidaturas, interesses e matches |
| Sarah de Moura Silva | Painel de resumo e integração |
