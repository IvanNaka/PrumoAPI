using Prumo.API.BackgroundServices;

namespace Prumo.Tests.Api
{
    // T21 — job diário de notificações às 07:00.
    public class DailyNotificationJobTests
    {
        [Fact]
        public void RodaUmaVezPorDia_APartirDas7h()
        {
            var dia = new DateTime(2026, 9, 29);

            Assert.False(DailyNotificationJob.DeveExecutar(dia.AddHours(6).AddMinutes(59), null));
            Assert.True(DailyNotificationJob.DeveExecutar(dia.AddHours(7), null));
            Assert.False(DailyNotificationJob.DeveExecutar(dia.AddHours(9), DateOnly.FromDateTime(dia)));
            Assert.True(DailyNotificationJob.DeveExecutar(dia.AddDays(1).AddHours(7), DateOnly.FromDateTime(dia)));
        }
    }
}
