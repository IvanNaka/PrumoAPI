
namespace Prumo.Domain.Entities
{
    public class BaseEntity
    {
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool? Active { get; private set; } = true;
        public virtual void Activate()
            => Active = true;

        public virtual void Delete()
            => Active = false;
    }
}
