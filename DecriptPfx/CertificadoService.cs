using System.Security.Cryptography.X509Certificates;

public class CertificadoService
{
    public string? cnpj;
    
    public CertificadoInfo DescriptografarCertificado(byte[] certificadoBytes, string senha)
    {        
        // Carregar o certificado PFX usando a senha fornecida
        var cert = new X509Certificate2(certificadoBytes, senha, X509KeyStorageFlags.MachineKeySet);
        var subject = cert.Subject;

        // Obter o período de validade do certificado
        var validadeInicial = cert.NotBefore;
        var validadeFinal = cert.NotAfter;

        // Extrair o nome da empresa
        //var nomeEmpresa = cert.Subject.Split(',').FirstOrDefault(x => x.Trim().StartsWith("CN="))?.Replace("CN=", "").Trim();
        var nomeEmpresa = ExtractCompanyName(subject);

        if(!string.IsNullOrWhiteSpace(subject))
        {
            // Extrair o CNPJ da empresa a partir do campo nomeEmpresa
            cnpj = ExtractCnpj(subject);
        }      
        
        // Retornar as informações encapsuladas
        return new CertificadoInfo
        {
            NomeEmpresa = nomeEmpresa,
            CNPJ = cnpj,
            ValidadeInicial = validadeInicial,
            ValidadeFinal = validadeFinal
        };
    }

    private string? ExtractCnpj(string subject)
    {
        var cnpjPrefix = ":";
        var cnpjStartIndex = subject.IndexOf(cnpjPrefix);
        if (cnpjStartIndex >= 0)
        {
            cnpjStartIndex += cnpjPrefix.Length;
            var cnpjEndIndex = subject.IndexOf(',', cnpjStartIndex);
            if (cnpjEndIndex == -1)
            {
                cnpjEndIndex = subject.Length;
            }

            var cnpj = subject.Substring(cnpjStartIndex, cnpjEndIndex - cnpjStartIndex);
            if (cnpj.All(char.IsDigit) && cnpj.Length == 14)
            {
                return cnpj;
            }
        }
        return null;
    }

    private string? ExtractCompanyName(string subject)
    {
        var companyNamePrefix = "CN=";
        var companyNameStartIndex = subject.IndexOf(companyNamePrefix);
        if (companyNameStartIndex >= 0)
        {
            companyNameStartIndex += companyNamePrefix.Length;
            var companyNameEndIndex = subject.IndexOf(':', companyNameStartIndex);
            if (companyNameEndIndex == -1)
            {
                companyNameEndIndex = subject.Length;
            }

            return subject.Substring(companyNameStartIndex, companyNameEndIndex - companyNameStartIndex);
        }
        return null;
    }

}