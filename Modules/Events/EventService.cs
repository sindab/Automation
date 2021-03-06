﻿using Logging;
using Module;
using System;
using System.Collections.Generic;

namespace Events
{
    public class EventService : ServiceBase
    {
        public class Event : EventArgs
        {
            public Event()
            {
                Data = new Dictionary<string,string>();
            }

            public string Name { get; set; }
            public Dictionary<string,string> Data { get; set; }
        }

        public event EventHandler<Event> OnEvent;

        public EventService(ServiceCreationInfo info)
            : base("events", info)
        {}

        [ServicePutContract()]
        public void OnEventRequest(Event externalEvent)
        {
            if (externalEvent == null)
                return;

            Log.Info("Got event " + externalEvent.Name);
            if (OnEvent != null)
                OnEvent(this, externalEvent);
        }
    }
}
