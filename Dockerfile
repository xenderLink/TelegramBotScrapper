FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

WORKDIR /build
ADD TelegramBotScrapper ./

RUN dotnet restore 
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:6.0 as runtime

WORKDIR /Chrome

RUN apt-get update && apt-get install -y \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libatspi2.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
#   libgtk-4-1 \
    libnspr4 \
    libnss3 \
    libwayland-client0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    xdg-utils \
    libu2f-udev \
    libvulkan1 \
    wget \
    libcurl3-gnutls

ADD ChromePackages/chrome.deb ./
RUN dpkg -i chrome.deb

WORKDIR /
RUN rm -rf Chrome

WORKDIR /App
RUN mkdir -p JsonCrud
ADD ChromePackages/chromedriver .
COPY --from=build-env /build/out .

EXPOSE 10112

ENTRYPOINT ["dotnet", "TelegramBotScrapper.dll"]