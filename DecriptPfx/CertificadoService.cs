using System.Security.Cryptography.X509Certificates;

public class CertificadoService
{
    public CertificadoInfo DescriptografarCertificado(byte[] certificadoBytes, string senha)
    {
        // Carregar o certificado PFX usando a senha fornecida
        var cert = new X509Certificate2(certificadoBytes, senha, X509KeyStorageFlags.MachineKeySet);

        // Obter o período de validade do certificado
        var validadeInicial = cert.NotBefore;
        var validadeFinal = cert.NotAfter;

        // Extrair o CNPJ da empresa a partir do campo Subject
        var cnpj = cert.Subject.Split(',').FirstOrDefault(x => x.Trim().StartsWith("2.5.4.45="))?.Replace("2.5.4.45=", "").Trim();

        // Extrair o nome da empresa
        var nomeEmpresa = cert.Subject.Split(',').FirstOrDefault(x => x.Trim().StartsWith("CN="))?.Replace("CN=", "").Trim();

        // Retornar as informações encapsuladas
        return new CertificadoInfo
        {
            NomeEmpresa = nomeEmpresa,
            CNPJ = cnpj,
            ValidadeInicial = validadeInicial,
            ValidadeFinal = validadeFinal
        };
    }
}