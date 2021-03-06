﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace Module
{

    /**

     * get
device/list

[
{
    name : "lamp_bedroom,"
    type : "NexaDevice"
    archetype : "lamp"
    properties = [
        {
            name : "value"
            value : true
        },
        {
            name : "level"
            value : "0.15"
            type : "float"
        }
        {
            name : "group"
            value : "false"
            type : "bool"
        }
    ]
},
{
...
}
]

get
device/status/<deviceName>

// Induvidual device status

{
...
}

get
device/status<deviceName>?<var1>=<value1>&<var2>=<value2>&...


put
device/status/<deviceName>

     * **/


    public class DeviceManagerService : ServiceBase
    {
        private DeviceManager mDeviceManager;

        public DeviceManagerService(ServiceCreationInfo info)
            : base("device", info)
        {
            mDeviceManager = info.DeviceManager;
        }

        [ServiceGetContractAttribute("list")]
        public List<DeviceBase.DeviceState> OnListDevices()
        {
            var devices = new List<DeviceBase.DeviceState>();

            foreach (var pair in mDeviceManager.Devices)
            {
                DeviceBase deviceBase = pair.Value;
                devices.Add(pair.Value.CopyState());
            }

            return devices;
        }

        [ServiceGetContractAttribute("status/{deviceName}")]
        public object OnGetDeviceStatus(string deviceName)
        {
            DeviceBase device = mDeviceManager.GetDevice(deviceName);
            if (device == null)
                throw new ServiceBase.RequestException(string.Format("Unknown device: {0}", deviceName));

            lock (device)
            {
                return device.CopyState();
            }
        }

        [ServicePutContract("status/{deviceName}")]
        public object OnUpdateDeviceStatus(string deviceName, dynamic body)
        {
            DeviceBase device = mDeviceManager.GetDevice(deviceName);
            if (device == null)
                throw new ServiceBase.RequestException(string.Format("Unknown device: {0}", deviceName));

            DeviceBase.DeviceState state = device.CopyState();
            foreach (var property in state.GetType().GetProperties())
            {
                if (!body.ContainsKey(property.Name))
                    continue;

                // Make sure we can actually set the value to begin with.
                if (property.SetMethod == null)
                    continue;

                Type type = property.PropertyType;
                var value = body[property.Name];

                if (type == typeof(System.Int32))
                    property.SetValue(state, Int32.Parse((string)value));
                else if (type == typeof(string))
                    property.SetValue(state, (string)value);
                else if (type == typeof(bool))
                    property.SetValue(state, bool.Parse((string)value));
                else if (type == typeof(float))
                    property.SetValue(state, float.Parse((string)value));
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // TODO - Add proper support for lists.
                }
                else if (type.IsEnum)
                {
                    var intValue = Int32.Parse((string)value);
                    property.SetValue(state, Enum.ToObject(type, intValue));
                }
                else
                    throw new ArgumentException("Unhandled type: " + type.Name);
            }

            lock (device)
            {
                device.ApplyState(state);

                return device.CopyState();
            }
        }
    }
}
