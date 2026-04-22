# Azure Services Diagram

The following diagram shows all Azure services created by this repo and how they connect:

```
+---------------------------+
|   Developer / CI/CD       |
|   deploy.sh /             |
|   deploy-with-chat.sh     |
+----------+----------------+
           |
           | az deployment group create (Bicep)
           v
+----------+----------------+
|   Azure Resource Group    |
|   rg-expensemgmt-demo     |
|   (uksouth)               |
+--+-------+-------+--------+
   |       |       |
   v       v       v
+------+ +-----+ +-----------+
| App  | | SQL | | User-Asgn |
| Svc  | | DB  | | Managed   |
| Plan | |     | | Identity  |
| S1   | |Basic| | mid-*     |
+--+---+ +--+--+ +-----+-----+
   |        |           |
   | hosts  | connects  | authenticates via
   v        | (Managed  | Entra ID (no passwords)
 +------+   | Identity) |
 | ASP  +<--+-----------+
 | .NET |   |
 | 8    |   | Azure AD only
 | App  |   | auth (MCAPS
 |      |   | policy)
 +--+---+   |
    |       +-----> Azure SQL (ExpenseMgmt DB)
    |               Tables: Expenses, Users,
    |               Roles, Categories, Status
    |               Stored Procs: sp_GetExpenses,
    |               sp_CreateExpense, etc.
    |
    | (optional - deploy-with-chat.sh only)
    |
    +-----> Azure OpenAI (swedencentral)
    |       Model: GPT-4o (capacity 8)
    |       Auth: Managed Identity
    |       Role: Cognitive Services OpenAI User
    |
    +-----> Azure AI Search (uksouth)
            SKU: Basic
            Auth: Managed Identity
            Role: Search Index Data Reader
```

## Connection Summary

| From | To | How |
|------|----|-----|
| App Service | Azure SQL | Managed Identity (Active Directory Managed Identity) |
| App Service | Azure OpenAI | Managed Identity (DefaultAzureCredential / ManagedIdentityCredential) |
| App Service | Azure AI Search | Managed Identity |
| deploy.sh | All resources | Azure CLI (az login) |
| Python scripts | Azure SQL | Azure AD token (AzureCliCredential) |

## Deployment Modes

- **`bash deploy.sh`** — Deploys App Service + SQL + app code (no GenAI). Chat UI shows fallback message.
- **`bash deploy-with-chat.sh`** — Deploys everything including Azure OpenAI + AI Search for full AI chat experience.
