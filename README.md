# netstat-ssh-parser
requests listed servers 

``` bash
netstat -atn | grep ESTABLISHED
```

and prints what ip addresses connected to which port

## setting up
need to create config files (<b>settings.json</b>, <b>ip-table.json</b>)next to executable (or in the root of the project if running through dotnet cli)

### ip-table.json

``` json
[
	{
		"address": "8.8.8.8",
		"name": "name for this ip address"
	}
]
```

### settings.json
``` json
[
	{
		"host": "123.123.123.123",
		"login": "user",
		"password": "12345678"
	}
]

```
