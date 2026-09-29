namespace Prumo.Domain.Entities
{
    // Okr (Seção 3.2, RF14). Todo OKR tem pelo menos 1 Key Result (UC8 / RN13).
    public class Objective : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
        public ICollection<ProjectObjective> Projects { get; set; } = new List<ProjectObjective>();
    }
}
