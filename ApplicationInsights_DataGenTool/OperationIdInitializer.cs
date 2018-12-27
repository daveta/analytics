using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp
{
    class OperationIdInitializer : ITelemetryInitializer
    {
        public OperationIdInitializer()
        {

        }

        public string OperationID { get; set; }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Operation.Id = OperationID;
        }
    }
}
