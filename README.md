# Porto Segura - Viaje solo, nunca sozinha (Backend / API)

<p align="center">
  <img src="https://img.shields.io/badge/Deploy-Netlify-blue?style=for-the-badge&logo=netlify" alt="Deploy na Netlify" />
  <img src="https://img.shields.io/badge/YouTube-Vídeos_da_Aplicação-red?style=for-the-badge&logo=youtube" alt="Vídeos no YouTube" />
</p>

**Porto Segura** não é um site de viagens. É uma plataforma que remove o atrito do medo através de uma rede de apoio, sem perder a autonomia da viagem solo.

🚀 **Aplicação (Frontend) em Produção:** [https://portosegura.netlify.app/](https://portosegura.netlify.app/)
📺 **Vídeos da Aplicação Rodando:** [https://www.youtube.com/@PortoSegura-g3t/videos](https://www.youtube.com/@PortoSegura-g3t/videos)

---

## 🎯 O Negócio: Autonomia Assistida
O Porto Segura resolve o problema das **62% das mulheres brasileiras** que já desistiram de viajar sozinhas por medo de assédio, violência ou falta de rede de apoio no destino (dados da nossa pesquisa).

A plataforma conecta a usuária a um **Time de Madrinhas** — moradoras locais verificadas (por meio de análise de redes sociais, vídeo e entrevista) que oferecem suporte através de um catálogo de serviços em troca de uma remuneração justa.
Tudo é gerenciado através de um sistema de créditos, garantindo flexibilidade e permitindo o consumo de serviços como:
- **Dicas Locais / Chat (2 CR):** Orientações rápidas e curadoria de locais seguros.
- **Ligação de Suporte (3 CR):** Atendimento direto para resolução de dúvidas críticas ou suporte imediato.
- **Busca no Aeroporto (15 CR):** Recepção no desembarque e acompanhamento até a hospedagem.
- **Acompanhamento Presencial (6 CR/hora):** Explorando a cidade com segurança.

## 🛠️ Tecnologias Utilizadas (Backend)
Este repositório contém a API responsável por toda a regra de negócio, gestão de usuárias, cadastro e aprovação de madrinhas, além da gestão de serviços e créditos.
- **C# & .NET 9.0** (ASP.NET Core Web API)
- **Entity Framework Core** (Migrations e acesso a dados estruturados)
- **SignalR (Hubs)** (Comunicação em tempo real, fundamental para chats e ligações de suporte)
- **Docker** (Para conteinerização, facilitando a implantação na nuvem)
- **Padrão de Arquitetura em Camadas** (Controllers, Services, Models, DTOs, Enums).

## ⚙️ Instruções de Build e Execução
Para detalhes sobre como compilar, executar localmente e utilizar o Docker para subir o backend da aplicação, consulte o nosso [**Guia de Build (BUILD.md)**](./BUILD.md).

---
*Porto Segura: Viajar solo nunca mais será sinônimo de estar sozinha.*
