#!/bin/sh

# Basic script to install / update a GameServerManager instance on a Linux server
# Requires dotnet SDK, see https://learn.microsoft.com/en-us/dotnet/core/install/linux, `sudo apt-get install -y dotnet-sdk-8.0` on Ubuntu 24.04 LTS

# Logs         : journalctl -fu kestrel-gsm -n 100
# Manual Stop  : sudo systemctl stop kestrel-gsm
# Manual Start : sudo systemctl start kestrel-gsm

if [ ! -d ~/build/GameServerManager ]; then
	mkdir ~/build
	cd ~/build
	git clone https://github.com/jetelain/GameServerManager.git GameServerManager
fi

if [ ! -d /opt/GameServerManager ]; then
	sudo mkdir /opt/GameServerManager
	sudo chown $USER:$USER /opt/GameServerManager
fi

if [ ! -d /var/www/GameServerManager ]; then
	sudo mkdir /var/www/GameServerManager
	sudo chown www-data:www-data /var/www/GameServerManager
fi

if [ ! -d /var/www/aspnet-keys ]; then
	sudo mkdir /var/www/aspnet-keys
	sudo chown www-data:www-data /var/www/aspnet-keys
fi

cd ~/build/GameServerManager

echo "Update git"
git checkout main
git pull

echo "Check config"
if [ ! -f /opt/GameServerManager/appsettings.Production.json ]; then
	echo " * Create appsettings.Production.json"
	cp setup/appsettings.Production.json /opt/GameServerManager/appsettings.Production.json
	read -p "Type the Steam Api Key obtained from https://steamcommunity.com/dev/apikey, then press [ENTER]:" STEAM_API_KEY
	read -p "Type your Steam Id (for admin access), then press [ENTER]:" STEAM_ADMIN_ID
	sed -i "s/STEAM_API_KEY/$STEAM_API_KEY/g"  /opt/GameServerManager/appsettings.Production.json
	sed -i "s/STEAM_ADMIN_ID/$STEAM_ADMIN_ID/g"  /opt/GameServerManager/appsettings.Production.json
fi

if [ ! -f /etc/systemd/system/kestrel-gsm.service ]; then
	echo " * Create kestrel-gsm.service"
	sudo cp setup/kestrel-gsm.service /etc/systemd/system/kestrel-gsm.service
	sudo systemctl enable kestrel-gsm
fi

echo "Build"
rm -rf dotnet-webapp
dotnet publish -c Release -o dotnet-webapp -r linux-x64 --self-contained false GameServerManagerWebApp/GameServerManagerWebApp.csproj

echo "Stop Service"
sudo systemctl stop kestrel-gsm

echo "Copy files"
cp -ar "dotnet-webapp/." "/opt/GameServerManager"

echo "Start Service"
sudo systemctl start kestrel-gsm
