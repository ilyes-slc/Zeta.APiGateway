# Zeta API Gateway

An API Gateway implementation using C#, Ocelot, and Keycloak for authentication and authorization.

## Features

- Route requests to the Users Management Service
- Authentication and authorization using Keycloak
- Token validation
- Role-based access control (admin realm role required)

## Prerequisites

- .NET 8.0 SDK
- Keycloak server running on localhost:8080
- Users Management Service running on localhost:5001

## Required NuGet Packages

- Ocelot (already installed)
- Microsoft.AspNetCore.Authentication.JwtBearer

## Keycloak Configuration

1. Create a realm in Keycloak (or use the master realm)
2. Create a client with ID `api-gateway-client`
3. Configure the client:
   - Set Access Type to `confidential`
   - Enable `Service Accounts Enabled`
   - Add valid redirect URIs (e.g., `https://localhost:5000/*`)
4. Create a realm role named `admin`
5. Assign the `admin` role to users who need access to the API

## API Endpoints

The API Gateway exposes the following endpoints:

- GET /gateway/users - Get list of users (with optional search query)
- GET /gateway/users/{id} - Get a single user by ID
- POST /gateway/users - Create a new user
- PUT /gateway/users/{id} - Update a user by ID
- DELETE /gateway/users/{id} - Delete a user by ID

All endpoints require a valid JWT token with the `admin` realm role.

## Running the Application

```bash
dotnet run
```

The API Gateway will be available at https://localhost:5000.

## Authentication

To access the API endpoints, include a valid JWT token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

You can obtain a token from Keycloak using the token endpoint.
