#!/bin/bash

echo "================================================"
echo "  CNK API - Nginx + SSL Installation Script"
echo "================================================"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

DOMAIN="54860.vpsvinahost.vn"

# Check if running as root or with sudo
if [ "$EUID" -eq 0 ]; then
    SUDO=""
else
    SUDO="sudo"
fi

echo -e "${BLUE}Step 1: Installing Nginx...${NC}"
$SUDO apt update
$SUDO apt install -y nginx

echo ""
echo -e "${BLUE}Step 2: Copying Nginx configuration...${NC}"
$SUDO cp nginx-config/cnk-api.conf /etc/nginx/sites-available/cnk-api

echo ""
echo -e "${BLUE}Step 3: Enabling site...${NC}"
$SUDO ln -sf /etc/nginx/sites-available/cnk-api /etc/nginx/sites-enabled/cnk-api

echo ""
echo -e "${BLUE}Step 4: Removing default site...${NC}"
$SUDO rm -f /etc/nginx/sites-enabled/default

echo ""
echo -e "${BLUE}Step 5: Testing Nginx configuration...${NC}"
$SUDO nginx -t

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Nginx configuration is valid${NC}"
else
    echo -e "${RED}❌ Nginx configuration has errors${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}Step 6: Restarting Nginx...${NC}"
$SUDO systemctl restart nginx
$SUDO systemctl enable nginx

echo ""
echo -e "${GREEN}✅ Nginx installed and configured successfully!${NC}"
echo ""
echo -e "${YELLOW}Testing API access...${NC}"
sleep 2

# Test HTTP
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/swagger/index.html 2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ API is responding on port 8080${NC}"
else
    echo -e "${YELLOW}⚠️  API might not be running yet. Please run: ./deploy.sh${NC}"
fi

echo ""
echo "================================================"
echo -e "${GREEN}Next steps:${NC}"
echo "================================================"
echo ""
echo "1. Make sure your API is running:"
echo -e "   ${BLUE}./deploy.sh${NC}"
echo ""
echo "2. Test HTTP access:"
echo -e "   ${BLUE}curl http://$DOMAIN/swagger${NC}"
echo -e "   Or open: ${BLUE}http://$DOMAIN/swagger${NC}"
echo ""
echo "3. Install SSL certificate:"
echo -e "   ${BLUE}sudo apt install -y certbot python3-certbot-nginx${NC}"
echo -e "   ${BLUE}sudo certbot --nginx -d $DOMAIN${NC}"
echo ""
echo "4. After SSL installation, test HTTPS:"
echo -e "   ${BLUE}https://$DOMAIN/swagger${NC}"
echo ""
echo "================================================"
echo -e "${GREEN}Useful commands:${NC}"
echo "================================================"
echo -e "View Nginx logs:     ${BLUE}sudo tail -f /var/log/nginx/cnk-api-access.log${NC}"
echo -e "View error logs:     ${BLUE}sudo tail -f /var/log/nginx/cnk-api-error.log${NC}"
echo -e "Test Nginx config:   ${BLUE}sudo nginx -t${NC}"
echo -e "Restart Nginx:       ${BLUE}sudo systemctl restart nginx${NC}"
echo -e "Check Nginx status:  ${BLUE}sudo systemctl status nginx${NC}"
echo ""
