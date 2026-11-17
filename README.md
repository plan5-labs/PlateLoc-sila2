# PlateLoc Controller

A C# SiLA Server for controlling the PlateLoc made by Agilent Technologies.

## About

### System Architecture

This server application is composed of the following two separate applications:

* PlateLocController.Server: A SiLA Server application implemented in accordance with the SiLA2 Standard  
* CommandExecutor: An application that sends commands to the instrument 

These applications communicate via gRPC.  
To control the PlateLoc, start both applications before sending any request.  

### Features

This server allows you to control the PlateLoc using the following features.

* Commands
  * StartCycle
  * StopCycle
  * SetSealingTime
  * SetSealingTemperature
  * Reset
  * EnumerateProfiles
* Properties
  * SealingTemperature
  * SealingTime
  * ActualTemperature
  * CycleCount
  * FirmwareVersion
  * Version
  * Status

## Command Details

### StartCycle

Start a seal cycle. This command triggers the execution of steps moving stage in, applying seal, and moving stage out in sequence.  
If you call this command while another observable command is running, the request will be rejected. 

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Running
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### StopCycle

Terminate the seal cycle currently in progress.  
The following notes apply to this command:
* This command may not work as intended because it is executed only after the StartCycle command has completed.
* When the StopCycle process is executed, the seal cycle stops immediately, which may cause the seal roll to become slack. This condition can result in an error, so verify the seal roll status and take any required actions.
  - The StopCycle process can be executed via the GUI without waiting for the StartCycle command to complete.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Idle
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### SetSealingTime

Assign the specified parameter value as the seal-cycle duration (in seconds).

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| SealingTime | Real | The value to be set as sealing time.<br>The specified value must be in the range 0.5–12.0. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle, Running
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Same as the status at the start of execution
  * If an error occurs: Error
  
### SetSealingTemperature

Assign the specified parameter value as the sealing temperature (°C) of the hot plate.

#### Parameters

| Identifier | Type | Description |
| -- | -- | -- |
| SealingTemperature | Integer | The value to be set as sealing temperature.<br>The specified value must be in the range 20-235. |

#### Response

None.

#### Status condition

* Status required to execute this command: Idle, Running
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Same as the status at the start of execution
  * If an error occurs: Error

### Reset

Reset the server to its IDLE state. This command is common to all instruments.  
If you call this command while another observable command is running, the request will be rejected.

#### Parameters

None.

#### Response

None.

#### Status condition

* Status required to execute this command: Error, Running
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes
  * If it completes normally: Idle
  * If an error occurs: Error

### EnumerateProfiles

Enumerate profiles for the PlateLoc.

#### Parameters

None.

#### Response

| Identifier | Type | Description |
| -- | -- | -- |
| ProfileNames | String List | The names of the profiles. |

#### Status condition

* Status required to execute this command: Idle, Running
* Status while this command is running: Same as the status at the start of execution
* Status after this command finishes:  Same as the status at the start of execution

## Property Details

### SealingTemperature

Get the sealing temperature (°C) currently set by the user.  
Status required to get this property: Idle, Running

### SealingTime

Get the sealing time currently set by the user.  
Status required to get this property: Idle, Running

### ActualTemperature

Actual temperature (°C) of the hot plate.  
Status required to get this property: Idle, Running

### CycleCount

The count of seal cycles performed.  
Status required to get this property: Idle, Running

### FirmwareVersion

The firmware version of the instrument.  
Status required to get this property: Idle, Running

### Version

The version number of the PlateLoc ActiveX Control.  
Status required to get this property: Idle, Running

### Status

Returns the status of the instrument. This property is common to all instruments.  

* 0: Not Connected  
* 1: Idle  
* 2: Running  
* 3: Error  

## Getting Started

### Cabling

Use a USB-to-serial (RS-232) conversion cable to connect the PC running this server to the PlateLoc.

### Prerequisites

* .NET 10 (for SiLA Server)
* .NET Framework 4.8 (for CommandExecutor)
* VWorks ActiveX Control (for CommandExecutor)
  * version: 13.1.0.1366 or later

### Usage

#### On the first launch (Configure PlateLoc Profile and Settings Files)

1. Double-click PlateLocCommandExecutor.exe. After the Command Executor is launched, a light-green square icon appears in the task tray.  
Note: If a confirmation dialog appears, click the **Yes** button to continue launching the Command Executor.
2. Right-click the icon. A context menu appears.
3. Click the **Profile Settings** menu in the context menu. A settings window opens.
4. Click **Create a new profile** button. A child window opens.
5. Enter a profile name of your choice and click the **OK** button. The child window closes.
6. Configure each setting as needed and click the **OK** button. The settings window closes.
7. Quit the Command Executor. The detailed procedure is described in a later section.
8. Edit the following files:  
    - appsettings.json (For SiLA Server) : Set the items in the ServerConfig section as needed.
    - application.json (For CommandExecutor) : Set the "profileName" and anything if you need to configure.

#### Launch the Software

1. Double-click PlateLocCommandExecutor.exe.  After the Command Executor is launched, a light-green square icon appears in the task tray.  
2. Double-click PlateLocController.Server.exe. A console window opens and hosting starts.

#### Quit the Server

1. Focus on the Console Window.
2. Press *Ctrl + C*. The console window will close.

#### Quit the Command Executor

1. Right-click the icon on task tray. A context menu appears.
2. Click **Exit** menu on the context menu.

#### Encryption

You can configure whether to enable TLS.  

If you want to enable it, follow the procedure below:
- Set "EnableTLS" in appsettings.json to True
- Place the public and private keys in the same directory as PlateLocController.Server.exe. The names of these files must be as follows:
  - Public key: server.crt
  - Private key: server.key
- If the software is already running, quit it and relaunch it

Note: For other certificate requirements, refer to the SiLA2 Documentation (especially *Encryption* in Part B).

If you want to disable it (i.e., run the SiLA Server in insecure mode), follow the procedure below:
- Set "EnableTLS" in appsettings.json to False
- If the software is already running, quit it and relaunch it

### Sample Scripts

You can find a simple script at `PythonClient/plateloc_controller/examples/snip_plateloc.py` that can be used to verify the basic operation of this software. The script contains example usage for each command and property. To test a specific command or property, follow the procedure below:
- Copy and paste the corresponding region from the script into your own script. Make sure your script is placed in the same directory as snip_plateloc.py
- Set "host", "port", and "cert_path" in  `PythonClient/plateloc_controller/examples/config.yml`
- Create and activate a virtual environment if you prefer to use one
- Install the mock server as a module by running the following command in the `PythonClient` directory
```bash
pip install .
```
- Run your script

## License

MIT License. Copyright (c) 2025-2026 RIKEN. See [LICENSE](LICENSE).

Third-party components are subject to their own licenses (see `Server/Licenses/`). The Agilent ActiveX controls used by the command executor are not included in this repository and are subject to Agilent's license terms.
