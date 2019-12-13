using DurableFanOutFanInApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DurableFanOutFanInApp.Interfaces
{
    interface IGiosApiClient
    {
        Task<PMDataSource> GetDataForSensorAsync(string sensorId);
    }
}
