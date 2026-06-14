using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using System;

public class BlobStorageService
{
    // A Connection String fica no seu appsettings.json (nunca hardcoded!)
    private readonly string _connectionString;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"]!;
        _containerName = configuration["AzureBlobStorage:ContainerName"]!;
    }

    public string GerarUrlDeUploadDireto(string nomeDoArquivoGeradoPelaApi)
    {
        // 1. Cria o cliente para o container específico
        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        
        // 2. Aponta para o arquivo que AINDA NÃO EXISTE (o destino do upload)
        var blobClient = containerClient.GetBlobClient(nomeDoArquivoGeradoPelaApi);

        // 3. Configura as regras do SAS Token
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = nomeDoArquivoGeradoPelaApi,
            Resource = "b", // "b" significa que a permissão é para um Blob (arquivo) específico
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5) // O token morre em 5 minutos
        };

        // 4. Define a permissão EXCLUSIVA de escrita (Create/Write)
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        // 5. Gera a URL final assinada
        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        return sasUri.ToString();
    }

    public string? GerarUrlDeLeitura(string? nomeDoArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeDoArquivo)) return null;

        // Se já for uma URL completa, retorna diretamente
        if (nomeDoArquivo.StartsWith("http://") || nomeDoArquivo.StartsWith("https://"))
        {
            return nomeDoArquivo;
        }

        var blobServiceClient = new BlobServiceClient(_connectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(nomeDoArquivo);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = nomeDoArquivo,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(2) // Expira em 2 horas
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }
}