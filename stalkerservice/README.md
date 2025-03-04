# Stalker E-Commerce Platform

A modern e-commerce platform built with .NET 8, featuring multi-currency support and a reseller system.

## Features

- Multi-currency support with real-time exchange rates
- Reseller system with customizable commission rates
- Product catalog with categories and dynamic pricing
- Order management system
- User authentication and role-based authorization
- RESTful API with Swagger documentation

## Project Structure

- **StalkerModels**: Class library containing all data models and DbContext
  - Models/: Entity models
  - Data/: Database context and configurations

- **StalkerApi**: Web API project
  - Controllers/: API endpoints
  - Repositories/: Generic repository pattern implementation

- **StalkerUI**: Blazor WebAssembly project (to be implemented)
  - Modern UI with Tailwind CSS
  - Responsive design
  - Real-time updates

## Setup Instructions

1. Update the database connection string in `StalkerApi/appsettings.json`

2. Install required packages:
```bash
cd stalkerservice
dotnet restore
```

3. Apply database migrations:
```bash
cd StalkerApi
dotnet ef database update
```

4. Run the API:
```bash
cd StalkerApi
dotnet run
```

5. Access Swagger documentation:
```
https://localhost:5001/swagger
```

## API Endpoints

### Authentication
- POST /api/auth/register - Register new user
- POST /api/auth/login - User login

### Products
- GET /api/products - List products with filtering and pagination
- GET /api/products/{id} - Get product details
- POST /api/products - Create product (Admin only)
- PUT /api/products/{id} - Update product (Admin only)
- DELETE /api/products/{id} - Delete product (Admin only)

### Categories
- GET /api/categories - List categories
- GET /api/categories/{id} - Get category details
- POST /api/categories - Create category (Admin only)
- PUT /api/categories/{id} - Update category (Admin only)
- DELETE /api/categories/{id} - Delete category (Admin only)

### Orders
- GET /api/orders - List orders
- GET /api/orders/{id} - Get order details
- POST /api/orders - Create order
- PUT /api/orders/{id}/status - Update order status (Admin only)

### Reseller Operations
- GET /api/reseller/products - List reseller products
- POST /api/reseller/products/{productId} - Add product to reseller catalog
- PUT /api/reseller/products/{id} - Update reseller product
- DELETE /api/reseller/products/{id} - Remove product from reseller catalog
- GET /api/reseller/stats - Get reseller statistics

### Currency
- GET /api/currency - List available currencies
- GET /api/currency/default - Get default currency
- GET /api/currency/{code} - Get currency details
- POST /api/currency - Add new currency (Admin only)
- PUT /api/currency/{code} - Update currency (Admin only)
- GET /api/currency/convert - Convert amount between currencies

## Next Steps

1. Implement the Blazor UI components
2. Add payment gateway integration
3. Implement email notifications
4. Add product image upload functionality
5. Implement caching for better performance
6. Add unit tests and integration tests

## Security Considerations

- JWT authentication is implemented
- Role-based authorization is in place
- Password requirements are enforced
- Email verification is required
- HTTPS is enforced
- CORS is configured for specific origins

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.
