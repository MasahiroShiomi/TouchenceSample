# TouchenceSample
This is a sample program for a touch sensor, CUB-D-16D-SN20-CN (Touchence Inc.). The details of sensor information are http://www.touchence.jp/en/cube/

## Overview
* Read data from touch sensors through serial-connecions
* Send abstract sensor data to clients through TCP/IP connections

## Desription
* This sample can read multiple touch sensors, and send time/ID/abstract sensor data to clients. If a client starts to connect to this program, it automatically sends the data to client. If the client wants to stop or start data, send "Stop\n" or "Start\n" to the program.
* Note that the multiple serial-connection function is implemented, but it is not tested yet.
* Currently a reconnection function to serial port is not implemented, if the serial connection failed then you need to re-run the program.

## Requirement
This program was written by Visual Studio 2015, therefore you might need it to compile this program.  

## Usage
* In "Config" folder, you can find connection settings in "Settings.xml". 
* Modify "comport" for your settings, and set appropriate "id" and "calibrationfilename" for your sensors. 
* By using a button on GUI, you can overwrite the background data.
* Currently the program only can use CUB-D-16D-SN20-CN (about "sensortype") 