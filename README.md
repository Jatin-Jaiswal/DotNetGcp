# DotNetCrudApp (Dockerized)

This project is a minimal ASP.NET Core app with Entity Framework Core and SQL Server.

Run locally with Docker Compose:

```bash
# build and start both DB and app
docker-compose up --build -d

# open http://localhost:5000 in your browser

# view logs
docker-compose logs -f
```

Notes:
- The SQL Server container uses `SA_PASSWORD=YourStrongPassword_123`. For production use secrets or environment variables.
- The app service reads the connection string from the `ConnectionStrings__DefaultConnection` environment variable.
- To persist DB data, a Docker volume `mssql-data` is configured.

GCP deployment:
- For GCP Cloud Run / GKE, build the `Dockerfile` image and push to Container Registry.
- Use Cloud SQL for SQL Server (or external SQL Server) and update the connection string accordingly.
