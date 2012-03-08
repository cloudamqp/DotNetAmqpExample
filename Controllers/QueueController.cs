using System;
using System.Text;
using System.Web.Mvc;
using RabbitMQ.Client;
using System.Configuration;

namespace AmqpExample.Controllers
{
    public class QueueController : Controller
    {
        // Create a ConnectionFactory and set the Uri to the CloudAMQP url
        // the connectionfactory is stateless and can safetly be a static resource in your app
        static readonly ConnectionFactory connFactory = new ConnectionFactory
        {
            Uri = ConfigurationManager.AppSettings["CLOUDAMQP_URL"]
        };

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Publish()
        {
            // create a connection and open a channel, dispose them when done
            using (var conn = connFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
                // The message we want to put on the queue
                var message = DateTime.Now.ToString("F");
                // the data put on the queue must be a byte array
                var data = Encoding.UTF8.GetBytes(message);
                // ensure that the queue exists before we publish to it
                channel.QueueDeclare("queue1", false, false, false, null);
                // publish to the "default exchange", with the queue name as the routing key
                channel.BasicPublish("", "queue1", null, data);
            }
            return new EmptyResult();
        }

        public ActionResult Get()
        {
            using (var conn = connFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
                // ensure that the queue exists before we access it
                channel.QueueDeclare("queue1", false, false, false, null);
                // do a simple poll of the queue 
                var data = channel.BasicGet("queue1", false);
                // the message is null if the queue was empty 
                if (data == null) return Json(null);
                // convert the message back from byte[] to a string
                var message = Encoding.UTF8.GetString(data.Body);
                // ack the message, ie. confirm that we have processed it
                // otherwise it will be requeued a bit later
                channel.BasicAck(data.DeliveryTag, false);
                return Json(message);
            }
        }
    }
}
