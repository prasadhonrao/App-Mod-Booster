# Deployment Order & Considerations

## deploy.sh and deploy-with-chat.sh follow this order:

1. Create resource group
2. Deploy Bicep (App Service Plan, App Service, Managed Identity, Azure SQL, optionally GenAI)
3. Configure App Service settings (connection string, managed identity client ID, OpenAI endpoints)
4. Wait 30 seconds for SQL Server to be fully ready
5. Add current IP and Azure services to SQL firewall
6. Wait 15 seconds for firewall rules to propagate
7. Install Python dependencies (`pyodbc`, `azure-identity`)
8. Import database schema (`run-sql.py`)
9. Configure managed identity database roles (`run-sql-dbrole.py`)
10. Deploy stored procedures (`run-sql-stored-procs.py`)
11. Build and zip app (`dotnet publish` + zip)
12. Deploy app to App Service (`az webapp deploy`)

## Key Notes

- **Azure AD Only Auth**: SQL Server uses `azureADOnlyAuthentication: true` - no SQL passwords
- **Resource naming**: All names use `uniqueString(resourceGroup().id)` for uniqueness - never `utcNow()` or `md5sum`
- **All resource names are lowercase** to comply with Azure naming requirements
- **OpenAI is always deployed to `swedencentral`** even if the resource group is in `uksouth`, to ensure GPT-4o quota availability
- **Circular dependency resolved**: OpenAI endpoint is set via `az webapp config appsettings set` AFTER Bicep deployment - not in Bicep itself
- **AZURE_CLIENT_ID must be set** in App Service config so `DefaultAzureCredential` / `ManagedIdentityCredential` knows which user-assigned identity to use
- **Chat UI is always present** - without GenAI deployment it returns a helpful fallback message

## Running locally

Change `appsettings.Development.json` connection string to use:
```
Authentication=Active Directory Default
```
Then run `az login` before starting the app. This uses your own Azure AD credentials.
