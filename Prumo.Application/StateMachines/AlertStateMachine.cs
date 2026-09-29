using Prumo.Application.Common;
using Prumo.Domain.Enums;

namespace Prumo.Application.StateMachines
{
    /// <summary>Notificação (Figura 30 + D12, Seção 3.4.5).</summary>
    public static class AlertStateMachine
    {
        public const string Enfileirar = "Enfileirar";
        public const string Enviar = "Enviar";
        public const string MarcarLida = "MarcarLida";
        public const string Ignorar = "Ignorar";
        public const string Arquivar = "Arquivar";

        private static readonly Dictionary<(AlertStatus, string), AlertStatus> T = new()
        {
            {(AlertStatus.Gerada,      Enfileirar), AlertStatus.Enfileirada},
            {(AlertStatus.Enfileirada, Enviar),     AlertStatus.Enviada},
            {(AlertStatus.Enviada,     MarcarLida), AlertStatus.Lida},
            {(AlertStatus.Enviada,     Ignorar),    AlertStatus.Ignorada},
            {(AlertStatus.Lida,        Arquivar),   AlertStatus.Arquivada},
            {(AlertStatus.Ignorada,    Arquivar),   AlertStatus.Arquivada},
        };

        public static AlertStatus Aplicar(AlertStatus atual, string evento) =>
            T.TryGetValue((atual, evento), out var novo)
                ? novo
                : throw new BusinessRuleException(409, Messages.RN22_Transicao(atual, evento));

        public static bool Permite(AlertStatus atual, string evento) => T.ContainsKey((atual, evento));
    }
}
