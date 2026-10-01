using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using VistoriaApi.Application.Abstractions;

namespace VistoriaApi.Infrastructure.Files
{
    public sealed class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucket;

        public S3FileStorage(IConfiguration configuration)
        {
            var accessKey = configuration["Storage:R2:AccessKeyId"];
            var secretKey = configuration["Storage:R2:SecretAccessKey"];
            var serviceUrl = configuration["Storage:R2:ServiceUrl"];
            _bucket = configuration["Storage:R2:Bucket"];

            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(_bucket))
            {
                throw new InvalidOperationException("Configuração do R2 incompleta. Verifique Storage:R2:AccessKeyId, Storage:R2:SecretAccessKey, Storage:R2:ServiceUrl e Storage:R2:Bucket.");
            }

            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            _client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> SaveAsync(Stream stream, string contentType, CancellationToken cancellationToken)
        {
            var extension = contentType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => throw new InvalidOperationException("Tipo de arquivo não suportado.")
            };

            var key = Path.Combine(
                DateTime.UtcNow.ToString("yyyy"),
                DateTime.UtcNow.ToString("MM"),
                $"{Guid.NewGuid():N}{extension}")
                .Replace('\\', '/');

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType
            };

            var response = await _client.PutObjectAsync(putRequest, cancellationToken);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK && response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new InvalidOperationException($"Falha ao enviar arquivo para R2. Status: {response.HttpStatusCode}");
            }

            return key;
        }
    }
}
