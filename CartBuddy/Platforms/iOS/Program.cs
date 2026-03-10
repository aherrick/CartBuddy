using Foundation;
using ObjCRuntime;
using UIKit;

namespace CartBuddy
{
    public class Program
    {
        static void Main(string[] args)
        {
            ObjCRuntime.Runtime.MarshalManagedException += (_, exArgs) =>
            {
                Console.WriteLine("*** MANAGED EXCEPTION ***");
                Console.WriteLine(exArgs.Exception);
                Console.WriteLine(exArgs.Exception.StackTrace);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, exArgs) =>
            {
                Console.WriteLine("*** UNHANDLED EXCEPTION ***");
                Console.WriteLine(exArgs.ExceptionObject);
            };

            try
            {
                UIApplication.Main(args, null, typeof(AppDelegate));
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** FATAL STARTUP EXCEPTION ***");
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}