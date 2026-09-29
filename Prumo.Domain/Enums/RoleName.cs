namespace Prumo.Domain.Enums
{
    // Perfis de acesso (RF02, D02). Os valores são gravados como texto e usados como claim de role no JWT.
    public enum RoleName
    {
        Desenvolvedor,
        QA,
        ProductOwner,
        TechLead,
        GerenteProjeto,
        Diretoria,
        Administrador
    }
}
