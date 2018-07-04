using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaraPaySMSService
{
    public partial class SMSPush : ServiceBase
    {
        ApplicationController applicationController = null;
        Thread applicationThread = null;
        public SMSPush()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            SharedClass.HasStopSignal = false;
            SharedClass.IsServiceCleaned = false;
            applicationController = new ApplicationController();
            SharedClass.Logger.Info("Starting Application");
            this.applicationController.Start();
        }

        protected override void OnStop()
        {
            SharedClass.Logger.Info("Stopping Application");
            SharedClass.HasStopSignal = true;
            applicationController.Stop();
            while (!SharedClass.IsServiceCleaned)
            {
                SharedClass.Logger.Info("Service Not Cleaned Yet");
                Thread.Sleep(1000);
            }   
        }
    }
}
