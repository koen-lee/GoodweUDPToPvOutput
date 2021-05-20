# GoodweUDPToPvOutput
 A tool to use the local network to query the inverter.
 
 Based on work by msatter on https://gathering.tweakers.net/forum/list_message/67162456#67162456 and ThinkPad https://gathering.tweakers.net/forum/list_message/67168926#67168926
 
 Tested with XS inverters with version numbers v1.52.14 and newer, open a ticket at Goodwe to have them update your inverter if your version is lower.
 It appears that the last dot (.14) is most significant and refers to the ARM program that contains the communication code.

# Usage

```
GoodweUdpPoller:
  Tool for querying Goodwe inverters over the local network. Without options set, will try to discover any responding
  inverters on the network and display current telemetry for the last one. Intended use is to invoke in a cronjob
  every 5 minutes to update PVOutput.org, but can be adapted to other uses as it outputs json by default.

Usage:
  GoodweUdpPoller.exe [options]

Options:
  --host <host>                                    IP address, hostname or subnet broadcast address of the inverter.
                                                   If unset, will broadcast a discovery packet to find any compatible
                                                   inverter
  --timeout <timeout>                              Listen timeout for replies
  --pvoutput-system-id <pvoutput-system-id>        System Id for API access on pvoutput.org, see
                                                   https://pvoutput.org/help/api_specification.html
  --pvoutput-apikey <pvoutput-apikey>              API key for API access on pvoutput.org, see
                                                   https://pvoutput.org/help/api_specification.html
  --pvoutput-request-url <pvoutput-request-url>    optional url to post to
  --version                                        Display version information
```
  Example:
  `GoodweUdpPoller.exe --host=192.168.2.123 --pvoutput-apikey=234abc123abcdef456789abcdef01234567abcde --pvoutput-system-id=12345`
  
  Response:
```
{
  "Temperature": 19.8,
  "Status": 0,
  "EnergyLifetime": 390.4,
  "EnergyToday": 3.2,
  "Power": 92,
  "Iac": 0.6,
  "Vac": 236.7,
  "GridFrequency": 49.98,
  "Ipv": 0.2,
  "Vpv": 197.9,
  "Timestamp": "2021-05-19T19:13:52+01:00",
  "ResponseIp": "192.168.2.123"
}
<=OK 200: Added Status
```
## Installation
A working PVOutput account is assumed.

Extract the executable to a suitable place.
For a first run, you can run it without arguments:
```
$ ./GoodweUdpPoller
```
It should discover your inverter.

Adapt the included `pollgoodwe.sh` script to use your PVOutput settings.

Schedule the script to run on your configured interval (either 5 or 15 minutes):
```
$ crontab -e
```

A sample cron line to push every five minutes:
```
*/5 *   *   *   *    /home/pi/goodweudppoller/pollgoodwe.sh
```
Make sure the path to the script is correct.
