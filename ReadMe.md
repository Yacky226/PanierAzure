# Rapport de déploiement

## Déploiement du microservice _MicroservicePanier_ sur Microsoft Azure

### **1. Introduction**

Ce rapport présente le processus complet de déploiement du microservice _MicroservicePanier_ (API panier développée en ASP.NET Core) dans l’environnement cloud Microsoft Azure.
L’architecture utilisée repose sur :

- Un **conteneur Docker** hébergeant l’API
- Un **Azure Container Registry (ACR)** pour stocker l'image Docker
- Un **Azure App Service Linux** pour exécuter le microservice
- Un **Azure Cache for Redis** pour la gestion du cache dans le panier

L’objectif est d’obtenir un déploiement scalable, sécurisé et automatisé, compatible avec les architectures microservices.

---

---

# 🎯 2. Architecture globale

### Composants utilisés :

- **Microservice Panier (.NET 9 / Redis)**
- **Azure Container Registry (ACR)** : stockage privé de l'image `cart_api:latest`
- **Azure App Service (Web App for Containers)** : exécution scalable du conteneur
- **Azure Cache for Redis** : stockage en cache rapide pour les paniers utilisateur
- **Azure CLI** : automatisation du provisioning et du déploiement

### Diagramme logique (textuel)

```
Client → Azure App Service → Conteneur Docker (.NET API)
                     ↓
                  Redis Cache (Azure)
                     ↓
       Azure Container Registry (stockage image)
```

---

---

# 🛠 3. Préparation de l'environnement

## 3.1. Prérequis locaux

- Docker Desktop installé
- Azure CLI installé (`az version`)
- Compte Azure actif
- Code source compilé du microservice

## 3.2. Connexion à Azure

```powershell
az login
az account set --subscription "MySubscription"
```

---

---

# 📦 4. Création du registre ACR

## 4.1. Création du Resource Group

```powershell
az group create --name myResourceGroup --location westeurope
```

## 4.2. Création du registre

```powershell
az acr create --resource-group myResourceGroup --name registrynet9xyz --sku Basic --admin-enabled true
```

## 4.3. Vérification de l'état

```powershell
az provider show -n Microsoft.ContainerRegistry --query "registrationState" -o tsv
```

Résultat attendu :

```
Registered
```

---

---

# 🐳 5. Build & Push de l’image Docker

## 5.1. Connexion à ACR

```powershell
az acr login --name registrynet9xyz
```

## 5.2. Build de l'image

```powershell
docker build -t registrynet9xyz.azurecr.io/cart_api:latest .
```

## 5.3. Push vers ACR

```powershell
docker push registrynet9xyz.azurecr.io/cart_api:latest
```

## 5.4. Vérification

```powershell
az acr repository list --name registrynet9xyz -o table
```

---

---

# 🚀 6. Déploiement du conteneur sur Azure App Service

## 6.1. Création du plan App Service Linux

```powershell
az appservice plan create --name myPlan --resource-group myResourceGroup --sku B1 --is-linux
```

## 6.2. Création de la Web App

```powershell
az webapp create `
  --resource-group myResourceGroup `
  --plan myPlan `
  --name my-cart-api `
  --deployment-container-image-name registrynet9xyz.azurecr.io/cart_api:latest
```

## 6.3. Configuration de la connexion entre Web App et ACR

```powershell
az acr credential show --name registrynet9xyz
```

Puis configuration :

```powershell
az webapp config container set --name my-cart-api --resource-group myResourceGroup `
  --container-image-name registrynet9xyz.azurecr.io/cart_api:latest `
  --container-registry-url https://registrynet9xyz.azurecr.io `
  --container-registry-user registrynet9xyz `
  --container-registry-password "<password>"
```

---

---

# 🔥 7. Mise en place de Redis Cache

## 7.1. Création de l'instance Redis

```powershell
az redis create `
  --name RedisPanierCache `
  --resource-group myResourceGroup `
  --location westeurope `
  --sku Basic --vm-size C0
```

## 7.2. Récupération du host et du mot de passe

```powershell
az redis show --name RedisPanierCache --resource-group myResourceGroup --query "hostName" -o tsv
az redis list-keys --name RedisPanierCache --resource-group myResourceGroup
```

---

---

# ⚙️ 8. Configuration des variables d'environnement

```powershell
az webapp config appsettings set `
  --resource-group myResourceGroup `
  --name my-cart-api `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    ASPNETCORE_URLS="http://+:80" `
    ConnectionStrings__Redis="RedisPanierCache.redis.cache.windows.net:6380,password=<primaryKey>,ssl=True,abortConnect=False"
```

Vérification :

```powershell
az webapp config appsettings list --resource-group myResourceGroup --name my-cart-api -o table
```

---

---

# 🧪 9. Tests & Validation

## 9.1. Récupération de l’URL publique

```powershell
az webapp show --resource-group myResourceGroup --name my-cart-api --query "defaultHostName" -o tsv
```

→ `https://my-cart-api.azurewebsites.net`

## 9.2. Test du health endpoint

```powershell
Invoke-RestMethod "https://my-cart-api.azurewebsites.net/"
```

## 9.3. Suivi des logs en continu

```powershell
az webapp log tail --resource-group myResourceGroup --name my-cart-api
```

Les logs affichent :

```
Application started.
Hosting environment: Production
Now listening on http://[::]:80
Redis connecté
```

---

---

# 📈 10. Résultat final

Le microservice est désormais :

- Déployé dans un environnement entièrement managé
- Stocké sous forme d'image Docker dans ACR
- Exécuté dans Azure App Service Linux
- Connecté à Azure Cache for Redis pour la persistance du panier
- Accessible via l'URL publique :

  ```
  https://my-cart-api.azurewebsites.net
  ```

- Mis en logs en temps réel via Azure Log Streaming

L'architecture est **scalable**, **sécurisée**, **conteneurisée**, et prête pour une architecture microservices multi-composants.

---

---

# 📡 11. Documentation de l'API

L'API expose les endpoints suivants pour la gestion du panier. Tous les endpoints sont préfixés par `/api/panier`.

## 11.1. Récupérer le panier

**GET** `/{userId}`

Récupère le panier complet pour un utilisateur donné.

**Paramètres :**

- `userId` (string) : Identifiant unique de l'utilisateur.

**Réponse (200 OK) :**

```json
{
  "id": 0,
  "userId": "user123",
  "items": [
    {
      "id": 0,
      "quantity": 2,
      "sousTotal": 199.98,
      "produit": {
        "id": 101,
        "nom": "Casque Audio",
        "prix": 99.99
      }
    }
  ],
  "total": 199.98
}
```

## 11.2. Ajouter un produit

**POST** `/{userId}/items`

Ajoute un produit au panier ou incrémente sa quantité s'il existe déjà.

**Corps de la requête (JSON) :**

```json
{
  "id": 101,
  "nom": "Casque Audio",
  "prix": 99.99,
  "quantity": 1
}
```

## 11.3. Mettre à jour la quantité

**PUT** `/{userId}/items/{produitId}`

Modifie la quantité d'un produit spécifique dans le panier.

**Paramètres :**

- `produitId` (int) : ID du produit à modifier.

**Corps de la requête (JSON) :**

```json
{
  "quantity": 5
}
```

## 11.4. Supprimer un produit

**DELETE** `/{userId}/items/{produitId}`

Retire un produit spécifique du panier.

## 11.5. Vider le panier

**DELETE** `/{userId}`

Supprime tous les articles du panier de l'utilisateur.

---

---

# 📚 12. Conclusion

Ce déploiement démontre une mise en production moderne basée sur :

- Le CI/CD manuel via Docker
- Le stockage sécurisé d’images via ACR
- Le hosting conteneurisé via App Service
- Les services managés comme Redis Cache
