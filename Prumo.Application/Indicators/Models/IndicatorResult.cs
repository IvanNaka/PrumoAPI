namespace Prumo.Application.Indicators.Models
{
    /// <summary>
    /// Formato comum dos indicadores (Seção 3.5): <c>{ disponivel, motivo?, ...valores }</c>.
    /// Quando falta dado, <c>disponivel = false</c> e <c>motivo</c> traz a mensagem RN18.
    /// </summary>
    public abstract class IndicatorResult
    {
        public bool Disponivel { get; set; } = true;
        public string? Motivo { get; set; }

        public T Indisponivel<T>(string? motivo = null) where T : IndicatorResult
        {
            Disponivel = false;
            Motivo = motivo ?? IndicatorMath.DadosInsuficientes;
            return (T)this;
        }
    }
}
