# Overview
A small batch file that allows you to ping grouped lists of IP addresses from text files<br />
Reads addresses from the file specified on the command line or from the default file (ip.txt).<br />
## Screenshot
![image](https://github.com/ng256/pingall/assets/90511962/77bddb3a-48e7-45c3-97cb-782cdd4772a6)

## Usage
**pingall** <br /> _Reads the data from ip.txt_<br /> 
**pingall** myfile.txt <br /> _Reads the data from specified myfile.txt_<br />

## Structure of text file
;Commentary<br />
Group_name<br />
ip description<br />
## Default file ip.txt
**Google**<br />
108.177.14.102 Web server google.com<br />
8.8.8.8 Public DNS<br />

**Yandex**<br />
5.255.255.242 Web server yandex.ru<br />
77.88.8.8 Public DNS1<br />
77.88.8.1 Public DNS2<br />

**Localhost**<br />
127.0.0.1 Self<br />
## Example of myfile.txt
_;My local network_<br />
**Home_net**<br />
192.168.0.1 router<br />
192.168.0.2 my computer<br />
192.168.0.3 printer<br />

_;Public DNS Server List_<br />
**DNS**<br />
204.106.240.53 dns3.dmcibb.net<br />
170.64.147.31 ns2.seolizer.de<br />


