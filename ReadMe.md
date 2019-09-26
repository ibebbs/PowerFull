﻿# PowerFull

A .Net Core utility for automatically controlling device power based on messages received from MQTT.

## Background

I recently authored a utility called [SolarEdge.Monitor](https://github.com/ibebbs/SolarEdge.Monitor). This utility is able to monitor solar generation and, optionally, electrcity import/export statistics from Solar Edge HD Wave inverters (import/export statistics require an additional [Energy Meter](https://www.solaredge.com/uk/products/metering-and-sensors/solaredge-modbus-meter#/)). Statistics gathered from the inverter and meter are published to an MQTT broker for consumption by a monitoring platform (I use [Node-Red](https://nodered.org/), [InfluxDb](https://www.influxdata.com/) & [Grafana](https://grafana.com/)).

In adition to being able to monitor solar energy production, I was keen to be able to maximise the use of electricity generated by the solar panels (lowering overall export) and minimize the use of electricity from the grid (lowering overall import). To do this, I realised that I needed a means to turn on devices when we start exporting electricity and turn off devices when we start importing electricity. And thus, PowerFull was born.

PowerFull receives "real power" readings from MQTT (positive when exporting, negative when importing) and publishes messages back to MQTT when devices should be powered on or off. While designed to my use case - i.e. receiving messages from SolarEdge.Monitor and controlling Sonoff devices flashed with Tasmota - it is extremely flexible through the use of configurable topic and payload settings.

## Usage

PowerFull is a .NET Core 3.0 application. As such it is run with the following command:

```
$> dotnet .\PowerFull.dll
```

When run like this you will see the following error:

```
One or more configuration errors occured:
The following fields must be specified when a Theme has not been specified: PowerStateRequestTopic, PowerStateResponseTopic, PowerStateResponseOnPayloadRegex, PowerStateResponseOffPayloadRegex, PowerOnRequestTopic, PowerOffRequestTopic
The Broker field is required.
The PowerReadingTopic field is required.
The PowerReadingPayloadValueRegex field is required.
The Devices field is required.
```

These configuration values can be provided via the command line or environment variables as outlined below. A 'env_file_defaults' file is supplied showing a common configuration of these values (which can also be used with the docker image - see the Docker section)

### Service Configuration

The service configuration defines the high-level behavior of the service and the devices the service is to control.

#### PowerFull:Service:Devices
##### Value: string - Required - A comma separated list of devices to power on/off _in priority order_
##### Example: sonoff-battery,sonoff-lights
The core setting of the service, this provides the respective deviceId's of the devices to control. The order of the devices in this setting dictate the order in which they will be turned on or off when there is an surplus or deficit of electricity.

#### PowerFull:Service:AveragePowerReadingAcrossMinutes
##### Value: int - Optional - Defaults to '10' if not suppled
To prevent devices from rapidly turning on and off, and to ensure that a representative current power reading is ascertained (i.e. not inadvertently skewed by short spikes), power readings are averaged across a time period defined by this setting. Only after this time will a device be turned on if the average power reading is greater than the ThresholdToTurnOnDeviceWatts setting, or turned off if the average power reading is less than the ThresholdToTurnOffDeviceWatts setting.

#### PowerFull:Service:ThresholdToTurnOnDeviceWatts
##### Value: double - Optional - Must be a positive value > 1.0 - Defaults to '100.0' if not supplied
The minimum power reading required to turn on a device. If the average power reading across the PowerChangeAfterMinutes duration is greater than or equal to this value, the next powered-off device from the Devices list will be turned on. There is no action if all devices in the devices list are already on.

#### PowerFull:Service:ThresholdToTurnOffDeviceWatts
##### Value: double - Optional - Must be a negatic value < -1.0 - Defaults to '-100.0' if not supplied
The minimum power reading required to turn off a device. If the average power reading across the PowerChangeAfterMinutes duration is less than or equal to this value, the next powered-on device from the Devices list will be turned off. There is no action if all devices in the devices list are already off.

### Messaging Configuration
The messaging configuration provides the settings required to allow the service to communicate with an MQTT broker and receive current power readings

#### PowerFull:Messaging:Broker
##### Value: string - Required - a valid IP address or hostname
The address or hostname of the MQTT broker to use for communication

#### PowerFull:Messaging:Port
##### Value: int - 1024-65535 - Optional - Defaults to '1883' if not supplied
The port number used to connect to the MQTT broker

#### PowerFull:Messaging:ClientId
##### Value: string - Optional - Defaults to 'PowerFull' if not supplied
The client id supplied to the MQTT broker for this connection.

#### PowerFull:Messaging:Username
##### Value: string - Optional
The username supplied to the MQTT broker for authentication

#### PowerFull:Messaging:Password
##### Value: string - Optional
The password supplied to th eMQTT broker for authentication

#### PowerFull:Messaging:PowerReadingTopic
##### Value: string - Required
The topic on which to receive messages containing the current power reading.

#### PowerFull:Messaging:PowerReadingPayloadValueRegex
##### Value: string - Required
The regex used to extract a power reading from the payload of messages received from the PowerReadingTopic. The regex must make a single capture a group named 'RealPower' containing a string representation of a double value. If the group is not found or the value can't be parsed into a double then no power readings will be captured.

### Device configuration
All the device configuration values support pattern substitition meaning they can be tailored to the specific device. For example, when the service is run with two devices named 'sonoff-battery' and 'sonoff-lights' with a PowerStateRequestTopic of '`cmnd/%deviceId%/POWER`', the power state request message will be published to the '`cmnd/sonoff-battery/POWER`' and '`cmnd/sonoff-lights/POWER`' topics respectively.

#### PowerFull:Device:Theme 
##### Value: null, Tasmota 

To save configuration work, Themes are provided to support common devices. At the moment only a single theme has been implemented - 'Tasmota' - to control sonoff devices flashed with the Tasmota firmware.

If you supply a Theme, no other device configuration values are required. Currently only a single theme for all devices is supported; this may change in future.

#### PowerFull:Device:PowerStateRequestTopic
##### Value: string - Required if Theme has not been provided
##### Example: 'Tasmota' Theme Value - `cmnd/%deviceId%/POWER`
The topic on which to publish a message requesting the current power state of the device.

#### PowerFull:Device:PowerStateRequestPayload
##### Value: string - Optional
The payload of the message published to the PowerStateRequestTopic used to request the current power state of the device.

#### PowerFull:Device:PowerStateResponseTopic
##### Value: string - Required if Theme has not been provided
##### Example: 'Tasmota' Theme Value - `stat/%deviceId%/POWER`
The topic on which to listen for messagings containing a response to the power state request message.

####  PowerFull:Device:PowerStateResponseOnPayloadRegex
##### Value: string - Must be a valid regex
##### Example: 'Tasmota' Theme Value - `ON`
The regex used to match an 'On' state from the payload of the message published to the PowerStateResponseTopic. No special capture values are required from this regex, it must simply match the payload that represents on (and not match the payload that represents off).

#### PowerFull:Device:PowerStateResponseOffPayloadRegex
##### Value: string - Must be a valid regex
##### Example: 'Tasmota' Theme Value - `OFF`
The regex used to match an 'Off' state from the payload of the message published to the PowerStateResponseTopic.

#### PowerFull:Device:PowerOnRequestTopic
##### Value: string - Required if Theme has not been provided
##### Example: 'Tasmota' Theme Value - `cmnd/%deviceId%/POWER`
The topic on which to publish a message requesting a device to turn on.

#### PowerFull:Device:PowerOnRequestPayload
##### Value: string
##### Example: 'Tasmota' Theme Value - `ON`
The payload of the message published to the PowerOnRequestTopic used to request that the device turn on.

#### PowerFull:Device:PowerOffRequestTopic
##### Value: string - Required if Theme has not been provided
##### Example: 'Tasmota' Theme Value - `cmnd/%deviceId%/POWER`
The topic on which to publish a message requesting a device to turn on.

#### PowerFull:Device:PowerOffRequestPayload
##### Value: string
##### Example: 'Tasmota' Theme Value - `OFF`
The payload of the message published to the PowerOffRequestTopic used to request that the device turn on.