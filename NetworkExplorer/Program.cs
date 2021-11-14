using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace NetworkExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        { 
            /*
             * Dve klicove casti, co program bude umet:
             * 1) zjistit aktivni hosty na networku
             * 2) zjistit co za oteverne porty na nich je
             * 
             * co je potreba udelat pro jednotlive casti
             * 1)
             *  (zjistit si masku site) 
             *      - byla by to cool featurka, ale uzivatel ji muze zadat, tohle budu implementovat az zbyde cas
             *  pingnout vsechny hosty a zjistit kdo je online
             *      separate class "Pinger"
             *          ta pingne vsechny hosty asynchronne 
             *              (asi na jinem vlakne???? aby na hlavnim slo se ptat na status searche jako to ma nmap)
             *              
             *      
             *  
             * 
             * 
             */


            //handele a rozdeleni commandu 
            //format prikazu bude NetworkExplorer (mby aliasnout nebo vydavat exe jako "nexp")
            //nexp 192.168.1.1/24           //pingne co je na siti za online pc
            //nexp 192.168.1.1              //
            //nexp 192.168.1.1:8080         //oskenuje co bezi na tom danem portu za dvojteckou
            //nexp 192.168.1.1 -ports       //oskenuje co bezi na specifikovanem hostovi
            //nexp 192.168.1.1/24 -ports    //oskenuje vsechny hosty na siti a vrati co na nich bezi (nejdriv vrati jenom senzma hostu)


            string vstup = args[0];
            //string vstup = "192.168.1.1/24";



    		//TODO: oznamit znacku pc (sitovky) podle MAC adresy

            //TODO: mby extractnout tohle do samostatne metody Check if ip is valid
            string ipToScan;
            byte maska = 32;
            Regex ipRegex = new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

            if (ipRegex.IsMatch(vstup))
            {
                if (vstup.Contains('/')) //v ip je i maska
                {
                    if (!byte.TryParse(vstup.Split('/')[1], out maska))
                    {
                        Console.WriteLine("Zadejte ip a masku ve tvaru 192.168.1.1/24 bez mezer");
                        return;
                    }
                    if (maska <= 1 || maska >= 32)
                    {
                        Console.WriteLine("Maska musi byt mezi 1 a 32");
                    }
                    ipToScan = vstup.Split('/')[0];
                }
                else
                {
                    //validni ip bez masky
                    ipToScan = vstup;
                }
            }
            else
            {
                Console.WriteLine("Zadejte ip adresu a pripadne masku ve tvaru 192.168.1.1/24 bez mezer");
                return;
            }


            Console.WriteLine(ipToScan);
            Console.WriteLine(maska);

            //handle nejakych commandu
            //pokud dostanu jenom single ip bez argumewntu a masky, tak na ni oskenuju porty.
            //pokud dostanu jenom ip s maskou, tak opinguju hosty a vratim je ktere jsou online
            //pokud dostanu jenom ip a port, oskenuju ten port abych zjistil co na nem (jestli neco) bezi


            Explorer explorer = new Explorer();

            if (maska == 32)
            {
                explorer.PingAdress(ipToScan);
            }
            {
                explorer.PingSweepRange(ipToScan, maska);

            }
        }
    }
}