#!/bin/bash

# Script para executar todos os microserviços
echo "🚀 Iniciando Microserviços - Desafio Avanade"
echo ""

# Verificar se o Docker está rodando
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker não está rodando. Por favor, inicie o Docker primeiro."
    exit 1
fi

# Iniciar RabbitMQ
echo "📨 Iniciando RabbitMQ..."
docker run -d --name rabbitmq-server -p 5672:5672 -p 15672:15672 rabbitmq:3-management > /dev/null 2>&1

# Aguardar RabbitMQ inicializar
echo "⏳ Aguardando RabbitMQ inicializar (30s)..."
sleep 30

# Função para executar um serviço em background
run_service() {
    local service_name=$1
    local service_path=$2
    local port=$3
    
    echo "🎯 Iniciando $service_name na porta $port..."
    cd "$service_path"
    dotnet run --urls="https://localhost:$port" > "../logs/${service_name,,}.log" 2>&1 &
    echo $! > "../pids/${service_name,,}.pid"
    cd ..
}

# Criar diretórios para logs e PIDs
mkdir -p logs pids

# Iniciar serviços
echo ""
echo "🏗️ Iniciando os microserviços..."

run_service "EstoqueService" "src/EstoqueService" "7001"
sleep 5

run_service "VendasService" "src/VendasService" "7002"
sleep 5

run_service "ApiGateway" "src/ApiGateway" "7000"
sleep 5

echo ""
echo "✅ Todos os serviços foram iniciados!"
echo ""
echo "📊 URLs dos serviços:"
echo "   🌐 API Gateway: https://localhost:7000/swagger"
echo "   📦 Estoque Service: https://localhost:7001/swagger"
echo "   🛒 Vendas Service: https://localhost:7002/swagger"
echo "   🐰 RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo ""
echo "📋 Para ver os logs:"
echo "   tail -f logs/estoqueservice.log"
echo "   tail -f logs/vendasservice.log"
echo "   tail -f logs/apigateway.log"
echo ""
echo "🛑 Para parar todos os serviços, execute: ./stop-services.sh"
