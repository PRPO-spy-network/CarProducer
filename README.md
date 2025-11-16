# CarProducer

CarProducer je mikrostoritev, ki izpostavi API endpoint /location. Za veè informacij poglej /swagger, ko zaženemo aplikacijo v Developer naèinu. Na ta endpoint lahko pošljemo informacije o lokaciji avtomobilov, ki jih ta nato shrani v pravilen Azure event hubs vrsto glede na registracijo. 

## Postavitev

Najlažje je naložiti z Dockerfile. Nastavimo lahko nekaj okoljskih spremenljivk:
- ASPNETCORE_HTTPS_PORTS : https port na katerim serviramo api npr. 443
- ASPNETCORE_HTTP_PORTS :  http port na katerim serviramo api npr. 80
- EVENT_HUBS_CONN_STRING : connection string za azure event hubs
- TIMESCALE_CONN_STRING : connection string za podatkovno bazo (\*baza ni nujno timescale, a je bilo narejeno z njo v mislih)
- APP_CONFIG_CONNECTION : povezava do konfiguracijskega strežnika (za dodajanje regij)