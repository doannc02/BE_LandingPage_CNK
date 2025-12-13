#!/bin/bash

echo "================================================"
echo "  SSL Certificate Installation (Let's Encrypt)"
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

echo -e "${YELLOW}Domain: $DOMAIN${NC}"
echo ""

# Check if Nginx is running
if ! systemctl is-active --quiet nginx; then
    echo -e "${RED}❌ Nginx is not running!${NC}"
    echo -e "${YELLOW}Please run ./install-nginx.sh first${NC}"
    exit 1
fi

echo -e "${BLUE}Step 1: Installing Certbot...${NC}"
$SUDO apt update
$SUDO apt install -y certbot python3-certbot-nginx

echo ""
echo -e "${BLUE}Step 2: Testing HTTP access to domain...${NC}"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://$DOMAIN/swagger/index.html 2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✅ Domain is accessible via HTTP${NC}"
elif [ "$HTTP_CODE" = "301" ] || [ "$HTTP_CODE" = "302" ]; then
    echo -e "${GREEN}✅ Domain is accessible (redirect detected)${NC}"
else
    echo -e "${YELLOW}⚠️  Warning: Cannot access domain via HTTP (Code: $HTTP_CODE)${NC}"
    echo -e "${YELLOW}   SSL installation may fail if domain is not accessible${NC}"
    echo ""
    read -p "Do you want to continue anyway? (y/n): " -n 1 -r
    echo ""
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo ""
echo -e "${BLUE}Step 3: Obtaining SSL certificate...${NC}"
echo -e "${YELLOW}You will be asked for:${NC}"
echo "  - Email address (for renewal notifications)"
echo "  - Agree to Terms of Service (Y)"
echo "  - Redirect HTTP to HTTPS (2 - recommended)"
echo ""

$SUDO certbot --nginx -d $DOMAIN

if [ $? -eq 0 ]; then
    echo ""
    echo "================================================"
    echo -e "${GREEN}✅ SSL Certificate installed successfully!${NC}"
    echo "================================================"
    echo ""
    echo -e "Your API is now accessible at:"
    echo -e "${GREEN}https://$DOMAIN${NC}"
    echo -e "${GREEN}https://$DOMAIN/swagger${NC}"
    echo ""
    echo -e "${BLUE}Testing HTTPS...${NC}"
    sleep 2

    HTTPS_CODE=$(curl -s -o /dev/null -w "%{http_code}" https://$DOMAIN/swagger/index.html 2>/dev/null || echo "000")

    if [ "$HTTPS_CODE" = "200" ]; then
        echo -e "${GREEN}✅ HTTPS is working!${NC}"
    else
        echo -e "${YELLOW}⚠️  HTTPS returned code: $HTTPS_CODE${NC}"
    fi

    echo ""
    echo "================================================"
    echo -e "${GREEN}SSL Information:${NC}"
    echo "================================================"
    $SUDO certbot certificates

    echo ""
    echo "================================================"
    echo -e "${GREEN}Auto-Renewal:${NC}"
    echo "================================================"
    echo "Certbot will automatically renew your certificate."
    echo "Testing renewal process..."
    $SUDO certbot renew --dry-run

    echo ""
    echo "================================================"
    echo -e "${GREEN}Next Steps:${NC}"
    echo "================================================"
    echo ""
    echo "1. Update your Frontend (Vercel) to use HTTPS:"
    echo -e "   ${BLUE}API_URL=https://$DOMAIN${NC}"
    echo ""
    echo "2. Update CORS in appsettings.json:"
    echo -e "   ${BLUE}nano src/NunchakuClub.API/appsettings.json${NC}"
    echo '   "CorsOrigins": "https://your-vercel-app.vercel.app"'
    echo ""
    echo "3. Restart API:"
    echo -e "   ${BLUE}docker compose restart api${NC}"
    echo ""
    echo "4. Test API from frontend:"
    echo -e "   ${BLUE}fetch('https://$DOMAIN/api/posts')${NC}"
    echo ""
else
    echo ""
    echo -e "${RED}❌ SSL installation failed${NC}"
    echo ""
    echo "Common issues:"
    echo "1. Domain not pointing to this server"
    echo "2. Firewall blocking port 80/443"
    echo "3. Nginx not configured correctly"
    echo ""
    echo "Check Nginx error logs:"
    echo -e "${BLUE}sudo tail -f /var/log/nginx/error.log${NC}"
    exit 1
fi
