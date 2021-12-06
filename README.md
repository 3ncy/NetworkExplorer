# NEXP - Network Explorer

**Commandline utilitka na oskenovani site a popr. zjisteni co kde bezi za sluzby ;)**  

Syntaxe:
`nexp ip_adresa[/maska] [-p [port(y)]]`  

`ip_adresa` je IPv4 v decimalnim formatu;       `/maska` je CIDR maska site s hodnotami mezi 1 a 30;
`port(y)` muze byt byd jeden port pro oskenovani na cilovem zarizeni/siti,      nebo rozsah portu pro oskenovani ve formatu `prvni_port-druhy_port`  

Pro zobrazeni progressu scanu portů, stisknete libovolnou klavesu

## Pro OSE:

Diky projektu jsem se naucil principy vicevlaknoveho programovani a jeho implementaci v C# (Tasky, Thready,…). Take networking: navazovani TCP spojeni, pingovani (a spravny management vlaken s tim spojeny),…  
Projekt ponekud prerostl puvodni zamer vytvorit pouze jednoduchy scanner, takze na nem budu s velkou pravdepodobnosti pokracovat

<br>

> Made by 3ncy in 2021
