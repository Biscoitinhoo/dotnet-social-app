﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using social_app.Database;
using social_app.Models.Request;
using social_app.Repositories.Post;
using System.Text;

namespace social_app.RabbitMQ.Services
{
    public class PostService : BackgroundService
    {

        private readonly IPostRepository _repository;
        private readonly ILogger<PostService> _logger;

        public PostService(IPostRepository repository, ILogger<PostService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /** This is the simplest RabbitMQ implementation and isn't recommended to be used in production.
             * However, my focus isn't improving RabbitMQ security for now.
             * To a more complete and secure implementation, read: https://www.rabbitmq.com/confirms.html
             */
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "posts",
                                    durable: false,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Post received via RabbitMQ", DateTime.UtcNow.ToLongTimeString());

                /** TODO: nullcheck */
                var post = Newtonsoft.Json.JsonConvert.DeserializeObject<PostRequest>(message);
                _repository.Create(post);
            };

            channel.BasicConsume(queue: "posts",
                                    autoAck: true,
                                    consumer: consumer);

            return Task.CompletedTask;
        }
    }
}
