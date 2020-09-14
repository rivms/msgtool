![master](https://github.com/rivms/msgtool/workflows/master/badge.svg)
# msgtool
CLI tool for sending and watching messages

## Available commands
Commands are organised into a hierarchy, View the list `.\azmsg.exe --help`
|Root Command|Sub Commands|Description|
|-------|------------|-----------|
|iothub |set-context|Set a named set of defaults for iothub commands|
|iothub |use-context|Activate a named set of defaultas for iothub commands|
|iothub |message    |Listen for a fixed number of messages or without limit. |
|iothub |d2c        |Send a message from the cli or from a file. Device identity is set via context|
|eventhub|set-context|Set a named set of defaults for eventhub commands|
|eventhub|use-context|Activate a named set of defaultas for iothub commands|
|eventhub|send|Send a message from the cli or from a file. Context event hub name can be overridden|
|eventhub|message|Listen for a fixed number of messages or without limit. Context event hub name can be overridden|
|eventhub|simulate|Send a fixed number of messages or stream continuously. Available simulator: Temperature |


## Configuration
The context needs to be setup for each root command separately. All values are written to a single json file located in the user home such as `C:\Users\auser\.azmsg\config`.  

Each context has an associated name and can be activated per root command using the `use-context` sub command. The currently active version command can be seen in the config file. Some commands allow a context attribute to be overridde via the cli. Context attribute represent key value pairs, these can be set in a single call or subsequent calls, only the specified attribute is modified. To unset an attribute set it to a blank string. 

|Root Command|Context|Description|
|------------|-------|-----------|
|iothub|connection-string|Full connection string, can be found via the Portal, Settings->Shared access policies. Enclose using "" if need. Sample connectring `"HostName=iothubname.azure-devices.net;SharedAccessKeyName=accesskeyname;SharedAccessKey=accesskey"`|
|iothub|eventhub-name|The Event Hub-compatible name found via the Portal, Settings->Built-in endpoints|
|iothub|device-connection-string|Used to send device to cloud message, the device needs to be created ahead of use|
|eventhub|connection-string|Full connect string for the event hub namespace. Hub name can be specified separately|
|eventhub|eventhub-name|Default hub name to be used with this context. Can be unset or overriden on the cli|
|eventhub|consumer-group|Default consumer group to be used with this context|


## Examples
All examples assume the context has been configured as needed.
```
.\azmsg.exe eventhub set-context test_context --connection-string "..." --eventhub-name "samplehub" 
```

Using a context
```
.\azmsg.exe eventhub use-context test_context
```

Sending a message via the cli
```
.\azmsg.exe eventhub send --message "Hello World!"
```

Sending a message using a file
```
.\azmsg.exe eventhub send --from-file .\message.txt
```

Streaming messages and overriding the default hub name
```
.\azmsg.exe eventhub --follow --eventhub-name "anotherhub"
```

Listening for the next 5 messages using the hub name setup in the context
```
.\azmsg.exe eventhub --limit 5
```
