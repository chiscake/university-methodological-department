# Пункт 3. Deployment Diagram

## Актуальная схема развертывания

```mermaid
flowchart LR
    user["Пользователь"] --> supabase["Supabase (BaaS)"] & postgresql["PostgreSQL (СУБД)"]
    supabase --> postgresql
```
