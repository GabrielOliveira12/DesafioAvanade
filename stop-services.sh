#!/bin/bash

echo "🛑 Parando todos os microserviços..."

# Parar serviços .NET
if [ -d "pids" ]; then
    for pidfile in pids/*.pid; do
        if [ -f "$pidfile" ]; then
            pid=$(cat "$pidfile")
            service_name=$(basename "$pidfile" .pid)
            echo "   🔴 Parando $service_name (PID: $pid)..."
            kill $pid 2>/dev/null
            rm "$pidfile"
        fi
    done
    rmdir pids 2>/dev/null
fi

# Parar RabbitMQ
echo "   🐰 Parando RabbitMQ..."
docker stop rabbitmq-server > /dev/null 2>&1
docker rm rabbitmq-server > /dev/null 2>&1

echo ""
echo "✅ Todos os serviços foram parados!"

# Limpar logs se desejar
read -p "🗑️  Deseja remover os logs? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    rm -rf logs
    echo "✅ Logs removidos!"
fi
