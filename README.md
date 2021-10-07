# SNSRPI Device

[SNSRPI Device](#snsrpi-device)
  - [Overview](#overview)
  - [Install and Setup](#install-and-setup)
    - [File structure](#file-structure)
    - [.NET setup](#net-setup)
    - [Python setup](#python-setup)
  - [Running the Program](#running-the-program)
    - [Required input settings](#required-input-settings)
    - [Configuring the device settings](#configuring-the-device-settings)
    - [Running with Docker](#running-with-docker)
    - [Running with .NET standalone](#running-with-net-standalone)
  - [Communciating with the Services](#communciating-with-the-services)
    - [ASP.NET API](#aspnet-api)
    - [AWS IoT Communcations](#aws-iot-communcations)
- [Room for improvement](#room-for-improvement)
  - [Terminology](#terminology)

## Overview

The primary purpose of this application is for managing and operating a SNSR CX1 accelerometer using the C# dll provided by the manufacturer. It consists of two main components/services:
- **snsrpi** - a C# application that utilises this library to connect and operate the accelerometers. It also uses an ASP.NET API to receive instructions for operation remotely
- **iot** - A python application utilising the AWS Device IoT SDK. This provides a simple and secure mechanism for the device to communicate to the outside world using the AWS IoT Core service. This is a preffered option than publicly exposing the snsrpi API. Instead the iot service acts as a sidecar to the snsrpi service, handling all incoming/outgoing messages, and simply interracts with the snsrpi service via its REST api 



### File structure

```
| snsrpi-device/
| --- snsrpi-device/
   | --- config/
   | --- Controllers/
   | --- CXLib/
   | --- Interfaces/
   | --- Models/
   | --- Services/
   | --- obj/Properties/bin/ ... default .net areas
   | --- Program.cs
   | --- Startup.cs
   | --- appsettings.*.json
   | --- snsrpi-device.csproj
| --- snsrpiTests/
   | --- .NET unit tests
| --- iot/
   | --- certs/
   | --- *.py files
| --- Dockerfiles + docker-compose

```
The main folders of interest are:
- `snsrpi-device/config` - where you can store pre-determined *_config.json files for device settings. These are more for development but can be copied to production
- `snsrpi-device/Controllers` - API definitions live here
- `snsrpi-device/CXLIb` - SNSR dlls live here
- `snsrpi-device/Models` - Main class definitions for all objects storing data
- `snsrpi-device/Services` - Manager classes so that the API controllers can interract with Models (i.e. logger)
- `snsrpi-device/Program.cs, Startup.cs` - Program.cs is where the main function gets called on startup. Startup is where additional options for the API are configured
- `snspriTests/` - Unit Test project. Fairly barebones at the moment (which was bad on my part). Definitely needs to be more comprehensive for production
- `iot/main` - main scrpt for AWS Iot Device sidecar. Configures everything and listens for events
- `iot/Device` - main class that handles device operations and heartbeats
- `iot/certs` - You will need to create and populate this for development (instructions below)
- `ShadowHandler` - handler functions for listening and responding for updates to device state

## Install and Setup


The main packages and runtimes that are used for the services are.

- **.NET 5.0 SDK and Runtime** - for development on local device. If you want to run using .NET on the raspberry pi instead of docker then you will need to install on the Rpi as well (discussed below)
- **Python 3.7+**

- **Docker and Docker compose** - for both local device and RPi. For Windows I would install Docker desktop as this comes shipped with all necessary packages (including docker compose). This requires WSL2. For setting up on the RPI follow [these instructions](#https://www.docker.com/blog/happy-pi-day-docker-raspberry-pi/). Docker compose can just be installed via `pip install docker-compose`


**Note**: The SNSR dlls use an older version of .NET framework (.NET 2.0 from back in circa 2003). I've upgraded the project to a modern .NET version so I could integrate an ASP.NET WebAPI with the project. The dlls are still mostly compatabile with new .NET versions, apart from the windows specific components such as USB interfaces (which are not used anyway)

### .NET setup
Once .NET is installed navigate to the internal `snsrpi-device` directory and run `dotnet restore` to install the dependencies. It doesn't use too many 3rd party packages, the main ones being:
- Newtonsoft.JSON - JSON reading/writing utility
- CSVHelper - Utility for simplifiying writing to CSV
- Featherdotnet - Utility for writing feather files. feather is a lightweight binary format that works well with Python/pandas (much quicker and more efficient than stupid csv's)

The SNSR dll's live in the CXLib folder

### Python setup

in root directory create a virtual python env and install the relevant aws packages. The key packages are the AWS IoT device SDK and all relevant packages with that

``` {python}
python -m venv .venv
source .venv/bin/active
pip install -r requirements.txt
```

You will need to setup each device as an AWS IoT thing. Easiest way is to follow this [AWS tutorial](https://docs.aws.amazon.com/iot/latest/developerguide/iot-gs.html).

the script [aws_iot_init.py](#aws_iot_init.py) can be used to initialse a new thing and download its certificates. These will then need to be transferred to the device of choise. It doesn't download the AmazonRootCA, so you may still need to get this from the console but I belive the Root CA can be reused among devices.

## Running the Program

There are several ways to run this application
- Running a standalone snsrpi instance (no iot sidecar) using .NET
- Running snsrpi and iot services using local .NET and python
- Running one or mutliple services using Docker (preferred)

Each option is discussed below

### Required input settings

Regardless of which option you go with. The applications expect several input parameters in the form of environment variables

*** SNSRPI / C# Environment Variables ***

| ENV_VAR | Description | Type | Example |
|---------|---------------------|------|---------|
|DEVICE_NAME| Name of device, should match AWS IoT Thing name| str | local_device|
| OUTPUT_DATA_DIR | Directory where output files are written to. This can differ from dev/prod and must be absolute | str | /home/username/data |
|DEMO| If true, does not look for devices and creates fake devices for development (useful if you don't have a sensor nearby). If this is not specified, default is assume Demo=true | bool | true/false |
|DEVICE_CONFIG_DIR| Directory to look for device config json files. If it deosn't exist, default settings are used | str | /home/userprofile/config |
|ASPNETCORE_URLS|Use to specify listening ports/urls (mainly for production so it doesn't listen on port 80)|str| "https://+:5000"|

*** IOT / Python Environment Variables ***

| ENV_VAR | Description | Type | Example |
|---------|---------------------|------|---------|
|AWS_IOT_ENDPOINT| Endpoint fo AWS Iot core account. See the AWS tutorial for how to find this| str | examples.iot.ap-southeast-2.amazonaws.com|
| DEVICE_ENDPOINT | Endpoint of snsrpi API (default port 5000) | str | localhost:5000 |
|PRIVATE_KEY| path to private key file (can be relative or absolute) | str | iot/certs/private.pem.key |
|PRIVATE_KEY| path to private key file (can be relative or absolute) | str | iot/certs/private.pem.key |
|DEVICE_CERT| path to private key file (can be relative or absolute) | str | iot/certs/device.pem.key |
|ROOT_CA| path to private key file (can be relative or absolute) | str | iot/certs/AmazonRootCA1.pem |
|DEVICE_NAME| Device name, should be same as above | str | local_device |


### Configuring the device settings

### Running with Docker

The instructions are more or less the same for running on a dev machine or on a RPi.  From the root directory:
- Ensure docker is installed
- Build to two docker images respectively:
  - For snsrpi `docker build -f Dockerfile -t device:latest .`
  - For iot `docker build -f Dockerfile.iot -t iot:latest .`
  - Note: the tag specified by -t could be anything you want, just update the references for the run command

To run the standalone docker images use the below commands

`docker run -it --network host -e <INSERT ENV VARS HERE> </INSERT> device:latest`
`docker run -it -e <INSERT ENV VARS HERE>  --network host iot:latest`

If running in demo mode (i.e. DEMO=true), you don't need to include the --network specification. Its important for running with an actual device though as the dll looks for devices on the local network of the host. As docker typically runs in its own subnet, if this is not specified, it won't find any devices.

Alternatively. You can run the multi-container setup using `docker-compose`. I personally find this the best way a nice easy and clear way to setup/configure the containers. Especially as there are multple envirionment variables to pass into the containers, it saves a multi-line cmd input. This requires a [docker-compose.yml](docker-compose.yml) file. From the root directory run `docker-compose up` and it will launch all containers and settings specified in the file.

The compose file also utilises docker bind volumes, which are a way of linking a directory in a docker container to a directory on the host (for things such as data files, config files and device certificates for example). Best explanation for how these work is in the docker documentation. 

***IMPORTANT:*** Because the RPi uses an Arm archicture instead of an AMD/intel architecture, the docker image needs to be built for that environment. Docker does have a multi-arch build function but it doesn't work very well with the dotnet images. The best way to do this is to build the image on the RPi itself, otherwise you will get an error when you try to run. Yes this does mean that the source code has to be on the device as well, but one could easily set up a 'master' RPi responsible for building and uploading images that every other Pi could just pull down.

### Running with .NET standalone
As .NET core and above (inclusive of .NET 5+) is cross-platform, we can also just install .NET on the RPi device locally and run it. Personally I prefer the docker option as its one less thing to install and the idy of just pulling down an image and running it seems quite neat.

To install .NET on the Pi, heres the [official MS docs](https://docs.microsoft.com/en-us/dotnet/iot/deployment) that might help.

Once installed from the root directory:

```cd snsrpi-device```

To build the project (this includes running dotnet restore)

``` dotnet build```

To run the project (with dev settings)

`dotnet run`

To publish (for production). This is a smaller/optimised package with all necessary libraries to go with the executable. You could theoretically just ship this to the RPi so then you only need to install the .NET Runtime (+ ASP.NET runtime) as opposed to the .NET Runtime + SDK. -o specifies the output directory

`dotnet publish -c release -o /app`

Similarly to run the iot service without docker, from the project root:

`python iot/main.py`

***NOTE*** you will need to make sure that all the environment variables are initialised correctly in the shell environment before running this.

## Communciating with the Services

Both services are capable of listening for incoming requests in different ways. the snsrpi uses an ASP.NET WebAPI framework (basic HTTP REST API) while the iot service uses AWS Iot messaging using MQTT protocal.

### ASP.NET API
The API is predominantly only for listening to requests from the iot service. In theory, if the iot service isn't running than this service should be inaccessible. It is however listening on port 5000 which can be usefuly for testing in development.

The key endpoints are

`GET - /api/health`

Returns basic information on device states. Used as periodic 'heartbeat' for the iot service. Returns list of devices and running states.

`POST - /api/devices/{id}?active={bool}`

Used for starting/stopping devices remotely. `id` referes to the specific device id, which corresponds to the device name/serial.

**params**: active: true|false - whether the device is active (i.e. starting) or not (i.e. stopping)

`GET - /api/settings/{id}`

Returns device settings in json form. Settings format will match the format of the config files. e.g.

``` {json}
{
  "Sample_rate": 100,
  "Output_type": "csv",
  "Offline_mode": false,
  "Output_directory": "/home/jondufty/data",
  "File_upload": { "Active": true, "Endpoint": "http://localhost:6000" },
  "Save_interval": { "Unit": "second", "Interval": 30 }
}
```

`PUT - /api/settings/{id}`

Used for updating settings of a device. Requires the body to contain the new json object (in full) to replace the current settings. Note currently there is not check to see if the device is running or not in the settings. If the device is running and the settings are changed, no change will take place until the device is stopped/started again


### AWS IoT Communcations

The iot services uses the AWS Iot shadow service for communicate with the 'outside world'. This [AWS tutorial](https://docs.aws.amazon.com/iot/latest/developerguide/iot-shadows-tutorial.html) on device shadows is probably a good starting point, but they are essentially a way of viewing and modifying the state of a device.

This utilises two named shadows

**global** - `$aws/things/THING_NAME/shadow/name/global/` - primarily for the overall state of all devices and whether they are running or not. This should be a read-only state

**SENSOR** - `$aws/things/THING_NAME/shadow/name/SENSOR_ID/` - one for individual each sensor to display/update their settings

where THING_NAME = the name used when the thing is initalised on AWS. This should match the DEVICE_NAME env variable used above. SENSOR_ID is the individual device/sensor id/name/serial name (same as id in the snsrpi REST API)


# Room for improvement
This project was very much a WIP/Proof of concept so there are a lot of features/shortcomings that can be included to make it more robust and production-ready:
- Tests! When I decided to restructure the whole app, that restructuring did not include writing new unit tests. This is something definitely needed
- Im not sure if ASP.NET is the best choice for the API. It does the job but there might be more lightweight options out there
- Improved validation of inputs from the API. Especially things like the updating settings API
- The sample rate currently just implements a 'record everything and take every n-th sample` approach. I know the SNSR dll has a Decimate function that is probably the correct way to implement this but wasn't sure on how its implemented exactly.
- There was an intention to include the upload of files to some cloud storage of your choice. This should be pretty straightforward to implement, especially with the AWS IOT SDK, which you can use the IoT device certificates for authentication instead of having to store AWS credentials on each device (https://aws.amazon.com/blogs/security/how-to-eliminate-the-need-for-hardcoded-aws-credentials-in-devices-by-using-the-aws-iot-credentials-provider/)

## Terminology

I got halfway through this and realised the confusing terminology and naming conventions that I have unfortunately chosen for this project. Some examples:
- Logger refers to the `Logger` class used for operating and managing the accelerometers. It also refers to the .NET default logging object. I've tried to refer to this as Logs/_logger in classes where both are present
- device/sensor get used interchangeable between referring to the RPi and accelerometers. In the snsrpi API requests - device_id refers to the accelerometers. However in the API responses, device_id is the RPi and sensor_id is the accelerometers (sorry...)

In summary I apologise for the confusion, it got to a point where I was in too deep and couldn't be arsed to change it. If I were to re-visit this properly I would do a bit of a rename of everything.
