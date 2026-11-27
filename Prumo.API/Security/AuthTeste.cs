using System.Security.Cryptography;
using System.Text;

namespace Prumo.API.Security
{
    /// <summary>
    /// Login de teste do plano alternativo de validação (Cap. 4, seção 4.9 b). O Google bloqueia login
    /// em navegadores automatizados, então os testes do Robot Framework entram com as contas simuladas
    /// por <c>POST /api/auth/teste</c>. A rota só responde quando, ao mesmo tempo:
    /// <list type="bullet">
    /// <item><c>AuthTeste:Habilitado = true</c>;</item>
    /// <item>o ambiente não é Production;</item>
    /// <item><c>AuthTeste:Chave</c> tem 16+ caracteres e chega igual no cabeçalho <see cref="Cabecalho"/>.</item>
    /// </list>
    /// Em qualquer outro caso a rota devolve 404, como se não existisse.
    /// </summary>
    public static class AuthTeste
    {
        public const string Cabecalho = "X-Auth-Teste-Chave";
        private const int TamanhoMinimoChave = 16;

        public static bool Habilitado(IConfiguration configuration, IHostEnvironment environment)
        {
            return !environment.IsProduction()
                && string.Equals(configuration["AuthTeste:Habilitado"], "true", StringComparison.OrdinalIgnoreCase)
                && (configuration["AuthTeste:Chave"]?.Length ?? 0) >= TamanhoMinimoChave;
        }

        public static bool ChaveValida(IConfiguration configuration, string? recebida)
        {
            var esperada = configuration["AuthTeste:Chave"];
            if (string.IsNullOrEmpty(esperada) || string.IsNullOrEmpty(recebida))
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(esperada), Encoding.UTF8.GetBytes(recebida));
        }
    }
}
