using System;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace BadCustomHelpPageWebHttpBehavior
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebGet]
        string EchoWithGet(string s);

        [OperationContract]
        [WebInvoke]
        string EchoWithPost(string s);
    }

    public class Service : IService
    {
        public string EchoWithGet(string s)
        {
            return "You said " + s;
        }

        public string EchoWithPost(string s)
        {
            return "You said " + s;
        }
    }

    static class Program
    {
        private static void Main(string[] args)
        {
            var host = new WebServiceHost(typeof(Service), new Uri("http://localhost:8000/"));
            try
            {
                host.AddServiceEndpoint(typeof(IService), new WebHttpBinding(), "");

                // BadCustomHelpPageWebHttpBehavior NOT INTENDED to use in real environment, added just for demonstration
                // Pass as an array of methods names to ignore in Help Page
                host.Description.Endpoints[0].Behaviors.Add(new BadCustomHelpPageWebHttpBehavior(new[] { "EchoWithGet" })
                {
                    HelpEnabled = true
                });

                host.Open();
                Console.WriteLine("Press <ENTER> to terminate");
                Console.ReadLine();

                host.Close();
            }
            catch (CommunicationException cex)
            {
                Console.WriteLine("An exception occurred: {0}", cex.Message);
                host.Abort();
            }
        }
    }
}
