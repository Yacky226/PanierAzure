# Microservice Panier - Azure Container Apps

Ce projet est une API de gestion de panier développée en **ASP.NET Core (.NET 9)**. Elle est conçue pour être déployée sur **Azure Container Apps (ACA)**, offrant une architecture **Serverless**, scalable et capable de s'éteindre complètement (Scale-to-Zero) pour optimiser les coûts.

## Le lien de deploiement : https://panier-app.nicemoss-e410f043.spaincentral.azurecontainerapps.io

## Architecture

L'architecture cloud native repose sur les composants suivants :

- **Azure Container Apps (ACA)** : Hébergement du microservice (Serverless Containers).
- **Azure Container Registry (ACR)** : Registre privé pour stocker l'image Docker.
- **Azure Cache for Redis** : Base de données en mémoire pour une persistance rapide des paniers.
- **KEDA (intégré)** : Gestion de l'autoscaling (de 0 à N instances).

<!-- end list -->

## 🛠 Prérequis

- **Docker Desktop** installé localement.
- **Azure CLI** installé et connecté (`az login`).
- Souscription Azure active (compatible "Azure for Students").

## Guide de Déploiement

### 1\. Préparation des ressources

Définition des variables (PowerShell) :

```powershell
$RG = "DefaultResourceGroup-ESC"       # Groupe de ressources (selon disponibilité région)
$Location = "spaincentral"             # Région
$ACR_Name = "registrynet9xyz"          # Nom unique du registre
$Env_Name = "my-env"                   # Environnement Container Apps
$App_Name = "panier-app"               # Nom du microservice
```

### 2\. Build & Push de l'image Docker

Si ce n'est pas déjà fait, construisez et poussez l'image vers Azure :

```powershell
# Création du registre
az acr create --resource-group $RG --name $ACR_Name --sku Basic --admin-enabled true

# Connexion & Push
az acr login --name $ACR_Name
docker build -t $ACR_Name.azurecr.io/cart_api:latest .
docker push $ACR_Name.azurecr.io/cart_api:latest
```

### 3\. Création de l'infrastructure Container Apps

Création de l'environnement géré :

```powershell
az containerapp env create `
  --name $Env_Name `
  --resource-group $RG `
  --location $Location
```

### 4\. Déploiement du Microservice

Déploiement de l'application avec configuration du port 80 :

```powershell
# Récupération automatique du mot de passe ACR
$AcrPass = az acr credential show --name $ACR_Name --resource-group $RG --query "passwords[0].value" -o tsv

# Création de l'App
az containerapp create `
  --name $App_Name `
  --resource-group $RG `
  --environment $Env_Name `
  --image "$ACR_Name.azurecr.io/cart_api:latest" `
  --target-port 80 `
  --ingress external `
  --registry-server "$ACR_Name.azurecr.io" `
  --registry-username $ACR_Name `
  --registry-password $AcrPass `
  --min-replicas 0 `
  --max-replicas 5 `
  --set-env-vars ASPNETCORE_URLS="http://+:80"
```

> **Note :** `min-replicas 0` active le "Scale to Zero". L'application s'éteint si elle n'est pas utilisée (coût = 0€).

### 5\. Connexion à Redis Cache

Pour que le panier fonctionne, il faut lier le cache Redis et autoriser la connexion.

1.  **Récupérer la clé Redis :**

    ```powershell
    $RedisKey = az redis list-keys -g $RG -n "RedisPanierCache" --query primaryKey -o tsv
    ```

2.  **Autoriser l'accès réseau (Firewall) :**
    _Indispensable si Redis et l'App sont dans des régions différentes._

    ```powershell
    az redis firewall-rules create --name "RedisPanierCache" --resource-group $RG --rule-name AllowAll --start-ip 0.0.0.0 --end-ip 255.255.255.255
    ```

3.  **Injecter la connexion dans l'App :**

    ```powershell
    az containerapp update `
      --name $App_Name `
      --resource-group $RG `
      --set-env-vars ConnectionStrings__Redis="RedisPanierCache.redis.cache.windows.net:6380,password=$RedisKey,ssl=True,abortConnect=False"
    ```

## Documentation de l'API

L'API est accessible via l'URL publique fournie par Azure Container Apps.

## Le lien de deploiement : https://panier-app.nicemoss-e410f043.spaincentral.azurecontainerapps.io

| Méthode    | Endpoint                          | Description                                  |
| :--------- | :-------------------------------- | :------------------------------------------- |
| **GET**    | `/api/panier/{userId}`            | Récupère le panier d'un utilisateur.         |
| **POST**   | `/api/panier/{userId}/items`      | Ajoute un article ou met à jour la quantité. |
| **PUT**    | `/api/panier/{userId}/items/{id}` | Modifie la quantité d'un article spécifique. |
| **DELETE** | `/api/panier/{userId}/items/{id}` | Supprime un article du panier.               |
| **DELETE** | `/api/panier/{userId}`            | Vide le panier complet.                      |

### Exemple de Payload (POST)

```json
{
  "id": 101,
  "nom": "Casque Audio",
  "prix": 99.99,
  "quantity": 1
}
```
