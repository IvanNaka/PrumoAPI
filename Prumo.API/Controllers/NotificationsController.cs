using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prumo.Application.DTOs.Notification;
using Prumo.Application.Interfaces;

namespace Prumo.API.Controllers
{
    // Central de notificações (RF46): cada usuário vê apenas as próprias.
    [ApiController]
    [Authorize]
    [Route("api/notificacoes")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        /// <summary>?status= e ?tipo= aceitam vários valores separados por vírgula (padrão: Enviada, Lida e Ignorada).</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificacaoDto>>> List([FromQuery] string? status, [FromQuery] string? tipo)
        {
            return Ok(await _service.ListMineAsync(status, tipo));
        }

        [HttpGet("nao-lidas/contagem")]
        public async Task<ActionResult<ContagemDto>> CountUnread()
        {
            return Ok(new ContagemDto { Quantidade = await _service.CountUnreadAsync() });
        }

        [HttpPatch("{id:guid}/lida")]
        public async Task<ActionResult<NotificacaoDto>> MarkAsRead(Guid id)
        {
            return Ok(await _service.MarkAsReadAsync(id));
        }

        [HttpPatch("{id:guid}/arquivar")]
        public async Task<ActionResult<NotificacaoDto>> Archive(Guid id)
        {
            return Ok(await _service.ArchiveAsync(id));
        }
    }
}
