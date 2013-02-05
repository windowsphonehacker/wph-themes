using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Linq;


namespace liveTileAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                // Subscribe to the managed exception handler
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        /// Code to execute on Unhandled Exceptions
        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            Assembly a = Assembly.Load("Microsoft.Phone.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=24eec0d8c86cda1e");
            Type comBridgeType = a.GetType("Microsoft.Phone.InteropServices.ComBridge");
            MethodInfo dynMethod = comBridgeType.GetMethod("RegisterComDll", BindingFlags.Public | BindingFlags.Static);
            object retValue = dynMethod.Invoke(null, new object[] { "liblw.dll", new Guid("E79018CB-46A6-432D-8077-8C0863533001") });
            //uint retval = Microsoft.Phone.InteropServices.ComBridge.RegisterComDll("libwph.dll", new Guid("56624E8C-CF91-41DF-9C31-E25A98FAF464"));
            Imangodll instance = (Imangodll)new Cmangodll();

            //Update messaging tiles
            int unread = 0;
            instance.getUnreadSMSCount(out unread);
            
            Microsoft.Phone.Shell.StandardTileData data = new Microsoft.Phone.Shell.StandardTileData();
            data.Count = unread;

            foreach (var t in Microsoft.Phone.Shell.ShellTile.ActiveTiles.Where(t => t.NavigationUri.ToString().Contains("5B04B775-356B-4AA0-AAF8-6491FFEA5610"))) {
                t.Update(data);
            }

            //Update phone tile


            ScheduledActionService.LaunchForTest("ScheduledAgent", TimeSpan.FromMilliseconds(2200));

            NotifyComplete();
        }
    }
}