#!/bin/bash

echo "======================================"
echo "CNK Hà Đông - Backend Deployment"
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if appsettings.json exists
if [ ! -f src/NunchakuClub.API/appsettings.json ]; then
    echo -e "${RED}Error: appsettings.json not found!${NC}"
    echo -e "${YELLOW}Please make sure appsettings.json is configured properly.${NC}"
    exit 1
fi

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed!${NC}"
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo -e "${RED}Error: Docker Compose is not installed!${NC}"
    exit 1
fi

# Determine docker compose command
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
else
    DOCKER_COMPOSE="docker-compose"
fi

echo ""
echo -e "${GREEN}Starting deployment...${NC}"
echo ""

# Stop existing containers
echo -e "${YELLOW}Stopping existing containers...${NC}"
$DOCKER_COMPOSE down

# Build and start containers
echo -e "${YELLOW}Building and starting API container...${NC}"
$DOCKER_COMPOSE up -d --build

# Wait for service to start
echo ""
echo -e "${YELLOW}Waiting for API to be ready...${NC}"
sleep 10

# Check container status
echo ""
echo -e "${GREEN}Container Status:${NC}"
$DOCKER_COMPOSE ps

echo ""
echo -e "${GREEN}======================================"
echo "Deployment Complete!"
echo "======================================${NC}"
echo ""
echo "API is running on: http://localhost:8080"
echo "Swagger UI: http://localhost:8080/swagger"
echo ""
echo "⚠️  Lưu ý: API đang kết nối tới PostgreSQL và Redis trên localhost của VPS"
echo ""
echo "Useful commands:"
echo "  View logs:          $DOCKER_COMPOSE logs -f api"
echo "  Stop API:           $DOCKER_COMPOSE down"
echo "  Restart API:        $DOCKER_COMPOSE restart api"
echo "  Rebuild:            $DOCKER_COMPOSE up -d --build"
echo ""
