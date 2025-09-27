# FCG – Payment API

---

API de pagamentos da **FIAP Cloud Games (FCG)**. Este microsserviço processa solicitações de pagamento, garante idempotência, publica eventos de confirmação via **Outbox → SQS**, e expõe endpoints REST documentados com Swagger/OpenAPI.
Ele se integra ao “PSP” (provedor de pagamentos simulado) e dispara o evento assíncrono `payment.confirmed` para que o **Games API** concedam o acesso ao jogo.

---

## Arquitetura (visão geral)

* **Microsserviço** em arquitetura de 3 camadas (Domain / Application / Infrastructure / Api)
* Comunicação **sincrona** HTTP com o PSP (simulado com **WireMock**).
* Persistência transacional (EF Core). Escrita de evento no **Outbox** na mesma transação do pagamento.
* **Publicação assíncrona** do evento `payment.confirmed` para **AWS SQS (FIFO)**.
* **Consumidor serverless (AWS Lambda)** (https://github.com/8NETT-2025-Grupo40/FCGPaymentConfirmedConsumer) reage ao SQS e chama a Games API para liberação do jogo (idempotente e rastreável com cabeçalhos).

---

## Fluxo de pagamento

1. **Criação do pagamento** (`POST /payments`): recebe `userId`, `purchaseId`, itens e **requer** cabeçalho `Idempotency-Key` para evitar duplicidades em retries.
2. Payment API inicia sessão no **PSP** (simulado) e processa o **capture**.
3. Em caso de sucesso:
   * Persiste o pagamento (EF Core).
   * Registra uma mensagem no **Outbox** (mesma transação).
   * Um publisher de Outbox envia `payment.confirmed` para o **SQS (FIFO)**.
4. A **Lambda** consome o evento e chama a **Games API**

---

## Stack técnica

* **.NET 8**, C#
* **Minimal API**/**ASP.NET Core**
* **EF Core** (provider configurável via connection string)
* **Outbox** + **AWS SQS (FIFO)** para integração assíncrona
* **Swagger/OpenAPI**
* **xUnit** (tests)
* **Docker** (runtime ASP.NET 8)
* **OpenTelemetry** / **AWS X-Ray** para tracing distribuído

---

## Como rodar (local)
**Pré-requisitos**

* .NET 8 SDK
* SQL Server local/remoto acessível
* (Opcional) Credenciais AWS configuradas (`aws configure`) se for publicar em SQS real

**1) Variáveis**
* Ajuste a connection string para o seu SQL:
  * `ConnectionStrings__DefaultConnection` e `ConnectionStrings__PaymentDb`
* PSP WireMock. (Na pasta Infra/wiremock se encontra o Dockerfile para rodar a imagem do WireMock usado):
  * `Psp__WIREMOCK_URL=http://localhost:5166/psp/` (ou a URL correspondente)
* SQS real:
  * `Sqs__PaymentConfirmedQueueUrl=https://sqs.us-east-1.amazonaws.com/<ACCOUNT_ID>/fcgPayment.fifo`

**2) Migrações e execução**

```bash
dotnet ef database update --project Fcg.Payment.Infrastructure --startup-project Fcg.Payment.API
dotnet run --project Fcg.Payment.API
```

**3) Teste**
* Swagger: [http://localhost:5066/swagger](http://localhost:5066/swagger)
---

### Como rodar (Docker/Compose) — recomendado
1. Rode o docker compose

   ```bash
   cd Infra
   docker compose up -d
   ```
3. **Verifique saúde**:

   ```bash
   docker compose ps
   docker compose logs -f fcg-payment-api
   ```
4. **Testar** (Swagger):

   * Payment API: [http://localhost:5066/swagger](http://localhost:5066/swagger)
5. **cURL – criar pagamento**:

   ```bash
   curl -i http://localhost:5066/payments \
     -H "Content-Type: application/json" \
     -H "Idempotency-Key: 6f6c1d2f-6a1a-4f34-9b9c-0a1e9d5b1c88" \
     -d '{
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "items": [
        {
            "gameId": "string",
            "unitPrice": 0
        }
    ],
    "currency": "BRL"
    }'
   ```

---

## Configurações e variáveis de ambiente

| Chave                                  | Exemplo                                                   | Descrição                                         |
| -------------------------------------- | --------------------------------------------------------- | ------------------------------------------------- |
| `ConnectionStrings__DefaultConnection`         | `Server=...;Database=...;...`                             | Connection string do banco.                       |
| `Psp__WIREMOCK_URL`                         | `http://localhost:5166/psp/`                              | Base URL do PSP simulado (WireMock).              |
| `Sqs__PaymentConfirmedQueueUrl`   | `https://sqs.us-east-1.amazonaws.com/.../fcgPayment.fifo` | URL da fila FIFO para `payment.confirmed`.        |


---

## Migrações (EF Core)

Executar no Package Manager Console:

```bash
# criar migration
Update-Database {MigrationName} -StartupProject Fcg.Payment.API -Connection "Server={ServerName};Database={DBName};Trusted_Connection=True;TrustServerCertificate=True"
```

---

## Endpoints & Contratos

### `POST /payments` — Criar pagamento (idempotente)

**Headers obrigatórios**

* `Idempotency-Key: string`

**Body (exemplo)**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    {
      "gameId": "string",
      "unitPrice": 0
    }
  ],
  "currency": "BRL"
}
```

**Responses**

* `201 Created` – `{"paymentId":"...","status":"Confirmed", ...}`
* `400 Bad Request` – validação / ausência de `Idempotency-Key`
* `409 Conflict` – chave de idempotência já usada com payload divergente
* `502/504` – falha de PSP (com retry possível pelo client)

### `GET /payments/{paymentId}`

* Retorna o estado atual do pagamento.

### `POST /webhooks/psp`

* Endpoint para callbacks do PSP (se aplicável). Verifica assinatura (opcional), atualiza estado e registra Outbox.

> **OpenAPI/Swagger**: acesse `/swagger` para ver o contrato completo e testar.

---

## Mensageria, Outbox & Entrega assíncrona

* **Outbox**: tabela `OutboxMessages` garantindo que a publicação do evento ocorra **exatamente uma vez** em relação à transação do pagamento.
* **Publisher** (BackgroundService/HostedService): lê Outbox pendente e publica em **SQS FIFO** (`fcgPayment.fifo`) com:
  * `MessageGroupId = <purchaseId>` (ex.: serialização por usuário)
  * `MessageDeduplicationId = <userId>` (garante deduplicação)
  * Payload exemplo do evento: `{
  "purchaseId": "8f4a8b9e-6f6a-4d58-9f9f-8e2f0d0a3b1c",
  "userId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "amount": 59.9,
  "currency": "BRL",
  "occurredAt": "2025-09-26T21:00:00Z",
  "items": [
    { "gameId": "f0f1f2f3-f4f5-f6f7-f8f9-000102030405", "price": 39.9 }
  ]
}`

---

## Observabilidade

* **Tracing distribuído**: OpenTelemetry + AWS X-Ray. Subsegmentos para chamadas HTTP externas (PSP) e publicação SQS.

---

## CI/CD

* **CI** (PR/commit): restore, build, testes, análise estática.
* **Docker**: imagem **ASP.NET 8**
* **CD** (merge → main): build/push para **ECR**, atualização do **ECS Fargate Service** 
* **Infra AWS**:
  * **ECS Fargate** + **ALB** (Path `/payments/*`)
  * **SQS FIFO** `fcgPayment.fifo` (+ DLQ)
  * **RDS** (SQL Server)
  * **CloudWatch Logs** / **X-Ray** / **OTel Collector**

