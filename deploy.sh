#!/bin/bash
# deploy.sh - Deploy App Service, Managed Identity, Azure SQL, schema, roles, stored procs and app code
# Run once to set up all infrastructure and app (without GenAI chat services)
#
# USAGE: bash deploy.sh

set -e

# === CONFIGURE THESE ===
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_LOGIN="$(az account show --query user.name -o tsv)"
ADMIN_OBJECT_ID="$(az ad signed-in-user show --query id -o tsv)"
# =======================

echo "==> Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

echo "==> Deploying Bicep infrastructure (App Service + SQL, no GenAI)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters adminLogin="$ADMIN_LOGIN" adminObjectId="$ADMIN_OBJECT_ID" deployGenAI=false \
  --query properties.outputs -o json)

APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
DB_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')

echo "==> App Service:    $APP_SERVICE_NAME"
echo "==> SQL Server:     $SQL_SERVER_FQDN"
echo "==> Database:       $DB_NAME"
echo "==> Managed Identity: $MANAGED_IDENTITY_NAME"

# Update App Service connection string
echo "==> Configuring App Service settings..."
CONN_STR="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DB_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};"
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=${CONN_STR}" \
    "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
    "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}" \
  --output none

echo "==> Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current IP + Azure services to SQL firewall
echo "==> Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowAllAzureIPs" \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  --output none
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowDeploymentIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output none
echo "==> Waiting additional 15 seconds for firewall rules to propagate..."
sleep 15

# Update Python scripts with actual server/database values
sed -i.bak "s/example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py run-sql-dbrole.py run-sql-stored-procs.py && rm -f run-sql.py.bak run-sql-dbrole.py.bak run-sql-stored-procs.py.bak
sed -i.bak "s/DATABASE = \"ExpenseMgmt\"/DATABASE = \"${DB_NAME}\"/g" run-sql.py run-sql-dbrole.py run-sql-stored-procs.py && rm -f run-sql.py.bak run-sql-dbrole.py.bak run-sql-stored-procs.py.bak

# Replace managed identity placeholder in script.sql
sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

echo "==> Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

echo "==> Importing database schema..."
python3 run-sql.py

echo "==> Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

echo "==> Deploying stored procedures..."
python3 run-sql-stored-procs.py

echo "==> Building and packaging app..."
cd app
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../app.zip . 
cd ../..

echo "==> Deploying app to Azure App Service..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip

echo ""
echo "====================================================="
echo " DEPLOYMENT COMPLETE!"
echo "====================================================="
echo " App URL:  https://$(az webapp show --resource-group $RESOURCE_GROUP --name $APP_SERVICE_NAME --query defaultHostName -o tsv)/Index"
echo " Note: Navigate to /Index - not the root URL"
echo "====================================================="
