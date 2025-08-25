# Instalar e iniciar RabbitMQ usando Docker
docker run -d --hostname my-rabbit --name rabbitmq-server -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Construir todos os projetos
dotnet build

# Executar os microserviços (em terminais separados)

# Terminal 1 - EstoqueService
cd src/EstoqueService
dotnet run

# Terminal 2 - VendasService  
cd src/VendasService
dotnet run

# Terminal 3 - ApiGateway
cd src/ApiGateway
dotnet run

# URLs dos serviços:
# ApiGateway: https://localhost:7000
# EstoqueService: https://localhost:7001  
# VendasService: https://localhost:7002
# RabbitMQ Management: http://localhost:15672 (admin/admin)
