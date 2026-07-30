# Finanças

Sistema completo de gestão financeira pessoal com uma API robusta em ASP.NET Core e uma interface web moderna em React + Vite. O projeto foi organizado para ajudar no controle de receitas, despesas, contas, cartões, metas, transferências, faturas e no acompanhamento financeiro por meio de um dashboard e de um assistente com inteligência artificial.

## Visão Geral

O Finanças permite centralizar o controle financeiro em um único ambiente, oferecendo recursos para:

- cadastrar e autenticar usuários;
- registrar contas bancárias e cartões de crédito;
- organizar categorias e lançamentos financeiros;
- acompanhar o saldo e o resumo mensal;
- controlar faturas e pagamentos;
- criar metas de gastos, transferências e cofres;
- utilizar um assistente com IA para tirar dúvidas e obter insights.

## Funcionalidades Principais

- Autenticação e cadastro de usuários com JWT
- Gestão de contas bancárias
- Gestão de cartões de crédito e faturas
- Controle de categorias e lançamentos
- Dashboard com resumo financeiro mensal
- Gestão de metas de gasto
- Transferências entre contas
- Cofrinhos para organização de objetivos financeiros
- Assistente financeiro com IA integrado ao backend
- Documentação automática da API com Swagger

## Tecnologias Utilizadas

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL / Npgsql
- JWT para autenticação
- Swagger / OpenAPI
- BCrypt para hash de senhas
- MailKit para envio de e-mails
- Google APIs / Gemini para integração com IA
- Docker

### Frontend
- React 19
- Vite
- React Router DOM
- Axios
- @react-oauth/google

## Pré-requisitos

Antes de iniciar, certifique-se de ter instalado:

- .NET SDK 8.0
- Node.js 20+ e npm
- PostgreSQL (ou um serviço compatível, como Supabase)
- Git

## Instalação

### 1. Clone o repositório

```bash
git clone <url-do-repositorio>
cd Financas
```

### 2. Configure a conexão com o banco de dados

Edite o arquivo de configuração da API em:

- Financas.Api/appsettings.Development.json

Informe a string de conexão do PostgreSQL e ajuste as demais configurações, como JWT e chave da IA, se necessário.

### 3. Instale as dependências do backend

```bash
cd Financas.Api
dotnet restore
```

Se ainda não estiver instalado, configure o EF Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

Aplique as migrações do banco:

```bash
dotnet ef database update
```

### 4. Inicie a API

```bash
dotnet run
```

A API ficará disponível, em geral, em:

- https://localhost:7041
- Swagger em https://localhost:7041/swagger

### 5. Instale as dependências do frontend

Em outro terminal:

```bash
cd Financas.Web
npm install
npm run dev
```

O frontend será aberto em:

- http://localhost:5173

## Como Usar

1. Acesse o frontend no navegador.
2. Crie uma conta ou faça login.
3. Cadastre contas bancárias, categorias e cartões.
4. Registre lançamentos financeiros.
5. Acompanhe o dashboard com os dados consolidados.
6. Utilize o módulo de IA para fazer perguntas sobre sua situação financeira.

## Estrutura do Projeto

- Financas.Api: backend da aplicação
- Financas.Web: frontend em React/Vite
- Financas.sln: solução .NET principal