# EasySave Log Centralization Server

Service Docker pour centraliser les logs de plusieurs instances EasySave en temps réel.

## 🚀 Démarrage rapide

### Démarrer le serveur

```bash
docker-compose up --build -d
```

### Vérifier l'état

```bash
docker logs easysave-logserver
docker ps
```

### Arrêter le serveur

```bash
docker-compose down
```

## 📁 Fichiers logs centralisés

Les logs sont stockés dans `./logs/` avec un fichier par jour :
- `2025-02-19.json`
- `2025-02-20.json`
- etc.

Chaque entrée contient :
- `MachineName` : Nom de la machine cliente
- `UserName` : Nom de l'utilisateur
- Toutes les informations de backup

## ⚙️ Configuration EasySave

Dans `default.json` ou via l'interface, configurez :

```json
{
  "LogMode": "Local",           // Options: "Local", "Remote", "Both"
  "LogServerUrl": "http://localhost:5000",
  "LoggerFormat": "json"
}
```

### Modes disponibles :

| Mode       | Description                                |
|------------|--------------------------------------------|
| `Local`    | Logs uniquement sur le PC client          |
| `Remote`   | Logs uniquement sur le serveur Docker     |
| `Both`     | Logs sur le PC client ET le serveur       |

## 🔌 API Endpoints

### POST `/api/logs`
Reçoit et stocke une entrée de log.

### GET `/api/logs/health`
Health check du serveur.

### GET `/api/logs/files`
Liste des fichiers de logs disponibles.

## 🧪 Test du serveur

```bash
# Test manuel avec curl
curl -X POST http://localhost:5000/api/logs \
  -H "Content-Type: application/json" \
  -d '{"MachineName":"TEST","UserName":"Admin","Message":"Test log"}'

# Vérifier la santé
curl http://localhost:5000/api/logs/health
```

## 📦 Architecture

```
EasySave Client 1 ──┐
EasySave Client 2 ──┼──> [Docker LogServer:5000] ──> logs/2025-02-19.json
EasySave Client N ──┘
```

Tous les logs sont agrégés dans un fichier journalier unique avec distinction par `MachineName` et `UserName`.
