# Simple Microservices

A .NET microservices architecture built with **ASP.NET Core**, **Docker**, **Kubernetes**, **RabbitMQ**, and **gRPC** — based on the [.NET Microservices Full Course]

---

## Overview

This project demonstrates a real-world microservices architecture using two independently deployable .NET services that communicate both synchronously (HTTP & gRPC) and asynchronously (RabbitMQ event bus), orchestrated on a local Kubernetes cluster.

---

## Architecture

```
                        ┌─────────────────────────────────┐
                        │         Kubernetes Cluster        │
                        │                                   │
  External Traffic ───▶ │  NGINX Ingress (API Gateway)     │
                        │         │            │            │
                        │         ▼            ▼            │
                        │  PlatformService  CommandsService │
                        │   (SQL Server)    (In-Memory DB)  │
                        │         │            │            │
                        │         └────────────┘            │
                        │     RabbitMQ (Async Events)       │
                        │     gRPC (Sync Data Seeding)      │
                        └─────────────────────────────────-─┘
```

**PlatformService** is the upstream service — it manages platforms (e.g. Dot Net, SQL Server) and publishes events when new platforms are created.

**CommandsService** is the downstream service — it consumes platform events from RabbitMQ and stores related commands against each platform. It also pulls an initial data seed from PlatformService via gRPC on startup.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Services | ASP.NET Core (.NET), C# |
| Containerisation | Docker |
| Orchestration | Kubernetes (local cluster) |
| API Gateway | NGINX Ingress Controller |
| Async Messaging | RabbitMQ (Event Bus) |
| Sync Messaging | gRPC + Protocol Buffers |
| HTTP Communication | REST APIs (HttpClient) |
| Persistence (Platform) | SQL Server |
| Persistence (Commands) | In-Memory Database |
| API Documentation | Swagger / OpenAPI |

---

## Services

### PlatformService
- Exposes a REST API to create and retrieve platforms
- Persists data to **SQL Server** via Entity Framework Core
- Publishes `PlatformPublished` events to **RabbitMQ** when a platform is created
- Exposes a **gRPC server** to provide platform data to CommandsService on startup

**Endpoints**

| Method | Route | Description |
|---|---|---|
| GET | `/api/platforms` | Get all platforms |
| GET | `/api/platforms/{id}` | Get platform by ID |
| POST | `/api/platforms` | Create a new platform |

---

### CommandsService
- Exposes a REST API to create and retrieve commands for a given platform
- Uses an **in-memory database** for persistence
- Subscribes to **RabbitMQ** events to consume new platforms published by PlatformService
- Calls PlatformService via **gRPC** on startup to seed its local platform cache
- Communicates with PlatformService via HTTP in development mode

**Endpoints**

| Method | Route | Description |
|---|---|---|
| GET | `/api/c/platforms` | Get all platforms (local cache) |
| GET | `/api/c/platforms/{platformId}/commands` | Get commands for a platform |
| GET | `/api/c/platforms/{platformId}/commands/{commandId}` | Get specific command |
| POST | `/api/c/platforms/{platformId}/commands` | Create a command for a platform |

---

## Communication Patterns

### Synchronous (HTTP)
Used in development for direct service-to-service calls from CommandsService to PlatformService via `HttpClient`.

### Synchronous (gRPC)
CommandsService calls PlatformService via gRPC on startup to seed its local platform data. Uses Protocol Buffers (`.proto`) for contract definition.

### Asynchronous (RabbitMQ)
PlatformService publishes a `PlatformPublished` message to RabbitMQ whenever a platform is created. CommandsService listens on the event bus and processes incoming platform events via a background `IHostedService`.

---

## Project Structure

```
simple-microservices/
├── PlatformService/
│   ├── Controllers/        # REST API controllers
│   ├── Data/               # DbContext, repositories, EF migrations
│   ├── Dtos/               # Request/response data transfer objects
│   ├── Models/             # Domain entities
│   ├── Profiles/           # AutoMapper profiles
│   ├── AsyncDataServices/  # RabbitMQ message publisher
│   ├── SyncDataServices/   # gRPC server implementation
│   └── Protos/             # .proto contract definitions
│
├── CommandsService/
│   ├── Controllers/        # REST API controllers
│   ├── Data/               # In-memory DbContext and repositories
│   ├── Dtos/               # Request/response data transfer objects
│   ├── Models/             # Domain entities
│   ├── Profiles/           # AutoMapper profiles
│   ├── AsyncDataServices/  # RabbitMQ event listener (IHostedService)
│   ├── SyncDataServices/   # gRPC client to PlatformService
│   └── EventProcessing/    # Event processor for incoming bus messages
│
└── K8S/                    # Kubernetes manifests
    ├── platforms-depl.yaml
    ├── commands-depl.yaml
    ├── platforms-np-srv.yaml
    ├── rabbitmq-depl.yaml
    ├── mssql-plat-depl.yaml
    ├── local-pvc.yaml
    └── ingress-srv.yaml
```

---

## Running Locally

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Kubernetes enabled
- [kubectl](https://kubernetes.io/docs/tasks/tools/)

### 1. Enable Kubernetes in Docker Desktop

Open Docker Desktop → Settings → Kubernetes → Enable Kubernetes.

### 2. Apply Kubernetes manifests

```bash
kubectl apply -f K8S/
```

This deploys both services, RabbitMQ, SQL Server, NGINX Ingress, and required persistent volumes.

### 3. Verify pods are running

```bash
kubectl get pods
kubectl get services
```

### 4. Add local host entry

Add the following to your `/etc/hosts` (Mac/Linux) or `C:\Windows\System32\drivers\etc\hosts` (Windows):

```
127.0.0.1  acme.com
```

### 5. Access the APIs

- Platform Service: `http://acme.com/api/platforms`
- Commands Service: `http://acme.com/api/c/platforms`

---

## Running in Development (without Kubernetes)

```bash
# Terminal 1 – Platform Service
cd PlatformService
dotnet run

# Terminal 2 – Commands Service
cd CommandsService
dotnet run
```

> In development mode, CommandsService communicates directly with PlatformService via HTTP. RabbitMQ and gRPC require the full Kubernetes setup.

---

## Key Concepts Demonstrated

- **Microservices architecture** — independently deployable services with isolated persistence
- **Event-driven communication** — decoupled async messaging via RabbitMQ
- **gRPC data seeding** — efficient binary sync between services using Protocol Buffers
- **API Gateway pattern** — NGINX Ingress as a single entry point to route traffic
- **Containerisation** — Docker images for each service pushed to Docker Hub
- **Kubernetes orchestration** — deployments, cluster IP services, NodePort services, persistent volumes, and ingress configuration

---

## Acknowledgements

Built as part of the [**.NET Microservices – Full Course**](https://www.youtube.com/watch?v=DgVjEo3OGBI) by [Les Jackson](https://www.youtube.com/@binarythistle).

---

## Contact

**Ryan Davari**  
📧 ryan.davari@gmail.com  
📍 Melbourne, Australia  
🔗 [linkedin.com/in/ryan-davari](https://www.linkedin.com/in/ryan-davari/)
